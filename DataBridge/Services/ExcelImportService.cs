using Dapper;
using DataBridge.Hubs;
using DataBridge.Models;
using ExcelDataReader;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace DataBridge.Services;

public class ExcelImportService(IHubContext<ProgressHub> hub)
{
    public async Task<JobResult> ImportAsync(
        ImportRequest req, List<(string FileName, Stream Stream)> files,
        string jobId, CancellationToken ct)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var sw     = Stopwatch.StartNew();
        var result = new JobResult();
        long totalWritten = 0;

        try
        {
            // ── Step 1: Scan all files for unified column schema ────
            await SendProgress(jobId, "Scanning", "Scanning files for column schema…", 2);

            var allColumns = new List<string>();
            var seenCols   = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var fileSnapshots = new List<(string FileName, byte[] Data)>();

            foreach (var (fileName, stream) in files)
            {
                var data = await ReadStreamAsync(stream);
                fileSnapshots.Add((fileName, data));

                using var ms     = new MemoryStream(data);
                using var reader = ExcelReaderFactory.CreateReader(ms);
                var ds = reader.AsDataSet(new ExcelDataSetConfiguration
                {
                    ConfigureDataTable = _ => new ExcelDataTableConfiguration
                    {
                        UseHeaderRow  = true,
                        ReadHeaderRow = _ => { }
                    },
                    UseColumnDataType = false
                });

                foreach (DataTable table in ds.Tables)
                {
                    foreach (DataColumn col in table.Columns)
                    {
                        var safe = SafeColumnName(col.ColumnName);
                        if (!string.IsNullOrWhiteSpace(safe) && seenCols.Add(safe))
                            allColumns.Add(safe);
                    }
                    break;
                }
            }

            await SendProgress(jobId, "Scanning",
                $"Found {allColumns.Count} unique columns across {fileSnapshots.Count} file(s)", 5);

            // ── Step 2: Drop & recreate or extend table ─────────────
            await using var conn = new SqlConnection(req.ConnectionString);
            await conn.OpenAsync(ct);

            var qualifiedTable = $"[{req.SchemaName}].[{req.TableName}]";

            if (req.ReplaceTable)
            {
                await SendProgress(jobId, "Setup", $"Recreating table {qualifiedTable}…", 8);
                await DropAndCreateTableAsync(conn, req.SchemaName, req.TableName, allColumns, ct);
            }
            else
            {
                await EnsureColumnsExistAsync(conn, req.SchemaName, req.TableName, allColumns, ct);
            }

            // ── Step 3: Import each file ────────────────────────────
            int fileIndex = 0;
            foreach (var (fileName, data) in fileSnapshots)
            {
                ct.ThrowIfCancellationRequested();
                fileIndex++;

                await SendProgress(jobId, "Importing",
                    $"[{fileIndex}/{fileSnapshots.Count}] Reading {fileName}…",
                    8 + (int)(fileIndex * 85.0 / fileSnapshots.Count));

                using var ms     = new MemoryStream(data);
                using var reader = ExcelReaderFactory.CreateReader(ms);
                var ds = reader.AsDataSet(new ExcelDataSetConfiguration
                {
                    ConfigureDataTable = _ => new ExcelDataTableConfiguration
                    {
                        UseHeaderRow          = true,
                        EmptyColumnNamePrefix = "col_"
                    },
                    UseColumnDataType = false
                });

                var table = ds.Tables[0];
                if (table.Rows.Count == 0)
                {
                    await SendProgress(jobId, "Importing", $"  ⚠ {fileName} is empty, skipping.", 0);
                    continue;
                }

                var colMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (DataColumn col in table.Columns)
                    colMap[col.ColumnName] = SafeColumnName(col.ColumnName);

                var mapped = RemapTable(table, colMap, allColumns);
                long rows  = await BulkInsertAsync(conn, qualifiedTable, mapped, fileName, jobId, ct);

                totalWritten += rows;
                result.FilesCreated++;

                await SendProgress(jobId, "Importing",
                    $"  ✓ {fileName}: {rows:N0} rows imported",
                    8 + (int)(fileIndex * 85.0 / fileSnapshots.Count),
                    totalWritten);
            }

            sw.Stop();
            result.Success     = true;
            result.RowsTotal   = totalWritten;
            result.ElapsedTime = $"{(int)sw.Elapsed.TotalMinutes}m {sw.Elapsed.Seconds}s";
            result.Message     = $"Imported {totalWritten:N0} rows from {result.FilesCreated} file(s) " +
                                 $"into {qualifiedTable} in {result.ElapsedTime}.";

            await SendProgress(jobId, "Done", result.Message, 100, totalWritten, isComplete: true);
        }
        catch (OperationCanceledException)
        {
            result.Message = "Import cancelled by user.";
            await SendProgress(jobId, "Error", result.Message, 0, isError: true);
        }
        catch (Exception ex)
        {
            result.Message = $"Error: {ex.Message}";
            await SendProgress(jobId, "Error", result.Message, 0, isError: true);
        }

        return result;
    }

    // ── Helpers ────────────────────────────────────────────────────

    private static async Task<byte[]> ReadStreamAsync(Stream s)
    {
        using var ms = new MemoryStream();
        await s.CopyToAsync(ms);
        return ms.ToArray();
    }

    private static string SafeColumnName(string name)
    {
        var safe = Regex.Replace(name ?? "", @"[^0-9a-zA-Z_]", "_").Trim('_').ToLower();
        if (string.IsNullOrEmpty(safe)) return "col";
        if (char.IsDigit(safe[0]))      safe = "c_" + safe;
        return safe;
    }

    private static async Task DropAndCreateTableAsync(
        SqlConnection conn, string schema, string table,
        List<string> columns, CancellationToken ct)
    {
        var drop   = $"IF OBJECT_ID('[{schema}].[{table}]', 'U') IS NOT NULL DROP TABLE [{schema}].[{table}]";
        var colDef = string.Join(",\n  ", columns.Select(c => $"[{c}] NVARCHAR(MAX)"));
        var create = $"CREATE TABLE [{schema}].[{table}] (\n  {colDef}\n)";

        await conn.ExecuteAsync(new CommandDefinition(drop,   cancellationToken: ct));
        await conn.ExecuteAsync(new CommandDefinition(create, cancellationToken: ct));
    }

    private static async Task EnsureColumnsExistAsync(
        SqlConnection conn, string schema, string table,
        List<string> columns, CancellationToken ct)
    {
        var existing = new HashSet<string>(
            await conn.QueryAsync<string>(new CommandDefinition(
                @"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS
                  WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table",
                new { schema, table },
                cancellationToken: ct)),
            StringComparer.OrdinalIgnoreCase);

        foreach (var col in columns.Where(c => !existing.Contains(c)))
            await conn.ExecuteAsync(new CommandDefinition(
                $"ALTER TABLE [{schema}].[{table}] ADD [{col}] NVARCHAR(MAX)",
                cancellationToken: ct));
    }

    private static DataTable RemapTable(
        DataTable source, Dictionary<string, string> colMap, List<string> allColumns)
    {
        var mapped = new DataTable();
        foreach (var col in allColumns)
            mapped.Columns.Add(col, typeof(string));

        foreach (DataRow srcRow in source.Rows)
        {
            var newRow = mapped.NewRow();
            foreach (DataColumn srcCol in source.Columns)
            {
                var safeName = colMap.GetValueOrDefault(srcCol.ColumnName);
                if (safeName != null && mapped.Columns.Contains(safeName))
                {
                    var val = srcRow[srcCol];
                    newRow[safeName] = val == DBNull.Value || val == null
                        ? DBNull.Value
                        : val.ToString();
                }
            }
            mapped.Rows.Add(newRow);
        }

        return mapped;
    }

    private async Task<long> BulkInsertAsync(
        SqlConnection conn, string qualifiedTable,
        DataTable table, string fileName, string jobId, CancellationToken ct)
    {
        using var bulk = new SqlBulkCopy(conn)
        {
            DestinationTableName = qualifiedTable,
            BatchSize            = 500,
            BulkCopyTimeout      = 0,
            NotifyAfter          = 5_000,
        };

        foreach (DataColumn col in table.Columns)
            bulk.ColumnMappings.Add(col.ColumnName, col.ColumnName);

        long written = 0;
        bulk.SqlRowsCopied += async (_, e) =>
        {
            written = e.RowsCopied;
            await SendProgress(jobId, "Importing",
                $"  {fileName}: {written:N0} rows inserted…", 0, written);
        };

        await bulk.WriteToServerAsync(table, ct);
        return table.Rows.Count;
    }

    private async Task SendProgress(
        string jobId, string stage, string message, int percent,
        long rowsDone = 0, long rowsTotal = 0,
        bool isError = false, bool isComplete = false)
    {
        await hub.Clients.Group(jobId).SendAsync("progress", new ProgressMessage
        {
            JobId      = jobId,
            Stage      = stage,
            Message    = message,
            Percent    = percent,
            RowsDone   = rowsDone,
            RowsTotal  = rowsTotal,
            IsError    = isError,
            IsComplete = isComplete,
        });
    }
}
