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
            var (columns, rows) = await exportRepository.ExecuteQueryAsync(cmd.ConnectionString, sql, ct);

            long totalRows  = rows.Count;
            result.RowsTotal = totalRows;
            int totalParts   = Math.Max(1, (int)Math.Ceiling((double)totalRows / cmd.MaxRowsPerFile));

            await Notify(cmd.JobId, "Writing",
                $"Fetched {totalRows:N0} rows — writing {totalParts} file(s)…", 50, totalRows, totalRows);

            for (int part = 1; part <= totalParts; part++)
            {
                int start = (part - 1) * cmd.MaxRowsPerFile;
                int count = Math.Min(cmd.MaxRowsPerFile, rows.Count - start);
                int pct   = 50 + (int)((double)(part - 1) / totalParts * 50);

                await Notify(cmd.JobId, "Writing",
                    $"Writing file {part} of {totalParts}…", pct, (long)start, totalRows);

                var partRows = rows.Skip(start).Take(count).ToList();
                var filepath = excelWriter.WritePartFile(
                    columns, partRows, cmd.FilePrefix, cmd.SheetName, cmd.OutputFolder, part, totalParts);
                result.OutputFiles.Add(filepath);
                result.FilesCreated++;
            }

            sw.Stop();
            result.Success     = true;
            result.ElapsedTime = $"{(int)sw.Elapsed.TotalMinutes}m {sw.Elapsed.Seconds}s";
            result.Message     = $"Exported {totalRows:N0} rows to {result.FilesCreated} file(s) in {result.ElapsedTime}.";

            var fileNames = result.OutputFiles.Select(f => Path.GetFileName(f) ?? f).ToList();
            await progressNotifier.NotifyAsync(cmd.JobId, new ProgressMessage
            {
                JobId       = cmd.JobId,
                Stage       = "Done",
                Message     = result.Message,
                Percent     = 100,
                RowsDone    = totalRows,
                RowsTotal   = totalRows,
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
