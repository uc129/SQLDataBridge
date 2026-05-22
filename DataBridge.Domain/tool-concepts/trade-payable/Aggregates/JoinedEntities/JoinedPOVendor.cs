using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Aggregates.JoinedEntities
{
    public class JoinedPOVendor: JoinedPOCreditPeriod
    {

        public JoinedPOVendor(JoinedPOCreditPeriod cprecords):base(cprecords)
        {
        }
        public JoinedPOVendor()
        {
        }
        public string? EBELN { get; set; } = null;
        public string? LIFNR { get; set; } = null;
    }
}
