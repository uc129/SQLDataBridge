using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Aggregates
{
    public class FAGLL03NetCPFixed:FAGLL03NetLiability
    {
        public FAGLL03NetCPFixed() { }
        public FAGLL03NetCPFixed(FAGLL03NetLiability liabilitydata) : base()
        {
            // Copy all properties from liabilitydata to this instance
            this.Grouped_Invoice_Key_Original = liabilitydata.Grouped_Invoice_Key_Original;
            this.Advance_22006 = liabilitydata.Advance_22006;
            this.Advance_22071 = liabilitydata.Advance_22071;
            this.Advance_22072 = liabilitydata.Advance_22072;
            this.Advance_22113 = liabilitydata.Advance_22113;
            this.Advance_23021 = liabilitydata.Advance_23021;
            this.Advance_23051 = liabilitydata.Advance_23051;
            this.Advance_23054 = liabilitydata.Advance_23054;
            this.Advance_23057 = liabilitydata.Advance_23057;
            this.Advance_23059 = liabilitydata.Advance_23059;
            this.Advance_23141 = liabilitydata.Advance_23141;
            this.Advance_TotalAdvanceAmount = liabilitydata.Advance_TotalAdvanceAmount;
            this.Advance_Applied = liabilitydata.Advance_Applied;
            this.Amount_Local_Adjusted = liabilitydata.Amount_Local_Adjusted;
            this.Adjustment_Type = liabilitydata.Adjustment_Type;
            this.Total_Remaining_Advance = liabilitydata.Total_Remaining_Advance;
            this.Base_Hyperion_Code = liabilitydata.Base_Hyperion_Code;
            this.Base_Hyperion_Description = liabilitydata.Base_Hyperion_Description;
            this.Base_SAP_Amount = liabilitydata.Base_SAP_Amount;
            this.ICP_Name = liabilitydata.ICP_Name;
            this.Entity_Type = liabilitydata.Entity_Type;
            this.Entity_Relation = liabilitydata.Entity_Relation;
            this.Vertical = liabilitydata.Vertical;
        }
        public bool CP_Fixed { get; set; } = false;
    }
}
