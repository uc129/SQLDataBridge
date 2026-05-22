using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Aggregates.ExcelUpload
{
    public class FAGLL03ExcelEntity
    {
        [Column("Purchasing Document")]
        public string PurchasingDocument { get; set; } = null!;

        [Column("Document Header Text")]
        public string DocumentHeaderText { get; set; } = null!;

        [Column("Assignment")]
        public string Assignment { get; set; } = null!;

        [Column("Reference")]
        public string Reference { get; set; } = null!;

        [Column("Vendor")]
        public string Vendor { get; set; } = null!;


        [Column("Payment Terms")]
        public string? Payment_Terms { get; set; } = null!;

        [Column("Text")]
        public string Text { get; set; } = null!;

        [Column("Vendor/Customer Description")]
        public string VendorCustomerDescription { get; set; } = null!;

        [Column("G/L Account")]
        public string GLAccount { get; set; } = null!;

        [Column("G/L Description")]
        public string GLDescription { get; set; } = null!;

        [Column("Company Code")]
        public string CompanyCode { get; set; } = null!;

        [Column("User Name")]
        public string UserName { get; set; } = null!;

        [Column("Amount in Local Currency")]
        public decimal? AmountInLocalCurrency { get; set; } = null!;

        [Column("Valuated Amt in LC 3")]
        public decimal? ValuatedAmtInLC3 { get; set; }

        [Column("Document Type")]
        public string DocumentType { get; set; } = null!;

        [Column("Document Number")]
        public string DocumentNumber { get; set; } = null!;

        [Column("Industry")]
        public string Industry { get; set; } = null!;

        [Column("Profit Center")]
        public string ProfitCenter { get; set; } = null!;

        [Column("Document Date")]
        public string DocumentDate { get; set; } = null!;// Kept as string because SQL converts it from text

        [Column("Posting Date")]
        public string PostingDate { get; set; } = null!;

        [Column("Net Due Date")]
        public string NetDueDate { get; set; } = null!;

        [Column("Document Currency")]
        public string DocumentCurrency { get; set; } = null!;

        [Column("Amount in Doc. Curr.")]
        public decimal? AmountInDocCurr { get; set; }
    }
}
