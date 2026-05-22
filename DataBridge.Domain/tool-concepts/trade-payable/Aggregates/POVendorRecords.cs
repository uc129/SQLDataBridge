using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
//SELECT[ebeln], MAX([lifnr]) as lifnr, MAX([name1]) as vendor_name FROM[Lnt_PO_Data].[dbo].[podata] GROUP BY[ebeln]
namespace Domain.Aggregates
{
    public class POVendorRecords
    {
        public string? EBELN { get; set; } = null;
        public string? LIFNR { get; set; } = null;
        public string? Vendor_Name { get; set; } = null;
        public POVendorRecords() { }
    }
}
