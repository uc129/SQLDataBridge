using DataBridge.Application.Interfaces;
using DataBridge.Application.TradePayable.UseCases.Commands;
using DataBridge.Domain.Models;
using DataBridge.Domain.TradePayable.Aggregates;
using DataBridge.Domain.TradePayable.Contracts;
using DataBridge.Domain.TradePayable.Models;
using System.Data;

namespace DataBridge.Application.TradePayable.UseCases;

public class UploadFAGLL03UseCase(
    IExcelParser              excelParser,
    IFAGLL03StagingRepository stagingRepo,
    IPipelineRunRepository    pipelineRunRepo,
    IPipelineMemoryStore      memoryStore,
    IProgressNotifier         progressNotifier,
    IJobRegistry              jobRegistry,
    ISPStorageService         spoService)
{
    private static readonly string[] RequiredColumns =
    [
        "Purchasing Document", "Document Header Text", "Assignment", "Reference", "Vendor",
        "Text", "Vendor/Customer Description", "G/L Account", "G/L Description", "Company Code",
        "User Name", "Amount in Local Currency", "Valuated Amt in LC 3", "Document Type",
        "Document Number", "Industry", "Profit Center", "Document Date", "Posting Date",
        "Net Due Date", "Document Currency", "Amount in Doc. Curr."
    ];

    private static readonly string[] InvalidDocTypes = ["KG", "KV", "KL"];

    private static readonly string[] LocalCurrCompanyCodes =
        ["1000", "2000", "3000", "4000", "6000", "C100", "A000"];

    public async Task<(bool Success, string Message, Guid? RunId)> ExecuteAsync(UploadFAGLL03Command cmd)
    {
        var ct = jobRegistry.Register(cmd.JobId);
        try
        {
            var fileCount = cmd.Files.Count;
            await progressNotifier.NotifyAsync(cmd.JobId, Notify("Parsing",
                $"Reading {fileCount} file(s)…", 10));

            var (_, dt) = await excelParser.BuildMergedDataTableAsync(cmd.Files, ct, sanitizeColumnNames: false);

            // Validate required columns before doing any further work.
            var existingCols = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToHashSet();
            var missingCols  = RequiredColumns.Where(c => !existingCols.Contains(c)).ToList();
            if (missingCols.Count > 0)
            {
                var msg = $"Excel file is missing required columns: {string.Join(", ", missingCols)}";
                await progressNotifier.NotifyAsync(cmd.JobId, Notify("Error", msg, 0, isError: true));
                return (false, msg, null);
            }

            await progressNotifier.NotifyAsync(cmd.JobId, Notify("Parsing",
                $"Parsed {dt.Rows.Count:N0} rows across {fileCount} file(s). Creating pipeline run…", 40));

            var quarterEnd = Processing.HelperFunctions.GetLastDayOfQuarter(cmd.QuarterDate);
            var runId = Guid.NewGuid();
            var run = new PipelineRun
            {
                RunId          = runId,
                QuarterDate    = cmd.QuarterDate,
                RevisionNumber = cmd.RevisionNumber,
                Status         = PipelineRunStatus.Uploaded,
                StartedBy      = cmd.UserName ?? "unknown",
                StartedAt      = DateTime.UtcNow,
            };
            await pipelineRunRepo.CreateAsync(run);

            await progressNotifier.NotifyAsync(cmd.JobId, Notify("Uploading",
                "Converting and inserting raw data…", 60));

            var entities = ConvertToEntities(dt, runId, cmd.QuarterDate, quarterEnd, cmd.RevisionNumber);
            await stagingRepo.BulkInsertAsync(entities, runId);

            memoryStore.Store(runId, entities);

            // Best-effort SPO archive — does not block or fail the upload.
            _ = ArchiveFilesToSpoAsync(cmd, runId);

            var completionMsg = $"Uploaded {entities.Count:N0} rows. Run ID: {runId}";
            await progressNotifier.NotifyAsync(cmd.JobId, new ProgressMessage
            {
                JobId      = cmd.JobId,
                Stage      = "Done",
                Message    = completionMsg,
                Percent    = 100,
                RowsDone   = entities.Count,
                RowsTotal  = entities.Count,
                IsComplete = true,
            });

            return (true, completionMsg, runId);
        }
        catch (OperationCanceledException)
        {
            return (false, "Upload cancelled.", null);
        }
        catch (Exception ex)
        {
            await progressNotifier.NotifyAsync(cmd.JobId, Notify("Error", ex.Message, 0, isError: true));
            return (false, ex.Message, null);
        }
        finally
        {
            jobRegistry.Remove(cmd.JobId);
        }
    }

    private static List<FAGLL03RAWEntity> ConvertToEntities(
        DataTable dt, Guid runId, DateTime reportDate, DateTime quarterEnd, string revisionNumber)
    {
        var result = new List<FAGLL03RAWEntity>(dt.Rows.Count);

        foreach (DataRow row in dt.Rows)
        {
            var docType = row["Document Type"]?.ToString();
            var glAcc   = row["G/L Account"]?.ToString()?.Trim();

            if (InvalidDocTypes.Contains(docType)) continue;
            if (string.IsNullOrWhiteSpace(glAcc) || glAcc == "14724") continue;

            var companyCode = GetStr(row["Company Code"]);
            var amtLocal    = LocalCurrCompanyCodes.Contains(companyCode)
                                ? ParseDecimal(row["Amount in Local Currency"])
                                : ParseDecimal(row["Valuated Amt in LC 3"]);

            result.Add(new FAGLL03RAWEntity
            {
                Invoice_Key        = Guid.NewGuid().ToString(),
                RevisionNumber     = revisionNumber,
                QuarterEndDate     = quarterEnd,
                UploadedDate       = DateTime.UtcNow,
                Report_Date        = reportDate,
                SOURCE             = "FAGLL03 Raw Excel Upload",
                Edited             = "False",

                Document_Number    = GetStr(row["Document Number"]),
                Purchasing_Document= GetStr(row["Purchasing Document"]),
                Document_Header    = GetStr(row["Document Header Text"]),
                Assignment         = GetStr(row["Assignment"]),
                Invoice_Reference  = GetStr(row["Reference"]),
                Vendor             = GetStr(row["Vendor"]),
                Invoice_Description= GetStr(row["Text"]),
                Vendor_Description = GetStr(row["Vendor/Customer Description"]),
                GL_Account         = glAcc,
                GL_Description     = GetStr(row["G/L Description"]),
                Company_Code       = companyCode,
                User_Name          = GetStr(row["User Name"]),
                Document_Type      = docType,
                Industry           = GetStr(row["Industry"]),
                Profit_Center      = GetStr(row["Profit Center"]),
                Document_Currency  = GetStr(row["Document Currency"]),

                Amount_Local       = amtLocal,
                Amount_Doc         = ParseDecimal(row["Amount in Doc. Curr."]),

                Document_Date      = ParseDate(row["Document Date"]),
                Posting_Date       = ParseDate(row["Posting Date"]),
                Payment_Date       = ParseDate(row["Net Due Date"]),
            });
        }

        return result;
    }

    private async Task ArchiveFilesToSpoAsync(UploadFAGLL03Command cmd, Guid runId)
    {
        try
        {
            var year       = cmd.QuarterDate.Year.ToString();
            var quarter    = cmd.QuarterDate.ToString("yyyy-MM-dd");
            var folderPath = $"{year}/{quarter}/Excel_Upload_Data";

            foreach (var (fileName, stream) in cmd.Files)
            {
                try
                {
                    if (stream.CanSeek) stream.Position = 0;
                    using var ms = new MemoryStream();
                    await stream.CopyToAsync(ms);
                    await spoService.UploadFileAsync(ms.ToArray(), fileName, folderPath);
                }
                catch { /* individual file failure is non-fatal */ }
            }
        }
        catch { /* SPO archive failure is non-fatal */ }
    }

    private static string? GetStr(object? val)
    {
        if (val == null || val == DBNull.Value) return null;
        if (val is DateTime dt) return dt.ToString("dd.MM.yyyy");
        var s = val.ToString()?.Trim();
        return string.IsNullOrWhiteSpace(s) ? null : s;
    }

    private static DateTime? ParseDate(object? val)
    {
        var s = GetStr(val);
        if (s is null) return null;

        string[] formats =
        [
            "dd-MM-yyyy HH:mm:ss",
            "dd-MM-yyyy",
            "dd.MM.yyyy",
            "yyyy-MM-dd HH:mm:ss",
        ];

        if (DateTime.TryParseExact(s, formats,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var exact))
            return exact;

        return DateTime.TryParse(s, out var fallback) ? fallback : null;
    }

    private static decimal ParseDecimal(object? val)
    {
        var s = val?.ToString() ?? string.Empty;
        return decimal.TryParse(s, System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : 0m;
    }

    private static ProgressMessage Notify(string stage, string message, int percent,
        bool isError = false) =>
        new() { Stage = stage, Message = message, Percent = percent, IsError = isError };
}
