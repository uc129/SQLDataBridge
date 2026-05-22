using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//SELECT  [PurchasingDoc] ,MAX([CreditPeriod]) AS Credit_Period FROM [Lnt_PO_Data].[dbo].[POTemsfromSAP] GROUP BY [PurchasingDoc]
namespace Domain.Aggregates
{
    public class POCreditPeriod
    {
        public string? PurchasingDoc { get; set; } = null;
        public string? Credit_Period { get; set; } = null;
        public POCreditPeriod() { }
    }
}
