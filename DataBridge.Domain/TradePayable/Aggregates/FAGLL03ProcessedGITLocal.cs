namespace DataBridge.Domain.TradePayable.Aggregates;

public class FAGLL03ProcessedGITLocal
{
    public string? RevisionNumber { get; set; }
    public Guid Grouped_Invoice_Key { get; set; } = Guid.Empty;
    public string? Purchasing_Document { get; set; }
    public string? Vendor { get; set; }
    public string? Company_Code { get; set; }
    public string? Composite_Join_Key { get; set; }
    public string? GL_Account { get; set; }
    public string? Profit_Center { get; set; }
    public decimal Amount_Local { get; set; }
    public string? Vendor_Description { get; set; }
    public string? GL_Description { get; set; }
    public string? Industry { get; set; }
    public string? Credit_Period { get; set; }
    public DateTime Report_Date { get; set; }
    public string? ICP_Name { get; set; }
    public bool IsSNACompany { get; set; }
    public string? Vertical { get; set; }

    // Pivoted liability GL amounts
    public decimal _14005 { get; set; }
    public Guid Grouped_Key_14005 { get; set; } = Guid.Empty;
    public decimal _14006 { get; set; }
    public Guid Grouped_Key_14006 { get; set; } = Guid.Empty;
    public decimal _14007 { get; set; }
    public Guid Grouped_Key_14007 { get; set; } = Guid.Empty;
    public decimal _14012 { get; set; }
    public Guid Grouped_Key_14012 { get; set; } = Guid.Empty;
    public decimal _14021 { get; set; }
    public Guid Grouped_Key_14021 { get; set; } = Guid.Empty;
    public decimal _14701 { get; set; }
    public Guid Grouped_Key_14701 { get; set; } = Guid.Empty;
    public decimal _14705 { get; set; }
    public Guid Grouped_Key_14705 { get; set; } = Guid.Empty;

    // Adjustment and final balance
    public string? Join_Type { get; set; }
    public string? Adjusted_GL { get; set; }
    public decimal Adjusted_Amount { get; set; }
    public decimal Total_Adjustment { get; set; }
    public decimal Balance_Local { get; set; }

    public Guid RunId { get; set; }
    public int StepIndex { get; set; }
}
