namespace DataBridge.Domain.TradePayable.Aggregates;

public class FAGLL03Populated : FAGLL03RAWEntity
{
    public FAGLL03Populated() { }

    public FAGLL03Populated(FAGLL03RAWEntity raw)
    {
        Invoice_Key = raw.Invoice_Key;
        Document_Number = raw.Document_Number;
        Purchasing_Document = raw.Purchasing_Document;
        Invoice_Reference = raw.Invoice_Reference;
        Document_Header = raw.Document_Header;
        Document_Type = raw.Document_Type;
        Company_Code = raw.Company_Code;
        Assignment = raw.Assignment;
        Vendor = raw.Vendor;
        Vendor_Description = raw.Vendor_Description;
        Invoice_Description = raw.Invoice_Description;
        Industry = raw.Industry;
        Amount_Local = raw.Amount_Local;
        GL_Account = raw.GL_Account;
        GL_Description = raw.GL_Description;
        Profit_Center = raw.Profit_Center;
        Payment_Terms = raw.Payment_Terms;
        Document_Currency = raw.Document_Currency;
        Amount_Doc = raw.Amount_Doc;
        Document_Date = raw.Document_Date;
        Posting_Date = raw.Posting_Date;
        Payment_Date = raw.Payment_Date;
        Report_Date = raw.Report_Date;
        User_Name = raw.User_Name;
        SOURCE = raw.SOURCE;
        Edited = raw.Edited;
        RevisionNumber = raw.RevisionNumber;
        QuarterEndDate = raw.QuarterEndDate;
        UploadedDate = raw.UploadedDate;
    }

    public FAGLL03Populated(FAGLL03Populated existing) : this((FAGLL03RAWEntity)existing)
    {
        RunId = existing.RunId;
        StepIndex = existing.StepIndex;
        Processed = existing.Processed;
        LineItemType = existing.LineItemType;
    }

    public Guid RunId { get; set; } = Guid.Empty;
    public int StepIndex { get; set; } = 0;
    public string? Processed { get; set; }
    public string? LineItemType { get; set; }
}
