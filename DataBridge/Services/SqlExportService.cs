using ClosedXML.Excel;
using DataBridge.Hubs;
using DataBridge.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;

namespace DataBridge.Services;

public class SqlExportService(IHubContext<ProgressHub> hub, IConfiguration config)
{
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

            // ── Phase 1: fetch everything before touching the file system ──
            await SendProgress(jobId, "Fetching", "Connecting…", 1);

            var allRows = new List<object?[]>();
            List<string> columns;

            await using (var conn = new SqlConnection(req.ConnectionString))
            {
                await conn.OpenAsync(ct);
                await using var cmd    = new SqlCommand(sql, conn) { CommandTimeout = 0 };
                await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess, ct);

                columns = Enumerable.Range(0, reader.FieldCount)
                                    .Select(i => reader.GetName(i))
                                    .ToList();

                while (await reader.ReadAsync(ct))
                {
                    ct.ThrowIfCancellationRequested();
                    var row = new object?[reader.FieldCount];
                    reader.GetValues(row!);
                    for (int i = 0; i < row.Length; i++)
                        if (row[i] is DBNull) row[i] = null;
                    allRows.Add(row);

                    if (allRows.Count % 10_000 == 0)
                        await SendProgress(jobId, "Fetching",
                            $"Fetching… {allRows.Count:N0} rows", 1, allRows.Count, 0);
                }
            }
            // Connection and reader are fully closed — SQL Server is free.

            long totalRows = allRows.Count;
            result.RowsTotal = totalRows;
            int totalParts   = Math.Max(1, (int)Math.Ceiling((double)totalRows / req.MaxRowsPerFile));

            await SendProgress(jobId, "Writing",
                $"Fetched {totalRows:N0} rows — writing {totalParts} file(s)…",
                50, totalRows, totalRows);

            // ── Phase 2: write all files after the connection is gone ──────
            for (int part = 1; part <= totalParts; part++)
            {
                int start = (part - 1) * req.MaxRowsPerFile;
                int count = Math.Min(req.MaxRowsPerFile, allRows.Count - start);
                int pct   = 50 + (int)((double)(part - 1) / totalParts * 50);

                await SendProgress(jobId, "Writing",
                    $"Writing file {part} of {totalParts}…", pct, (long)start, totalRows);

                var filepath = WritePartFile(allRows, start, count, columns, req, part, totalParts);
                result.OutputFiles.Add(filepath);
                result.FilesCreated++;
            }

            sw.Stop();
            result.Success     = true;
            result.ElapsedTime = $"{(int)sw.Elapsed.TotalMinutes}m {sw.Elapsed.Seconds}s";
            result.Message     = $"Exported {totalRows:N0} rows to {result.FilesCreated} file(s) in {result.ElapsedTime}.";

            var fileNames = result.OutputFiles.Select(Path.GetFileName).ToList()!;
            await SendProgress(jobId, "Done", result.Message, 100, totalRows, totalRows,
                isComplete: true, outputFiles: fileNames);
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
        List<object?[]> allRows, int startRow, int rowCount,
        List<string> columns, ExportRequest req, int part, int totalParts)
    {
        var filename = totalParts == 1
            ? $"{req.FilePrefix}.xlsx"
            : $"{req.FilePrefix}_part{part:D2}.xlsx";
        var filepath = Path.Combine(req.OutputFolder, filename);

        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet(req.SheetName);

        for (int c = 0; c < columns.Count; c++)
        {
            var cell = ws.Cell(1, c + 1);
            cell.Value = columns[c];
            cell.Style.Font.Bold            = true;
            cell.Style.Font.FontColor       = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E79");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        for (int r = 0; r < rowCount; r++)
        {
            var row = allRows[startRow + r];
            for (int c = 0; c < row.Length; c++)
            {
                var val = row[c];
                if (val != null)
                    ws.Cell(r + 2, c + 1).SetValue(val.ToString());
            }

            if (r % 2 == 0)
                ws.Row(r + 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#EBF3FB");
        }

        ws.Columns().AdjustToContents(1, Math.Min(201, rowCount + 1));
        ws.SheetView.FreezeRows(1);
        ws.RangeUsed()!.SetAutoFilter();

        wb.SaveAs(filepath);
        return filepath;
    }

    private async Task SendProgress(
        string jobId, string stage, string message, int percent,
        long rowsDone = 0, long rowsTotal = 0,
        bool isError = false, bool isComplete = false,
        List<string>? outputFiles = null)
    {
        await hub.Clients.Group(jobId).SendAsync("progress", new ProgressMessage
        {
            JobId       = jobId,
            Stage       = stage,
            Message     = message,
            Percent     = percent,
            RowsDone    = rowsDone,
            RowsTotal   = rowsTotal,
            IsError     = isError,
            IsComplete  = isComplete,
            OutputFiles = outputFiles,
        });
    }
}
