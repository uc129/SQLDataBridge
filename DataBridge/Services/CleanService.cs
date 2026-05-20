using Dapper;
using DataBridge.Hubs;
using DataBridge.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace DataBridge.Services;

public class CleanService(IHubContext<ProgressHub> hub, IConfiguration config)
{
    public static readonly string[] AllowedTables =
        ["FNATool_DataBridge1", "FNATool_DataBridge2", "FNATool_DataBridge3"];

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
        ["invoiceDescription"] = AutoMap(cols, "invoice_description", "invoice_desc", "inv_desc", "narration", "description"),
        ["purchasingDocument"] = AutoMap(cols, "purchasing_document", "purch_doc", "po_number", "po_no", "purchase_order"),
        ["documentHeader"] = AutoMap(cols, "document_header", "doc_header"),
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

            string vendorCol = Col("vendor", "vendor");
            string invDescCol = Col("invoiceDescription", "invoice_description");
            string poDocCol = Col("purchasingDocument", "purchasing_document");
            string docHdrCol = Col("documentHeader", "document_header");
            string assignCol = Col("assignment", "assignment");
            string processedCol = Col("processed", "processed");
            string lineTypeCol = Col("lineItemType", "lineitemtype");

            bool hasVendor = columns.Contains(vendorCol);
            bool hasInvDesc = columns.Contains(invDescCol);
            bool hasPO = columns.Contains(poDocCol);
            bool hasDocHeader = columns.Contains(docHdrCol);
            bool hasAssignment = columns.Contains(assignCol);
            bool hasProcessed = columns.Contains(processedCol);
            bool hasLineItemType = columns.Contains(lineTypeCol);

            if (!hasVendor && !hasPO)
            {
                result.Success = true;
                result.Message = $"No cleanable columns found — looked for vendor ({vendorCol}) and PO ({poDocCol}).";
                await SendProgress(req.JobId, "Done", result.Message, 100, isComplete: true);
                return result;
            }

            var poRegex = BuildPoRegex(req.PoLeadingDigits);

            // Status note strings (dynamic so they reflect actual column names)
            var noteVendorExtracted = $"Extracted Vendor From [{invDescCol}]";
            var noteCheckedDesc = $"Checked [{invDescCol}]";
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
                    if (hasVendor)
                    {
                        var vendor = row.TryGetValue(vendorCol, out var v) ? v?.ToString() : null;
                        if (string.IsNullOrWhiteSpace(vendor))
                        {
                            var invDesc = hasInvDesc && row.TryGetValue(invDescCol, out var id)
                                ? id?.ToString() : null;

                            if (!string.IsNullOrEmpty(invDesc))
                            {
                                var m = VendorRegex.Match(invDesc);
                                if (m.Success)
                                {
                                    var cm = VendorCodeRegex.Match(m.Value);
                                    if (cm.Success)
                                    {
                                        updates[vendorCol] = cm.Value;
                                        updates[processedCol] = noteVendorExtracted;
                                        vendorUpdated++;
                                    }
                                }
                                else
                                {
                                    updates[vendorCol] = "Not Found";
                                    updates[processedCol] = noteCheckedDesc;
                                }
                            }
                            else
                            {
                                updates[vendorCol] = "Not Found";
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

                            if (extractedPo != null)
                            {
                                updates[poDocCol] = extractedPo;
                                if (hasLineItemType) updates[lineTypeCol] = "With PO";
                                updates[processedCol] = prevProcessed == noteVendorExtracted
                                    ? $"{processNote} & {noteVendorExtracted}"
                                    : processNote;
                                poUpdated++;
                            }
                            else
                            {
                                updates[poDocCol] = "Not Found";
                                if (hasLineItemType) updates[lineTypeCol] = "Non PO";
                                updates[processedCol] = prevProcessed == noteVendorExtracted
                                    ? $"PO not found & {noteVendorExtracted}"
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
