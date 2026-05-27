using DataBridge.Application.Commands;
using DataBridge.Application.Interfaces;
using DataBridge.Domain.Models;
using System.Diagnostics;

namespace DataBridge.Application.UseCases;

public class ExportDataUseCase(
    IExportRepository exportRepository,
    IExcelWriter excelWriter,
    IProgressNotifier progressNotifier,
    IJobRegistry jobRegistry)
{
    private const int ProgressInterval = 50_000;

    public async Task<JobResult> ExecuteAsync(ExportCommand cmd)
    {
        var ct     = jobRegistry.Register(cmd.JobId);
        var sw     = Stopwatch.StartNew();
        var result = new JobResult();

        try
        {
            Directory.CreateDirectory(cmd.OutputFolder);

            var sql = cmd.IsRawQuery ? cmd.QueryOrView : $"SELECT * FROM {cmd.QueryOrView}";

            await Notify(cmd.JobId, "Fetching", "Connecting…", 1);
            var stream  = await exportRepository.StreamQueryAsync(cmd.ConnectionString, sql, ct);
            var columns = stream.Columns;

            var buffer      = new List<object?[]>(cmd.MaxRowsPerFile);
            long rowsFetched = 0;
            int  partNum    = 0;
            var  partPaths  = new List<string>();

            await foreach (var row in stream.Rows.WithCancellation(ct))
            {
                buffer.Add(row);
                rowsFetched++;

                if (rowsFetched % ProgressInterval == 0)
                    await Notify(cmd.JobId, "Fetching",
                        $"Fetched {rowsFetched:N0} rows…",
                        Math.Clamp((int)(rowsFetched / (double)ProgressInterval) + 1, 2, 45),
                        rowsFetched);

                if (buffer.Count >= cmd.MaxRowsPerFile)
                {
                    partNum++;
                    await Notify(cmd.JobId, "Writing", $"Writing file {partNum}…", 50, rowsFetched);
                    partPaths.Add(excelWriter.WritePartFile(
                        columns, buffer, cmd.FilePrefix, cmd.SheetName, cmd.OutputFolder, partNum));
                    buffer.Clear();
                }
            }

            if (buffer.Count > 0)
            {
                partNum++;
                await Notify(cmd.JobId, "Writing", $"Writing file {partNum}…", 90, rowsFetched);
                partPaths.Add(excelWriter.WritePartFile(
                    columns, buffer, cmd.FilePrefix, cmd.SheetName, cmd.OutputFolder, partNum));
                buffer.Clear();
            }

            // Single-file exports get a clean name without the _part01 suffix
            if (partPaths.Count == 1)
            {
                var cleanPath = Path.Combine(cmd.OutputFolder, $"{cmd.FilePrefix}.xlsx");
                File.Move(partPaths[0], cleanPath, overwrite: true);
                partPaths[0] = cleanPath;
            }

            result.RowsTotal    = rowsFetched;
            result.OutputFiles  = partPaths;
            result.FilesCreated = partPaths.Count;

            sw.Stop();
            result.Success     = true;
            result.ElapsedTime = $"{(int)sw.Elapsed.TotalMinutes}m {sw.Elapsed.Seconds}s";
            result.Message     = $"Exported {rowsFetched:N0} rows to {result.FilesCreated} file(s) in {result.ElapsedTime}.";

            var fileNames = result.OutputFiles.Select(f => Path.GetFileName(f) ?? f).ToList();
            await progressNotifier.NotifyAsync(cmd.JobId, new ProgressMessage
            {
                JobId       = cmd.JobId,
                Stage       = "Done",
                Message     = result.Message,
                Percent     = 100,
                RowsDone    = rowsFetched,
                RowsTotal   = rowsFetched,
                IsComplete  = true,
                OutputFiles = fileNames,
            });
        }
        catch (OperationCanceledException)
        {
            result.Message = "Export cancelled by user.";
            await Notify(cmd.JobId, "Error", result.Message, 0, isError: true);
        }
        catch (Exception ex)
        {
            result.Message = $"Error: {ex.Message}";
            await Notify(cmd.JobId, "Error", result.Message, 0, isError: true);
        }
        finally
        {
            jobRegistry.Remove(cmd.JobId);
        }

        return result;
    }

    private Task Notify(string jobId, string stage, string message, int percent,
        long rowsDone = 0, long rowsTotal = 0, bool isError = false, bool isComplete = false) =>
        progressNotifier.NotifyAsync(jobId, new ProgressMessage
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
