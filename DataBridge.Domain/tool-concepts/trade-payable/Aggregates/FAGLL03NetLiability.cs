using Domain.Aggregates.JoinedEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Aggregates
{
    public class FAGLL03NetLiability: JoinedVendorRecords
    {

        public FAGLL03NetLiability() { }
        public FAGLL03NetLiability(JoinedPOVendor povendordata) : base(povendordata)
        {
        }
        public FAGLL03NetLiability(FAGLL03NetLiability existing) : base(existing)
        {
            this.Grouped_Invoice_Key_Original = existing.Grouped_Invoice_Key_Original;
            this.Advance_22006 = existing.Advance_22006;
            this.Advance_22071 = existing.Advance_22071;
            this.Advance_22072 = existing.Advance_22072;
            this.Advance_22113 = existing.Advance_22113;
            this.Advance_23021 = existing.Advance_23021;
            this.Advance_23051 = existing.Advance_23051;
            this.Advance_23054 = existing.Advance_23054;
            this.Advance_23057 = existing.Advance_23057;
            this.Advance_23059 = existing.Advance_23059;
            this.Advance_23141 = existing.Advance_23141;
            this.Advance_TotalAdvanceAmount = existing.Advance_TotalAdvanceAmount;
            this.Advance_Applied = existing.Advance_Applied;
            this.Amount_Local_Adjusted = existing.Amount_Local_Adjusted;
            this.Adjustment_Type = existing.Adjustment_Type;
            this.Total_Remaining_Advance = existing.Total_Remaining_Advance;
            this.Base_Hyperion_Code = existing.Base_Hyperion_Code;
            this.Base_Hyperion_Description = existing.Base_Hyperion_Description;
            this.Base_SAP_Amount = existing.Base_SAP_Amount;
            this.ICP_Name = existing.ICP_Name;
            this.Entity_Type = existing.Entity_Type;
            this.Entity_Relation = existing.Entity_Relation;
            this.IsSNACompany = existing.IsSNACompany;
            this.Vertical = existing.Vertical;

            this.Advance_22006_Doc = existing.Advance_22006_Doc;
            this.Advance_22071_Doc = existing.Advance_22071_Doc;
            this.Advance_22072_Doc = existing.Advance_22072_Doc;
            this.Advance_22113_Doc = existing.Advance_22113_Doc;
            this.Advance_23021_Doc = existing.Advance_23021_Doc;
            this.Advance_23051_Doc = existing.Advance_23051_Doc;
            this.Advance_23054_Doc = existing.Advance_23054_Doc;
            this.Advance_23057_Doc = existing.Advance_23057_Doc;
            this.Advance_23059_Doc = existing.Advance_23059_Doc;
            this.Advance_23141_Doc = existing.Advance_23141_Doc;
            this.Advance_TotalAdvanceAmount_Doc = existing.Advance_TotalAdvanceAmount_Doc;
            this.Advance_Applied_Doc = existing.Advance_Applied_Doc;
            this.Amount_Doc_Adjusted = existing.Amount_Doc_Adjusted;
            this.Total_Remaining_Advance_Doc = existing.Total_Remaining_Advance_Doc;

        }


        public Guid Grouped_Invoice_Key_Original { get; set; } = Guid.Empty;
        public string Composite_Join_Key { get; set; } = null!;
        //public string Vertical { get; set; } = null!;
        public decimal Advance_22006 { get; set; }
        public decimal Advance_22071 { get; set; }
        public decimal Advance_22072 { get; set; }
        public decimal Advance_22113 { get; set; }
        public decimal Advance_23021 { get; set; }
        public decimal Advance_23051 { get; set; }
        public decimal Advance_23054 { get; set; }
        public decimal Advance_23057 { get; set; }
        public decimal Advance_23059 { get; set; }
        public decimal Advance_23141 { get; set; }
        public decimal Advance_22075 { get; set; }

        public decimal Advance_TotalAdvanceAmount { get; set; }
        public decimal Advance_Applied { get; set; }
        public decimal Amount_Local_Adjusted { get; set; }
        public string? Adjustment_Type { get; set; } = null;
        public decimal Total_Remaining_Advance { get; set; }
        public string? Base_Hyperion_Code { get; set; } = null;
        public string? Base_Hyperion_Description { get; set; } = null;
        public decimal Base_SAP_Amount { get; set; }

        // Doc Currency Properties
        public Guid Grouped_Invoice_Key_Original_Doc { get; set; } = Guid.Empty;
        public decimal Advance_22006_Doc { get; set; }
        public decimal Advance_22071_Doc { get; set; }
        public decimal Advance_22072_Doc { get; set; }
        public decimal Advance_22113_Doc { get; set; }
        public decimal Advance_23021_Doc { get; set; }
        public decimal Advance_23051_Doc { get; set; }
        public decimal Advance_23054_Doc { get; set; }
        public decimal Advance_23057_Doc { get; set; }
        public decimal Advance_23059_Doc { get; set; }
        public decimal Advance_23141_Doc { get; set; }
        public decimal Advance_TotalAdvanceAmount_Doc { get; set; }
        public decimal Advance_Applied_Doc { get; set; }
        public decimal Amount_Doc_Adjusted { get; set; }
        public decimal Total_Remaining_Advance_Doc { get; set; }
        }
}
