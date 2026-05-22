

namespace Domain.Aggregates.Static_Master_Tables
{
    public class ICPVendorMap
    {
        public string Vendor_Code { get; set; } = null!;
        public string Vendor_Name { get; set; } = null!;
        public string ICP_Name { get; set; } = null!;
        public string Entity_Type { get; set; } = null!;
        public string Entity_Relation { get; set; } = null!;
        public string Approver_PSNO { get; set; } = null!;
        public string Approver_Name { get; set; } = null!;
        public bool IsActive { get; set; } = true;
    }
}
