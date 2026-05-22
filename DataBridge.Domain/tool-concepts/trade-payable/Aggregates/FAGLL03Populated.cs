

namespace Domain.Aggregates
{
    public class FAGLL03Populated : FAGLL03RAWEntity
    {
        public FAGLL03Populated() { }
        public FAGLL03Populated(FAGLL03RAWEntity rawrecords) {

            // ------------------
            // IDENTIFIERS
            // ------------------
            this.Invoice_Key = rawrecords.Invoice_Key;
            this.Document_Number = rawrecords.Document_Number;
            this.Purchasing_Document = rawrecords.Purchasing_Document;
            this.Invoice_Reference = rawrecords.Invoice_Reference;

            // ------------------
            // HEADER DETAILS
            // ------------------
            this.Document_Header = rawrecords.Document_Header;
            this.Document_Type = rawrecords.Document_Type;
            this.Company_Code = rawrecords.Company_Code;
            this.Assignment = rawrecords.Assignment;

            // ------------------
            // VENDOR DETAILS
            // ------------------
            this.Vendor = rawrecords.Vendor;
            this.Vendor_Description = rawrecords.Vendor_Description;
            this.Invoice_Description = rawrecords.Invoice_Description;
            this.Industry = rawrecords.Industry;

            // ------------------
            // FINANCIAL DETAILS
            // ------------------
            this.Amount_Local = rawrecords.Amount_Local;
            this.GL_Account = rawrecords.GL_Account;
            this.GL_Description = rawrecords.GL_Description;
            this.Profit_Center = rawrecords.Profit_Center;
            this.Payment_Terms = rawrecords.Payment_Terms;
            this.Document_Currency = rawrecords.Document_Currency;
            this.Amount_Doc = rawrecords.Amount_Doc;

            // ------------------
            // DATE FIELDS
            // ------------------
            this.Document_Date = rawrecords.Document_Date;
            this.Posting_Date = rawrecords.Posting_Date;
            this.Payment_Date = rawrecords.Payment_Date;
            this.Report_Date = rawrecords.Report_Date;

            // ------------------
            // METADATA
            // ------------------
            this.User_Name = rawrecords.User_Name;
            this.SOURCE = rawrecords.SOURCE;
            this.Edited = rawrecords.Edited;

        }
        public FAGLL03Populated(FAGLL03Populated existing) : base()
        {
            // ------------------
            // IDENTIFIERS
            // ------------------
            this.Invoice_Key = existing.Invoice_Key;
            this.Document_Number = existing.Document_Number;
            this.Purchasing_Document = existing.Purchasing_Document;
            this.Invoice_Reference = existing.Invoice_Reference;

            // ------------------
            // HEADER DETAILS
            // ------------------
            this.Document_Header = existing.Document_Header;
            this.Document_Type = existing.Document_Type;
            this.Company_Code = existing.Company_Code;
            this.Assignment = existing.Assignment;

            // ------------------
            // VENDOR DETAILS
            // ------------------
            this.Vendor = existing.Vendor;
            this.Vendor_Description = existing.Vendor_Description;
            this.Invoice_Description = existing.Invoice_Description;
            this.Industry = existing.Industry;

            // ------------------
            // FINANCIAL DETAILS
            // ------------------
            this.Amount_Local = existing.Amount_Local;
            this.GL_Account = existing.GL_Account;
            this.GL_Description = existing.GL_Description;
            this.Profit_Center = existing.Profit_Center;
            this.Payment_Terms = existing.Payment_Terms;
            this.Document_Currency = existing.Document_Currency;
            this.Amount_Doc = existing.Amount_Doc;

            // ------------------
            // DATE FIELDS
            // ------------------
            this.Document_Date = existing.Document_Date;
            this.Posting_Date = existing.Posting_Date;
            this.Payment_Date = existing.Payment_Date;
            this.Report_Date = existing.Report_Date;

            // ------------------
            // METADATA
            // ------------------
            this.User_Name = existing.User_Name;
            this.SOURCE = existing.SOURCE;
            this.Edited = existing.Edited;

            // Copy properties added in FAGLL03Populated
            this.ProcessId = existing.ProcessId;
            this.StepIndex = existing.StepIndex;
            this.Processed = existing.Processed;
            this.LineItemType = existing.LineItemType;
        }

        public Guid ProcessId { get; set; } = Guid.Empty;
        public int StepIndex { get; set; } = 0;
        public string? Processed { get; set; } = null;
        public string? LineItemType { get; set; } = null;
    }
}
