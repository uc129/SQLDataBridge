using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Aggregates
{

    public class FAGLL03ProcessedGITLocal 
    {
        public FAGLL03ProcessedGITLocal():base() { }
        public string RevisionNumber { get; set; } = null!;

        // Primary joining and grouping keys
        public Guid Grouped_Invoice_Key { get; set; } = Guid.Empty;
        public string Purchasing_Document { get; set; } = null!;
        public string Vendor { get; set; } = null!;
        public string Company_Code { get; set; } = null!;
        public string Composite_Join_Key { get; set; } = null!;
        public string GL_Account { get; set; } = null!;
        public string Profit_Center { get; set; } = null!;
        public decimal Amount_Local { get; set; } // Original Amount
        public string Vendor_Description { get; set; } = null!;
        public string GL_Description { get; set; } = null!;
        public string Industry { get; set; } = null!;
        public string Credit_Period { get; set; } = null!;
        public DateTime Report_Date { get; set; }
        public string ICP_Name { get; set; } = null!;
        public bool IsSNACompany { get; set; }
        public string Vertical { get; set; } = null!;

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

        // --- Adjustment and Final Balance Data ---
        public string Join_Type { get; set; } = null!;
        public string Adjusted_GL { get; set; } = null!;
        public decimal Adjusted_Amount { get; set; }
        public decimal Total_Adjustment { get; set; } // Sum of all adjustments
        public decimal Balance_Local { get; set; } // Final Net Balance

        // --- Process Run Metadata ---
        public Guid ProcessId { get; set; }
        public int StepIndex { get; set; }


    }
}
