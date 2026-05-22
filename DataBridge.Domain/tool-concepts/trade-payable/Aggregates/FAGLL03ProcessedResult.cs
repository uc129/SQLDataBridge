using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Aggregates
{
    using System;

    public class FAGLL03ProcessedResult : FAGLL03NetCPFixed
    {
        public FAGLL03ProcessedResult():base() { }
        public FAGLL03ProcessedResult(FAGLL03ProcessedResult existing) : base(existing) {
            this.Hyperion_Code = existing.Hyperion_Code;
            this.Hyp_Code_Description = existing.Hyp_Code_Description;
            this.Base_Hyperion_Debit = existing.Base_Hyperion_Debit;
            this.Destination_Hyperion_Credit = existing.Destination_Hyperion_Credit;
            this.Transacton_Type = existing.Transacton_Type;
            this.Ageing = existing.Ageing;
            this.Ageing_Years = existing.Ageing_Years;
            this.Ageing_Group = existing.Ageing_Group;
            this.Due_Status = existing.Due_Status;
            this.Billed_Status = existing.Billed_Status;
            this.MSME_Ageing = existing.MSME_Ageing;
            this.MSME_Type = existing.MSME_Type;
            this.Calculated_Due_Date = existing.Calculated_Due_Date;
            this.Amount_Doc_Adjusted_INR = existing.Amount_Doc_Adjusted_INR;
            this.Amount_Doc_Adjusted_ERV = existing.Amount_Doc_Adjusted_ERV;
            this.Exchange_Rate = existing.Exchange_Rate;
        }

        //Ageing
        public int Ageing { get; set; }
        public decimal Ageing_Years { get; set; }
        public string? Ageing_Group { get; set; } = null;
        public string? Due_Status { get; set; } = null;
        public string? Billed_Status { get; set; } = null;
        public string? MSME_Ageing { get; set; } = null;
        public string? MSME_Type { get; set; } = null;
        public DateTime? Calculated_Due_Date { get; set; } = null;

        //Hyperion Codes
        public decimal Base_Hyperion_Debit { get; set; }
        public decimal Destination_Hyperion_Credit { get; set; }
        public string Hyperion_Code { get; set; } = null!;
        public string? Hyp_Code_Description { get; set; } = null;
        public string? ICP_Hyperion { get; set; } = null;
        public string? Transacton_Type { get; set; } = null;

        //ERV Calc
        public decimal Amount_Doc_Adjusted_INR { get; set; }
        public decimal Amount_Doc_Adjusted_ERV { get; set; }
        public decimal Exchange_Rate { get; set; }

        // Net Balance

        public decimal Net_Amount_INR { get; set; }
        public decimal Net_Amount_Doc {  get; set; }



        // SNA Approval
        public string? Approver_Name { get; set; } = null!;
        public string? Approver_PSNO { get; set; } = null!;
        public bool? Balance_Approved_Local { get; set; }
        public DateTime? Approval_Date_Local { get; set; }
        public bool? Balance_Approved_Doc { get; set; }
        public DateTime? Approval_Date_Doc { get; set; }
        public string? Approval_Comment { get; set; }
        public string? Balance_Evidence_URL { get; set; } = null!;

    }
}
