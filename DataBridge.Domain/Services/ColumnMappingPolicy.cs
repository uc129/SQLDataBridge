namespace DataBridge.Domain.Services;

public static class ColumnMappingPolicy
{
    public static Dictionary<string, string?> AutoMapColumns(IReadOnlyList<string> cols) => new()
    {
        ["vendor"]             = AutoMap(cols, "vendor", "vend", "supplier"),
        ["invoiceDescription"] = AutoMap(cols, "invoice_reference")
            ?? AutoMap(cols, "invoice_description", "invoice_desc", "inv_desc", "narration", "description"),
        ["text"]               = cols.FirstOrDefault(c => c.Equals("text", StringComparison.OrdinalIgnoreCase)),
        ["purchasingDocument"] = AutoMap(cols, "purchasing_document", "purch_doc", "po_number", "po_no", "purchase_order"),
        ["documentHeader"]     = AutoMap(cols, "document_header_text", "document_header", "doc_header"),
        ["assignment"]         = AutoMap(cols, "assignment"),
        ["processed"]          = AutoMap(cols, "processed"),
        ["lineItemType"]       = AutoMap(cols, "lineitemtype", "line_item_type", "item_type"),
    };

    private static string? AutoMap(IReadOnlyList<string> cols, params string[] keywords)
        => cols.FirstOrDefault(c => keywords.Any(k => c.Contains(k, StringComparison.OrdinalIgnoreCase)));
}
