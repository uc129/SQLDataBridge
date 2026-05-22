using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Aggregates
{
    public class FAGLL03_JoinedAndMerged: FAGLL03Populated
    {
        public FAGLL03_JoinedAndMerged() { }
        public FAGLL03_JoinedAndMerged(FAGLL03_JoinedAndMerged existing) : base(existing)
        {
            this.Vendor_Merged = existing.Vendor_Merged;
            this.CP_Merged = existing.CP_Merged;
            this.Industry_Merged = existing.Industry_Merged;
            this.Vendor_Code = existing.Vendor_Code;
            this.Vendor_Name = existing.Vendor_Name;
            this.ICP_Name = existing.ICP_Name;
            this.Entity_Type = existing.Entity_Type;
            this.Entity_Relation = existing.Entity_Relation;

        }
        public string? Credit_Period { get; set; } = null;
        public bool Vendor_Merged { get; set; } = false;
        public bool CP_Merged { get; set; } = false;
        public bool Industry_Merged { get; set; } = false;
        public string? Vendor_Code { get; set; } = null;
        public string? Vendor_Name { get; set; } = null;
        public string? ICP_Name { get; set; } = null;
        public string? Vertical {  get; set; } = null;
        public string? Entity_Type { get; set; } = null;
        public string? Entity_Relation { get; set; } = null;
        public bool IsSNACompany { get; set; } = false;
    }

}
