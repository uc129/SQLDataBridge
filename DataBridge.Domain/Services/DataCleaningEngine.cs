using System.Data;
using System.Text.RegularExpressions;

namespace DataBridge.Domain.Services;

public static class DataCleaningEngine
{
    public sealed record ColumnAliases(
        string Vendor,
        string InvoiceDescription,
        string Text,
        string PurchasingDocument,
        string DocumentHeader,
        string Assignment,
        string Processed,
        string LineItemType);

    public static ColumnAliases ResolveAliases(
        IReadOnlyList<string> columns,
        Dictionary<string, string?>? columnMapOverride)
    {
        var autoMap = ColumnMappingPolicy.AutoMapColumns(columns);

        string Col(string role, string fallback) =>
            columnMapOverride?.TryGetValue(role, out var r) == true && !string.IsNullOrWhiteSpace(r)
                ? r!
                : autoMap.TryGetValue(role, out var a) && !string.IsNullOrWhiteSpace(a)
                    ? a!
                    : fallback;

        return new ColumnAliases(
            Vendor:             Col("vendor",             "vendor"),
            InvoiceDescription: Col("invoiceDescription", "invoice_description"),
            Text:               Col("text",               "text"),
            PurchasingDocument: Col("purchasingDocument", "purchasing_document"),
            DocumentHeader:     Col("documentHeader",     "document_header_text"),
            Assignment:         Col("assignment",         "assignment"),
            Processed:          Col("processed",          "processed"),
            LineItemType:       Col("lineItemType",       "lineitemtype"));
    }

    /// Computes column updates for one row. Returns empty dict if nothing needs changing.
    public static Dictionary<string, string?> ComputeUpdates(
        IDictionary<string, object?> row,
        ColumnAliases a,
        IReadOnlySet<string> columns,
        Regex poRegex)
    {
        var updates = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        bool Has(string col) => columns.Contains(col);
        string? Get(string col) => row.TryGetValue(col, out var v) ? v?.ToString() : null;

        // ── Vendor extraction ──────────────────────────────────────────────
        if (Has(a.Vendor))
        {
            var vendor = Get(a.Vendor);
            if (string.IsNullOrWhiteSpace(vendor))
            {
                bool extracted = false;
                foreach (var (col, has) in new[] { (a.InvoiceDescription, Has(a.InvoiceDescription)), (a.Text, Has(a.Text)) })
                {
                    if (!has) continue;
                    var src = Get(col);
                    if (string.IsNullOrEmpty(src)) continue;
                    var m = VendorExtractionRules.VendorRegex.Match(src);
                    if (!m.Success) continue;
                    var cm = VendorExtractionRules.VendorCodeRegex.Match(m.Value);
                    if (!cm.Success) continue;
                    updates[a.Vendor]    = cm.Value;
                    updates[a.Processed] = $"Extracted Vendor From [{col}]";
                    extracted = true;
                    break;
                }
                if (!extracted)
                {
                    updates[a.Vendor]    = "Not Found";
                    updates[a.Processed] = $"Checked [{a.InvoiceDescription}]";
                }
            }
        }

        // ── PO extraction ──────────────────────────────────────────────────
        if (Has(a.PurchasingDocument))
        {
            var po = Get(a.PurchasingDocument);
            if (string.IsNullOrWhiteSpace(po))
            {
                string? extractedPo = null;
                string? processNote = null;

                if (Has(a.DocumentHeader))
                {
                    var dh = Get(a.DocumentHeader);
                    if (!string.IsNullOrEmpty(dh))
                    {
                        var m = poRegex.Match(dh);
                        if (m.Success) { extractedPo = m.Value; processNote = $"PO Extracted from [{a.DocumentHeader}]"; }
                    }
                }

                if (extractedPo == null && Has(a.Assignment))
                {
                    var asgn = Get(a.Assignment);
                    if (!string.IsNullOrEmpty(asgn))
                    {
                        var m = poRegex.Match(asgn);
                        if (m.Success) { extractedPo = m.Value; processNote = $"PO Extracted from [{a.Assignment}]"; }
                    }
                }

                // reads from updates dict first so vendor-just-extracted note is visible
                var prevProcessed = updates.TryGetValue(a.Processed, out var pp) ? pp : Get(a.Processed);
                bool prevHadVendor = prevProcessed?
                    .StartsWith("Extracted Vendor From", StringComparison.OrdinalIgnoreCase) == true;

                if (extractedPo != null)
                {
                    updates[a.PurchasingDocument] = extractedPo;
                    if (Has(a.LineItemType)) updates[a.LineItemType] = "With PO";
                    updates[a.Processed] = prevHadVendor
                        ? $"{processNote} & {prevProcessed}"
                        : processNote;
                }
                else
                {
                    updates[a.PurchasingDocument] = "Not Found";
                    if (Has(a.LineItemType)) updates[a.LineItemType] = "Non PO";
                    var checkedNote = $"Checked [{a.InvoiceDescription}]";
                    updates[a.Processed] = prevHadVendor
                        ? $"PO not found & {prevProcessed}"
                        : prevProcessed == checkedNote
                            ? "PO not found & Vendor not found"
                            : "PO not found";
                }
            }
            else if (Has(a.LineItemType))
            {
                updates[a.LineItemType] = "With PO";
            }
        }

        return updates;
    }

    /// Applies extraction in-place to every DataRow. Adds processed/lineitemtype columns if absent.
    public static void CleanDataTable(DataTable dt, ColumnAliases a, Regex poRegex)
    {
        if (!dt.Columns.Contains(a.Processed))    dt.Columns.Add(a.Processed,    typeof(string));
        if (!dt.Columns.Contains(a.LineItemType))  dt.Columns.Add(a.LineItemType,  typeof(string));

        var columns = new HashSet<string>(
            dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName),
            StringComparer.OrdinalIgnoreCase);

        foreach (DataRow row in dt.Rows)
        {
            var snapshot = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var col in columns)
                snapshot[col] = row[col] == DBNull.Value ? null : row[col];

            var updates = ComputeUpdates(snapshot, a, columns, poRegex);

            foreach (var (col, val) in updates)
                if (columns.Contains(col))
                    row[col] = val ?? (object)DBNull.Value;
        }
    }
}
