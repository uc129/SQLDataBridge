using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//SELECT[pkc_vendor_code],[pkc_company_code],[c_vendor_name], [ZTERM], [industry_type] FROM[Lnt_PO_Data].[dbo].[m_Vendor]
namespace Domain.Aggregates
{
    public class VendorRecords
    {
        public string? PKC_Vendor_Code { get; set; } = null;
        public string? PKC_Company_Code { get; set; } = null;
        public string? C_Vendor_Name { get; set; } = null;
        public string? ZTERM { get; set; } = null;
        public string? Industry_Type { get; set; } = null;

        public VendorRecords() { }
    }
}
