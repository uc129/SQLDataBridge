using DataBridge.Application.Commands;
using DataBridge.Application.Interfaces;
using DataBridge.Domain.Models;
using DataBridge.Domain.Services;
using System.Diagnostics;

namespace DataBridge.Application.UseCases;

public class CleanDataUseCase(
    ICleanRepository cleanRepository,
    IProgressNotifier progressNotifier,
    IJobRegistry jobRegistry)
{
    public async Task<JobResult> ExecuteAsync(CleanCommand cmd)
    {
        var ct     = jobRegistry.Register(cmd.JobId);
        var sw     = Stopwatch.StartNew();
        var result = new JobResult();

        try
        {
            await Notify(cmd.JobId, "Preparing", $"Refreshing view for {cmd.TableName}…", 1);
            int viewSuffix = cmd.TableName[^1] - '0';
            await cleanRepository.RefreshVendorViewAsync(cmd.TableName, viewSuffix, null, ct);

            await Notify(cmd.JobId, "Scanning", $"Scanning columns in {cmd.TableName}…", 2);
            var columns = await cleanRepository.GetTableColumnsAsync(cmd.TableName, ct);
            var aliases = DataCleaningEngine.ResolveAliases(columns.ToList(), cmd.ColumnMap);
            var poRegex = VendorExtractionRules.BuildPoRegex(cmd.PoLeadingDigits);

            if (!columns.Contains(aliases.Vendor) && !columns.Contains(aliases.PurchasingDocument))
            {
                result.Success = true;
                result.Message = $"No cleanable columns found — looked for vendor ({aliases.Vendor}) " +
                                 $"and PO ({aliases.PurchasingDocument}).";
                await Notify(cmd.JobId, "Done", result.Message, 100, isComplete: true);
                return result;
            }

            await Notify(cmd.JobId, "Setup", "Preparing rows for cleaning…", 5);
            await cleanRepository.AddTemporaryRowNumberAsync(cmd.TableName, ct);

            try
            {
                var rows          = await cleanRepository.GetAllRowsAsync(cmd.TableName, ct);
                long total        = rows.Count;
                long done         = 0;
                long vendorUpdated = 0;
                long poUpdated     = 0;

                await Notify(cmd.JobId, "Cleaning", $"Processing {total:N0} rows…", 15, 0, total);

                foreach (var row in rows)
                {
                    ct.ThrowIfCancellationRequested();
                    done++;

                    int rn      = Convert.ToInt32(row["_rn"]);
                    var updates = DataCleaningEngine.ComputeUpdates(row, aliases, columns, poRegex);

                    if (updates.TryGetValue(aliases.Vendor, out var v) && v != "Not Found")
                        vendorUpdated++;
                    if (updates.TryGetValue(aliases.PurchasingDocument, out var p) && p != "Not Found")
                        poUpdated++;

                    if (updates.Count > 0)
                        await cleanRepository.UpdateRowAsync(cmd.TableName, rn, updates, columns, ct);

                    if (done % 500 == 0)
                    {
                        int pct = 15 + (int)(done * 80.0 / total);
                        await Notify(cmd.JobId, "Cleaning",
                            $"Processed {done:N0} / {total:N0} rows…", pct, done, total);
                    }
                }

                sw.Stop();
                result.Success     = true;
                result.RowsTotal   = total;
                result.ElapsedTime = $"{(int)sw.Elapsed.TotalMinutes}m {sw.Elapsed.Seconds}s";
                result.Message     = $"Cleaned {total:N0} rows — {vendorUpdated:N0} vendor(s) extracted, " +
                                     $"{poUpdated:N0} PO(s) extracted. ({result.ElapsedTime})";
                await Notify(cmd.JobId, "Done", result.Message, 100, total, total, isComplete: true);
            }
            finally
            {
                try { await cleanRepository.RemoveTemporaryRowNumberAsync(cmd.TableName); }
                catch { /* best-effort cleanup */ }
            }
        }
        catch (OperationCanceledException)
        {
            result.Message = "Cleaning cancelled by user.";
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
