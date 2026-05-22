using DataBridge.Application.Commands;
using DataBridge.Application.Interfaces;
using DataBridge.Domain.Models;
using System.Diagnostics;

namespace DataBridge.Application.UseCases;

public class ImportDataUseCase(
    IExcelParser excelParser,
    IImportRepository importRepository,
    IProgressNotifier progressNotifier,
    IJobRegistry jobRegistry)
{
    public async Task<JobResult> ExecuteAsync(ImportCommand cmd)
    {
        var ct     = jobRegistry.Register(cmd.JobId);
        var sw     = Stopwatch.StartNew();
        var result = new JobResult();

        try
        {
            await Notify(cmd.JobId, "Scanning", "Scanning files for column schema…", 2);
            var (columns, dt) = await excelParser.BuildMergedDataTableAsync(cmd.Files, ct);
            await Notify(cmd.JobId, "Scanning",
                $"Found {columns.Count} unique columns across {cmd.Files.Count} file(s)", 5);

            var qualifiedTable = $"[{cmd.SchemaName}].[{cmd.TableName}]";
            if (cmd.ReplaceTable)
            {
                await Notify(cmd.JobId, "Setup", $"Recreating table {qualifiedTable}…", 8);
                await importRepository.DropAndCreateTableAsync(
                    cmd.SchemaName, cmd.TableName, columns, cmd.ConnectionString, ct);
            }
            else
            {
                await importRepository.EnsureColumnsExistAsync(
                    cmd.SchemaName, cmd.TableName, columns, cmd.ConnectionString, ct);
            }

            long totalRows = dt.Rows.Count;
            await Notify(cmd.JobId, "Importing",
                $"Importing {totalRows:N0} rows into {qualifiedTable}…", 30, 0, totalRows);
            await importRepository.BulkInsertAsync(
                cmd.SchemaName, cmd.TableName, dt, cmd.JobId, cmd.ConnectionString, ct);

            sw.Stop();
            result.Success      = true;
            result.RowsTotal    = totalRows;
            result.FilesCreated = cmd.Files.Count;
            result.ElapsedTime  = FormatElapsed(sw);
            result.Message      = $"Imported {totalRows:N0} rows from {result.FilesCreated} file(s) " +
                                  $"into {qualifiedTable} in {result.ElapsedTime}.";
            await Notify(cmd.JobId, "Done", result.Message, 100, totalRows, totalRows, isComplete: true);
        }
        catch (OperationCanceledException)
        {
            result.Message = "Import cancelled by user.";
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

    private static string FormatElapsed(Stopwatch sw) =>
        $"{(int)sw.Elapsed.TotalMinutes}m {sw.Elapsed.Seconds}s";
}
