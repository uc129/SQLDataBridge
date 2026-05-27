namespace DataBridge.Domain.TradePayable.Aggregates;

public class FAGLL03RAWEntity
{
    public string? Invoice_Key { get; set; }
    public string? Document_Number { get; set; }
    public string? Purchasing_Document { get; set; }
    public string? Invoice_Reference { get; set; }
    public string? Document_Header { get; set; }
    public string? Document_Type { get; set; }
    public string? Company_Code { get; set; }
    public string? Assignment { get; set; }
    public string? Vendor { get; set; }
    public string? Vendor_Description { get; set; }
    public string? Invoice_Description { get; set; }
    public string? Industry { get; set; }
    public decimal? Amount_Local { get; set; }
    public string? GL_Account { get; set; }
    public string? GL_Description { get; set; }
    public string? Profit_Center { get; set; }
    public string? Payment_Terms { get; set; }
    public string? Document_Currency { get; set; }
    public decimal? Amount_Doc { get; set; }
    public DateTime? Document_Date { get; set; }
    public DateTime? Posting_Date { get; set; }
    public DateTime? Payment_Date { get; set; }
    public string? User_Name { get; set; }
    public string? SOURCE { get; set; }
    public string? Edited { get; set; }
    public string? RevisionNumber { get; set; }
    public DateTime? Report_Date { get; set; }
    public DateTime? QuarterEndDate { get; set; }
    public DateTime? UploadedDate { get; set; }
}
