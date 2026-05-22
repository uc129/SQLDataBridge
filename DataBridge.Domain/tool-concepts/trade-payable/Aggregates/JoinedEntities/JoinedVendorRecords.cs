using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Aggregates.JoinedEntities
{
    public class JoinedVendorRecords : JoinedPOVendor
    {
        public JoinedVendorRecords()
        {
        }
        public JoinedVendorRecords(JoinedPOVendor povendor):base(povendor)
        {
        }

        public string? PKC_Vendor_Code { get; set; } = null;
        public string? PKC_Company_Code { get; set; } = null;
        public string? C_Vendor_Name { get; set; } = null;
        public string? ZTERM { get; set; } = null;
        public string? Industry_Type { get; set; } = null;
    }
}
