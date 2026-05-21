using Dapper;
using DataBridge.Hubs;
using DataBridge.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace DataBridge.Services;

public class CleanService(IHubContext<ProgressHub> hub, IConfiguration config, ExcelImportService excelImportService)
{
    public static readonly string[] AllowedTables =
        ["FNATool_VendorDetailsPipeline_1", "FNATool_VendorDetailsPipeline_2", "FNATool_VendorDetailsPipeline_3"];

    private static readonly Regex VendorRegex = new(
        @"\bLT\d{4}\b|VC\s?\d{7}|\bVC\s?-?\s?(?:\d{5}|\d{7})\b|\b\d{7}\b|\b\d{5}\b",
        RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

    private static readonly Regex VendorCodeRegex = new(
        @"\bLT\d{4}\b|\d{7}|\d{5}",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);


    // ── Column auto-mapping ────────────────────────────────────────────────
    public static Dictionary<string, string?> AutoMapColumns(IReadOnlyList<string> cols) => new()
    {
        ["vendor"] = AutoMap(cols, "vendor", "vend", "supplier"),
        ["invoiceDescription"] = AutoMap(cols, "invoice_reference")
            ?? AutoMap(cols, "invoice_description", "invoice_desc", "inv_desc", "narration", "description"),
        ["text"] = cols.FirstOrDefault(c => c.Equals("text", StringComparison.OrdinalIgnoreCase)),
        ["purchasingDocument"] = AutoMap(cols, "purchasing_document", "purch_doc", "po_number", "po_no", "purchase_order"),
        ["documentHeader"] = AutoMap(cols, "document_header_text", "document_header", "doc_header"),
        ["assignment"] = AutoMap(cols, "assignment"),
        ["processed"] = AutoMap(cols, "processed"),
        ["lineItemType"] = AutoMap(cols, "lineitemtype", "line_item_type", "item_type"),
    };

    private static string? AutoMap(IReadOnlyList<string> cols, params string[] keywords)
        => cols.FirstOrDefault(c => keywords.Any(k => c.Contains(k, StringComparison.OrdinalIgnoreCase)));

    private static Regex BuildPoRegex(int[] digits)
    {
        if (digits.Length == 0) digits = [7, 8, 3];
        var alts = string.Join("|", digits.Distinct().Select(d => $@"\b{d}\d{{9}}\b"));
        return new Regex(alts, RegexOptions.Compiled);
    }

    public async Task<JobResult> RunAsync(CleanRequest req, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var result = new JobResult();
        var cs = config.GetConnectionString("Default")!;

        try
        {
            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync(ct);

            await SendProgress(req.JobId, "Preparing", $"Refreshing view for {req.TableName}…", 1);
            int viewSuffix = req.TableName[^1] - '0';
            await conn.ExecuteAsync(
                new CommandDefinition(
                    "EXEC [dbo].[sp_CreateVendorDetailsPipelineView] @TableName = @tn, @ViewSuffix = @vs",
                    new { tn = req.TableName, vs = viewSuffix },
                    cancellationToken: ct));

            await SendProgress(req.JobId, "Scanning", $"Scanning columns in {req.TableName}…", 2);
            var columns = await GetTableColumnsAsync(conn, req.TableName);

            // Resolve logical roles → actual column names (user override → auto-detect → fallback)
            var colsList = columns.ToList();
            var autoMap = AutoMapColumns(colsList);

            string Col(string role, string fallback) =>
                req.ColumnMap?.TryGetValue(role, out var r) == true && !string.IsNullOrWhiteSpace(r)
                    ? r!
                    : autoMap.TryGetValue(role, out var a) && !string.IsNullOrWhiteSpace(a)
                        ? a!
                        : fallback;

            string vendorCol    = Col("vendor", "vendor");
            string invDescCol   = Col("invoiceDescription", "invoice_description");
            string textCol      = Col("text", "text");
            string poDocCol     = Col("purchasingDocument", "purchasing_document");
            string docHdrCol    = Col("documentHeader", "document_header_text");
            string assignCol    = Col("assignment", "assignment");
            string processedCol = Col("processed", "processed");
            string lineTypeCol  = Col("lineItemType", "lineitemtype");

            bool hasVendor      = columns.Contains(vendorCol);
            bool hasInvDesc     = columns.Contains(invDescCol);
            bool hasText        = columns.Contains(textCol);
            bool hasPO          = columns.Contains(poDocCol);
            bool hasDocHeader   = columns.Contains(docHdrCol);
            bool hasAssignment  = columns.Contains(assignCol);
            bool hasProcessed   = columns.Contains(processedCol);
            bool hasLineItemType = columns.Contains(lineTypeCol);

            if (!hasVendor && !hasPO)
            {
                result.Success = true;
                result.Message = $"No cleanable columns found — looked for vendor ({vendorCol}) and PO ({poDocCol}).";
                await SendProgress(req.JobId, "Done", result.Message, 100, isComplete: true);
                return result;
            }

            var poRegex = BuildPoRegex(req.PoLeadingDigits);

            // Status note strings
            var noteCheckedDesc  = $"Checked [{invDescCol}]";
            var notePOFromDocHdr = $"PO Extracted from [{docHdrCol}]";
            var notePOFromAssign = $"PO Extracted from [{assignCol}]";

            // Add a temporary identity column for stable per-row UPDATE addressing
            await SendProgress(req.JobId, "Setup", "Preparing rows for cleaning…", 5);
            await conn.ExecuteAsync($"ALTER TABLE [{req.TableName}] ADD [_rn] INT IDENTITY(1,1)");

            try
            {
                var rows = (await conn.QueryAsync(
                    new CommandDefinition($"SELECT * FROM [{req.TableName}]", cancellationToken: ct)))
                    .Cast<IDictionary<string, object?>>().ToList();

                long total = rows.Count;
                long processed = 0;
                long vendorUpdated = 0;
                long poUpdated = 0;

                await SendProgress(req.JobId, "Cleaning", $"Processing {total:N0} rows…", 15, 0, total);

                foreach (var row in rows)
                {
                    ct.ThrowIfCancellationRequested();
                    processed++;

                    int rn = Convert.ToInt32(row["_rn"]);
                    var updates = new Dictionary<string, string?>();

                    // ── Vendor extraction ──────────────────────────────────────
                    // Try columns in priority order: invoice_description → text
                    if (hasVendor)
                    {
                        var vendor = row.TryGetValue(vendorCol, out var v) ? v?.ToString() : null;
                        if (string.IsNullOrWhiteSpace(vendor))
                        {
                            bool extracted = false;
                            foreach (var (col, has) in new[] { (invDescCol, hasInvDesc), (textCol, hasText) })
                            {
                                if (!has) continue;
                                var src = row.TryGetValue(col, out var sv) ? sv?.ToString() : null;
                                if (string.IsNullOrEmpty(src)) continue;
                                var m = VendorRegex.Match(src);
                                if (!m.Success) continue;
                                var cm = VendorCodeRegex.Match(m.Value);
                                if (!cm.Success) continue;
                                updates[vendorCol]    = cm.Value;
                                updates[processedCol] = $"Extracted Vendor From [{col}]";
                                vendorUpdated++;
                                extracted = true;
                                break;
                            }
                            if (!extracted)
                            {
                                updates[vendorCol]    = "Not Found";
                                updates[processedCol] = noteCheckedDesc;
                            }
                        }
                    }

                    // ── PO extraction ──────────────────────────────────────────
                    if (hasPO)
                    {
                        var po = row.TryGetValue(poDocCol, out var p) ? p?.ToString() : null;
                        if (string.IsNullOrWhiteSpace(po))
                        {
                            string? extractedPo = null;
                            string? processNote = null;

                            if (hasDocHeader)
                            {
                                var dh = row.TryGetValue(docHdrCol, out var dhv) ? dhv?.ToString() : null;
                                if (!string.IsNullOrEmpty(dh))
                                {
                                    var m = poRegex.Match(dh);
                                    if (m.Success) { extractedPo = m.Value; processNote = notePOFromDocHdr; }
                                }
                            }

                            if (extractedPo == null && hasAssignment)
                            {
                                var asgn = row.TryGetValue(assignCol, out var av) ? av?.ToString() : null;
                                if (!string.IsNullOrEmpty(asgn))
                                {
                                    var m = poRegex.Match(asgn);
                                    if (m.Success) { extractedPo = m.Value; processNote = notePOFromAssign; }
                                }
                            }

                            var prevProcessed = updates.TryGetValue(processedCol, out var pp)
                                ? pp
                                : (row.TryGetValue(processedCol, out var pp2) ? pp2?.ToString() : null);

                            bool prevHadVendor = prevProcessed?
                                .StartsWith("Extracted Vendor From", StringComparison.OrdinalIgnoreCase) == true;

                            if (extractedPo != null)
                            {
                                updates[poDocCol] = extractedPo;
                                if (hasLineItemType) updates[lineTypeCol] = "With PO";
                                updates[processedCol] = prevHadVendor
                                    ? $"{processNote} & {prevProcessed}"
                                    : processNote;
                                poUpdated++;
                            }
                            else
                            {
                                updates[poDocCol] = "Not Found";
                                if (hasLineItemType) updates[lineTypeCol] = "Non PO";
                                updates[processedCol] = prevHadVendor
                                    ? $"PO not found & {prevProcessed}"
                                    : prevProcessed == noteCheckedDesc
                                        ? "PO not found & Vendor not found"
                                        : "PO not found";
                            }
                        }
                        else if (hasLineItemType)
                        {
                            updates[lineTypeCol] = "With PO";
                        }
                    }

                    // ── Write changes ──────────────────────────────────────────
                    if (updates.Count > 0)
                    {
                        var filtered = updates.Where(kv => columns.Contains(kv.Key))
                                              .ToDictionary(kv => kv.Key, kv => kv.Value);
                        if (filtered.Count > 0)
                        {
                            var set = string.Join(", ", filtered.Keys.Select(k => $"[{k}] = @{k}"));
                            var pars = new DynamicParameters();
                            foreach (var kv in filtered) pars.Add(kv.Key, kv.Value);
                            pars.Add("rn", rn);
                            await conn.ExecuteAsync(
                                new CommandDefinition(
                                    $"UPDATE [{req.TableName}] SET {set} WHERE [_rn] = @rn",
                                    pars, cancellationToken: ct));
                        }
                    }

                    if (processed % 500 == 0)
                    {
                        int pct = 15 + (int)(processed * 80.0 / total);
                        await SendProgress(req.JobId, "Cleaning",
                            $"Processed {processed:N0} / {total:N0} rows…", pct, processed, total);
                    }
                }

                sw.Stop();
                result.Success = true;
                result.RowsTotal = total;
                result.ElapsedTime = $"{(int)sw.Elapsed.TotalMinutes}m {sw.Elapsed.Seconds}s";
                result.Message = $"Cleaned {total:N0} rows — {vendorUpdated:N0} vendor(s) extracted, " +
                                     $"{poUpdated:N0} PO(s) extracted. ({result.ElapsedTime})";
                await SendProgress(req.JobId, "Done", result.Message, 100, total, total, isComplete: true);
            }
            finally
            {
                try { await conn.ExecuteAsync($"ALTER TABLE [{req.TableName}] DROP COLUMN [_rn]"); }
                catch { /* ignore cleanup failure */ }
            }
        }
        catch (OperationCanceledException)
        {
            result.Message = "Cleaning cancelled by user.";
            await SendProgress(req.JobId, "Error", result.Message, 0, isError: true);
        }
        catch (Exception ex)
        {
            result.Message = $"Error: {ex.Message}";
            await SendProgress(req.JobId, "Error", result.Message, 0, isError: true);
        }

        return result;
    }

    // ── Combined import + clean (in-memory, single bulk-insert) ───────────────

    /// Applies vendor/PO extraction rules to every DataRow in-place.
    /// Ensures `processed` and `lineitemtype` columns exist in the DataTable.
    public static void CleanDataTable(DataTable dt, CleanRequest req)
    {
        if (!dt.Columns.Contains("processed"))    dt.Columns.Add("processed",    typeof(string));
        if (!dt.Columns.Contains("lineitemtype")) dt.Columns.Add("lineitemtype", typeof(string));

        var colNames = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
        var autoMap  = AutoMapColumns(colNames);
        var poRegex  = BuildPoRegex(req.PoLeadingDigits);

        string Col(string role, string fallback) =>
            req.ColumnMap?.TryGetValue(role, out var r) == true && !string.IsNullOrWhiteSpace(r)
                ? r!
                : autoMap.TryGetValue(role, out var a) && !string.IsNullOrWhiteSpace(a)
                    ? a!
                    : fallback;

        string vendorCol    = Col("vendor",             "vendor");
        string invDescCol   = Col("invoiceDescription", "invoice_description");
        string textCol      = Col("text",               "text");
        string poDocCol     = Col("purchasingDocument", "purchasing_document");
        string docHdrCol    = Col("documentHeader",     "document_header_text");
        string assignCol    = Col("assignment",         "assignment");
        string processedCol = Col("processed",          "processed");
        string lineTypeCol  = Col("lineItemType",       "lineitemtype");

        // Ensure resolved output columns exist even if not in source Excel
        if (!dt.Columns.Contains(processedCol)) dt.Columns.Add(processedCol, typeof(string));
        if (!dt.Columns.Contains(lineTypeCol))  dt.Columns.Add(lineTypeCol,  typeof(string));

        bool hasVendor     = dt.Columns.Contains(vendorCol);
        bool hasInvDesc    = dt.Columns.Contains(invDescCol);
        bool hasText       = dt.Columns.Contains(textCol);
        bool hasPO         = dt.Columns.Contains(poDocCol);
        bool hasDocHeader  = dt.Columns.Contains(docHdrCol);
        bool hasAssignment = dt.Columns.Contains(assignCol);

        var noteCheckedDesc  = $"Checked [{invDescCol}]";
        var notePOFromDocHdr = $"PO Extracted from [{docHdrCol}]";
        var notePOFromAssign = $"PO Extracted from [{assignCol}]";

        foreach (DataRow row in dt.Rows)
        {
            // ── Vendor extraction ──────────────────────────────
            if (hasVendor && string.IsNullOrWhiteSpace(row[vendorCol]?.ToString()))
            {
                bool extracted = false;
                foreach (var (col, has) in new[] { (invDescCol, hasInvDesc), (textCol, hasText) })
                {
                    if (!has) continue;
                    var src = row[col]?.ToString();
                    if (string.IsNullOrEmpty(src)) continue;
                    var m = VendorRegex.Match(src);
                    if (!m.Success) continue;
                    var cm = VendorCodeRegex.Match(m.Value);
                    if (!cm.Success) continue;
                    row[vendorCol]    = cm.Value;
                    row[processedCol] = $"Extracted Vendor From [{col}]";
                    extracted = true;
                    break;
                }
                if (!extracted)
                {
                    row[vendorCol]    = "Not Found";
                    row[processedCol] = noteCheckedDesc;
                }
            }

            // ── PO extraction ──────────────────────────────────
            if (hasPO && string.IsNullOrWhiteSpace(row[poDocCol]?.ToString()))
            {
                string? extractedPo = null;
                string? processNote = null;

                if (hasDocHeader)
                {
                    var dh = row[docHdrCol]?.ToString();
                    if (!string.IsNullOrEmpty(dh))
                    {
                        var m = poRegex.Match(dh);
                        if (m.Success) { extractedPo = m.Value; processNote = notePOFromDocHdr; }
                    }
                }
                if (extractedPo == null && hasAssignment)
                {
                    var asgn = row[assignCol]?.ToString();
                    if (!string.IsNullOrEmpty(asgn))
                    {
                        var m = poRegex.Match(asgn);
                        if (m.Success) { extractedPo = m.Value; processNote = notePOFromAssign; }
                    }
                }

                var prevProcessed = row[processedCol]?.ToString();
                bool prevHadVendor = prevProcessed?.StartsWith("Extracted Vendor From", StringComparison.OrdinalIgnoreCase) == true;

                if (extractedPo != null)
                {
                    row[poDocCol]     = extractedPo;
                    row[lineTypeCol]  = "With PO";
                    row[processedCol] = prevHadVendor ? $"{processNote} & {prevProcessed}" : processNote;
                }
                else
                {
                    row[poDocCol]     = "Not Found";
                    row[lineTypeCol]  = "Non PO";
                    row[processedCol] = prevHadVendor
                        ? $"PO not found & {prevProcessed}"
                        : prevProcessed == noteCheckedDesc
                            ? "PO not found & Vendor not found"
                            : "PO not found";
                }
            }
            else if (hasPO)
            {
                row[lineTypeCol] = "With PO";
            }
        }
    }

    /// Reads Excel files → cleans in memory → bulk-inserts once → refreshes view.
    public async Task<JobResult> RunImportAndCleanAsync(
        CleanRequest cleanReq, ImportRequest importReq,
        List<(string FileName, Stream Stream)> files,
        CancellationToken ct)
    {
        var sw     = Stopwatch.StartNew();
        var result = new JobResult();

        try
        {
            // Phase 1: build in-memory DataTable from Excel files
            await SendProgress(cleanReq.JobId, "Scanning", $"Reading {files.Count} file(s)…", 2);
            var (_, dt) = await excelImportService.BuildMergedDataTableAsync(files, ct);
            long total = dt.Rows.Count;
            await SendProgress(cleanReq.JobId, "Scanning",
                $"Loaded {total:N0} rows from {files.Count} file(s).", 8, 0, total);

            // Phase 2: clean rows in memory
            await SendProgress(cleanReq.JobId, "Cleaning", $"Cleaning {total:N0} rows…", 10, 0, total);
            CleanDataTable(dt, cleanReq);
            await SendProgress(cleanReq.JobId, "Cleaning", "Cleaning complete.", 55, total, total);

            // Phase 3: upload cleaned data in a single bulk insert
            await SendProgress(cleanReq.JobId, "Uploading",
                $"Uploading {total:N0} rows to [{importReq.TableName}]…", 60, 0, total);
            await using var conn = new SqlConnection(importReq.ConnectionString);
            await conn.OpenAsync(ct);
            await excelImportService.UploadDataTableAsync(
                conn, importReq.SchemaName, importReq.TableName,
                importReq.ReplaceTable, dt, cleanReq.JobId, ct);
            await SendProgress(cleanReq.JobId, "Uploading", "Upload complete.", 90, total, total);

            // Phase 4: refresh view
            int viewSuffix = importReq.TableName[^1] - '0';
            await SendProgress(cleanReq.JobId, "Setup",
                $"Refreshing view for {importReq.TableName}…", 95);
            await conn.ExecuteAsync(new CommandDefinition(
                "EXEC [dbo].[sp_CreateVendorDetailsPipelineView] @TableName = @tn, @ViewSuffix = @vs",
                new { tn = importReq.TableName, vs = viewSuffix },
                cancellationToken: ct));

            sw.Stop();
            result.Success     = true;
            result.RowsTotal   = total;
            result.ElapsedTime = $"{(int)sw.Elapsed.TotalMinutes}m {sw.Elapsed.Seconds}s";
            result.Message     = $"Imported and cleaned {total:N0} rows in {result.ElapsedTime}";
            await SendProgress(cleanReq.JobId, "Done", result.Message, 100, total, total, isComplete: true);
        }
        catch (OperationCanceledException)
        {
            result.Message = "Pipeline cancelled by user.";
            await SendProgress(cleanReq.JobId, "Error", result.Message, 0, isError: true);
        }
        catch (Exception ex)
        {
            result.Message = $"Error: {ex.Message}";
            await SendProgress(cleanReq.JobId, "Error", result.Message, 0, isError: true);
        }

        return result;
    }

    private static async Task<HashSet<string>> GetTableColumnsAsync(SqlConnection conn, string tableName)
    {
        var cols = await conn.QueryAsync<string>(
            "SELECT LOWER(COLUMN_NAME) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @t",
            new { t = tableName });
        return new HashSet<string>(cols, StringComparer.OrdinalIgnoreCase);
    }

    private async Task SendProgress(
        string jobId, string stage, string message, int percent,
        long rowsDone = 0, long rowsTotal = 0,
        bool isError = false, bool isComplete = false)
    {
        await hub.Clients.Group(jobId).SendAsync("progress", new ProgressMessage
        {
            JobId = jobId,
            Stage = stage,
            Message = message,
            Percent = percent,
            RowsDone = rowsDone,
            RowsTotal = rowsTotal,
            IsError = isError,
            IsComplete = isComplete,
        });
    }

}
