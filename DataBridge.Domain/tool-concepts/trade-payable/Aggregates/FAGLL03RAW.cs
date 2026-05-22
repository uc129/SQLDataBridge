

namespace Domain.Aggregates
{
    public class FAGLL03RAWEntity
    {
        // Identifiers
        public Guid Invoice_Key { get; set; } = Guid.Empty;
        public string? Document_Number { get; set; } = null!;
        public string? Purchasing_Document { get; set; } = null!;
        public string? Invoice_Reference { get; set; } = null!;

        // Header Details
        public string? Document_Header { get; set; } = null!;
        public string? Document_Type { get; set; } = null!;
        public string? Company_Code { get; set; } = null!;
        public string? Assignment { get; set; } = null!;

        // Vendor Details
        public string? Vendor { get; set; } = null!;
        public string? Vendor_Description { get; set; } = null!;
        public string? Invoice_Description { get; set; } = null!;
        public string? Industry { get; set; } = null!;

        // Financial Details
        // Use 'decimal' for monetary values.
        public decimal? Amount_Local { get; set; }
        public string? GL_Account { get; set; } = null!;
        public string? GL_Description { get; set; } = null!;
        public string? Profit_Center { get; set; } = null!;
        public string?  Payment_Terms { get; set; } = null!;
        public string? Document_Currency { get; set; } = null!;
        public decimal? Amount_Doc { get; set; }

        // Date Fields
        public DateTime? Document_Date { get; set; }
        public DateTime? Posting_Date { get; set; }
        public DateTime? Payment_Date { get; set; }

        // Metadata
        public string? User_Name { get; set; } = null!;
        public string? SOURCE { get; set; } = null!;
        public string? Edited { get; set; } = null!;
        public string? RevisionNumber { get; set; } = null!;
        public DateTime? Report_Date { get; set; }
        public DateTime? QuarterEndDate { get; set; }
        public DateTime? UploadedDate { get; set; }
    }
}
