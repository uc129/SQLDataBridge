using DataBridge.Application.Commands;
using DataBridge.Application.Interfaces;
using DataBridge.Domain.Models;
using DataBridge.Domain.Services;
using System.Diagnostics;

namespace DataBridge.Application.UseCases;

public class ImportAndCleanUseCase(
    IExcelParser excelParser,
    IImportRepository importRepository,
    ICleanRepository cleanRepository,
    IProgressNotifier progressNotifier,
    IJobRegistry jobRegistry)
{
    public async Task<JobResult> ExecuteAsync(ImportAndCleanCommand cmd)
    {
        var ct     = jobRegistry.Register(cmd.JobId);
        var sw     = Stopwatch.StartNew();
        var result = new JobResult();

        try
        {
            await Notify(cmd.JobId, "Scanning", $"Reading {cmd.Files.Count} file(s)…", 2);
            var (columns, dt) = await excelParser.BuildMergedDataTableAsync(cmd.Files, ct);
            long total = dt.Rows.Count;
            await Notify(cmd.JobId, "Scanning",
                $"Loaded {total:N0} rows from {cmd.Files.Count} file(s).", 8, 0, total);

            await Notify(cmd.JobId, "Cleaning", $"Cleaning {total:N0} rows…", 10, 0, total);
            var aliases = DataCleaningEngine.ResolveAliases(columns, cmd.ColumnMap);
            var poRegex = VendorExtractionRules.BuildPoRegex(cmd.PoLeadingDigits);
            DataCleaningEngine.CleanDataTable(dt, aliases, poRegex);
            await Notify(cmd.JobId, "Cleaning", "Cleaning complete.", 55, total, total);

            await Notify(cmd.JobId, "Uploading",
                $"Uploading {total:N0} rows to [{cmd.TableName}]…", 60, 0, total);

            // Use the DataTable's actual columns after cleaning (CleanDataTable may have added columns).
            var finalColumns = dt.Columns.Cast<System.Data.DataColumn>().Select(c => c.ColumnName).ToList();

            if (cmd.ReplaceTable)
                await importRepository.DropAndCreateTableAsync(
                    cmd.SchemaName, cmd.TableName, finalColumns, cmd.ConnectionString, ct);
            else
                await importRepository.EnsureColumnsExistAsync(
                    cmd.SchemaName, cmd.TableName, finalColumns, cmd.ConnectionString, ct);

            await importRepository.BulkInsertAsync(
                cmd.SchemaName, cmd.TableName, dt, cmd.JobId, cmd.ConnectionString, ct);
            await Notify(cmd.JobId, "Uploading", "Upload complete.", 90, total, total);

            int viewSuffix = cmd.TableName[^1] - '0';
            await Notify(cmd.JobId, "Setup", $"Refreshing view for {cmd.TableName}…", 95);
            await cleanRepository.RefreshVendorViewAsync(cmd.TableName, viewSuffix, cmd.ConnectionString, ct);

            sw.Stop();
            result.Success     = true;
            result.RowsTotal   = total;
            result.ElapsedTime = $"{(int)sw.Elapsed.TotalMinutes}m {sw.Elapsed.Seconds}s";
            result.Message     = $"Imported and cleaned {total:N0} rows in {result.ElapsedTime}";
            await Notify(cmd.JobId, "Done", result.Message, 100, total, total, isComplete: true);
        }
        catch (OperationCanceledException)
        {
            result.Message = "Pipeline cancelled by user.";
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
