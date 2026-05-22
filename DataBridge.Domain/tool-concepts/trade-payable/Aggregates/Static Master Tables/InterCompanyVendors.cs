using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Aggregates
{
    public class InterCompanyVendors
    {
        public Guid Id { get; set; } = Guid.Empty;
        public string Vendor_Code { get; set; } = null!;
        public string Vendor_Name { get; set; } = null!;
        public string ICP_Name { get; set; } = null!;
        public string Entity_Type { get; set; } = null!;
        public string Entity_Relation { get; set; } = null!;

    }
}
