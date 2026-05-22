

namespace Domain.Aggregates
{
    public class FAGLL03SNABalanceApproveEntity
    {
        public Guid SNAGroupedInvoiceKey { get; set; }
        public string Company_Code {  get; set; } = null!;
        public required string Vendor {  get; set; } = null!;
        public required string Vendor_Description {  get; set; } = null!;
        //public required string Document_Number {  get; set; } = null!;
        //public required string User_Name {  get; set; } = null!;
        public required string RevisionNumber {  get; set; } = null!;

        public required string Base_Hyperion { get; set; } = null!;
        public required string ICP_Name { get; set; } = null!;
        public required string ICP_Hyperion { get; set; } = null!;
        public string Approver_Name {  get; set; } = null!;
        public string Approver_PSNO {  get; set; } = null!;
        public bool Balance_Approved_Local { get; set; }
        public DateTime? Approval_Date_Local { get; set; }
        public bool Balance_Approved_Doc { get; set; }
        public DateTime? Approval_Date_Doc { get; set; }
        public string? Approval_Comment { get; set; }

        // Finance Details - Local Curr
        public required decimal Total_Amount_Local_SAP { get; set; }
        public required decimal Total_Advance_Applied_Local { get; set; }
        public required decimal Net_Amount_Local{ get; set; }


        // Doc Curr
        public required string Document_Currency { get; set; } = null!;
        public required decimal Total_Amount_Doc { get; set; }
        public required decimal Total_Advance_Applied_Doc { get; set; }
        public required decimal Net_Amount_Doc { get; set; }
        public required decimal Net_Amount_Doc_INR { get; set; }
        public required decimal Net_Amount_Doc_ERV { get; set; }

        public required decimal Exchange_Rate { get; set; } = 1m;
        public DateTime? Exchange_Rate_Date { get; set; }
        public DateTime Quarter_Date { get; set; }
        public string Balance_Evidence_URL { get; set; } = null!;

        //////////////// Additional Fields ////////////////

    }
}