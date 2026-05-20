using ClosedXML.Excel;
using Dapper;
using DataBridge.Hubs;
using DataBridge.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;

namespace DataBridge.Services;

public class SqlExportService(IHubContext<ProgressHub> hub, IConfiguration config)
{
    private readonly int _fetchChunk = config.GetValue<int>("DataBridge:FetchChunkSize", 50_000);

    public async Task<JobResult> ExportAsync(ExportRequest req, string jobId, CancellationToken ct)
    {
        var sw     = Stopwatch.StartNew();
        var result = new JobResult();

        try
        {
            Directory.CreateDirectory(req.OutputFolder);

            var sql = req.IsRawQuery
                ? req.QueryOrView
                : $"SELECT * FROM {req.QueryOrView}";

            // ── Count rows ──────────────────────────────────────────
            await SendProgress(jobId, "Counting", "Counting rows…", 0);

            await using var countConn = new SqlConnection(req.ConnectionString);
            long totalRows = await countConn.ExecuteScalarAsync<long>(
                new CommandDefinition($"SELECT COUNT(*) FROM ({sql}) AS _cq",
                    commandTimeout: 0, cancellationToken: ct));

            result.RowsTotal = totalRows;
            int totalParts   = (int)Math.Ceiling((double)totalRows / req.MaxRowsPerFile);

            await SendProgress(jobId, "Counting",
                $"Found {totalRows:N0} rows → {totalParts} file(s)", 2, totalRows, totalRows);

            // ── Stream & split ──────────────────────────────────────
            await using var conn = new SqlConnection(req.ConnectionString);
            await conn.OpenAsync(ct);

            await using var reader = (SqlDataReader)await conn.ExecuteReaderAsync(
                new CommandDefinition(sql, commandTimeout: 0, cancellationToken: ct),
                CommandBehavior.SequentialAccess);

            var columns   = Enumerable.Range(0, reader.FieldCount)
                                      .Select(i => reader.GetName(i))
                                      .ToList();

            var buffer      = new List<object?[]>();
            long fetched    = 0;
            int  part       = 1;
            int  rowsInPart = 0;

            while (await reader.ReadAsync(ct))
            {
                ct.ThrowIfCancellationRequested();

                var row = new object?[reader.FieldCount];
                reader.GetValues(row!);
                for (int i = 0; i < row.Length; i++)
                    if (row[i] is DBNull) row[i] = null;

                buffer.Add(row);
                fetched++;
                rowsInPart++;

                if (fetched % 10_000 == 0)
                {
                    int pct = (int)(fetched * 90.0 / totalRows) + 2;
                    await SendProgress(jobId, "Fetching",
                        $"Fetching… {fetched:N0} / {totalRows:N0} rows", pct, fetched, totalRows);
                }

                if (rowsInPart >= req.MaxRowsPerFile)
                {
                    var filepath = WritePartFile(buffer, columns, req, part, totalParts);
                    result.OutputFiles.Add(filepath);
                    result.FilesCreated++;
                    part++;
                    rowsInPart = 0;
                    buffer.Clear();

                    int pct = (int)(fetched * 90.0 / totalRows) + 2;
                    await SendProgress(jobId, "Writing",
                        $"Written part {part - 1} of {totalParts}", pct, fetched, totalRows);
                }
            }

            if (buffer.Count > 0)
            {
                var filepath = WritePartFile(buffer, columns, req, part, totalParts);
                result.OutputFiles.Add(filepath);
                result.FilesCreated++;
            }

            sw.Stop();
            result.Success     = true;
            result.ElapsedTime = $"{(int)sw.Elapsed.TotalMinutes}m {sw.Elapsed.Seconds}s";
            result.Message     = $"Exported {fetched:N0} rows to {result.FilesCreated} file(s) in {result.ElapsedTime}.";

            await SendProgress(jobId, "Done", result.Message, 100, fetched, totalRows, isComplete: true);
        }
        catch (OperationCanceledException)
        {
            result.Message = "Export cancelled by user.";
            await SendProgress(jobId, "Error", result.Message, 0, isError: true);
        }
        catch (Exception ex)
        {
            result.Message = $"Error: {ex.Message}";
            await SendProgress(jobId, "Error", result.Message, 0, isError: true);
        }

        return result;
    }

    private static string WritePartFile(
        List<object?[]> buffer, List<string> columns,
        ExportRequest req, int part, int totalParts)
    {
        var filename = totalParts == 1
            ? $"{req.FilePrefix}.xlsx"
            : $"{req.FilePrefix}_part{part}.xlsx";
        var filepath = Path.Combine(req.OutputFolder, filename);

        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet(req.SheetName);

        for (int c = 0; c < columns.Count; c++)
        {
            var cell = ws.Cell(1, c + 1);
            cell.Value = columns[c];
            cell.Style.Font.Bold                = true;
            cell.Style.Font.FontColor           = XLColor.White;
            cell.Style.Fill.BackgroundColor     = XLColor.FromHtml("#1F4E79");
            cell.Style.Alignment.Horizontal     = XLAlignmentHorizontalValues.Center;
        }

        for (int r = 0; r < buffer.Count; r++)
        {
            for (int c = 0; c < buffer[r].Length; c++)
            {
                var val = buffer[r][c];
                if (val != null)
                    ws.Cell(r + 2, c + 1).SetValue(val.ToString());
            }

            if (r % 2 == 0)
                ws.Row(r + 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#EBF3FB");
        }

        ws.Columns().AdjustToContents(1, Math.Min(201, buffer.Count + 1));
        ws.SheetView.FreezeRows(1);
        ws.RangeUsed()!.SetAutoFilter();

        wb.SaveAs(filepath);
        return filepath;
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
