using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Aggregates.JoinedEntities
{
    public class JoinedPOCreditPeriod : FAGLL03_JoinedAndMerged
    {
        public JoinedPOCreditPeriod() { }
        public JoinedPOCreditPeriod(FAGLL03_JoinedAndMerged rawrecords):base(rawrecords) {}
        public string? PurchasingDoc { get; set; } = null;
    }
}
