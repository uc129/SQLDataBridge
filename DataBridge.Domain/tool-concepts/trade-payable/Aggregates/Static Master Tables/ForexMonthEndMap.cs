using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Aggregates.Static_Master_Tables
{
    public class ForexMonthEndMap
    {
        public DateTime Date { get; set; }
        public string Currency { get; set; } = null!;
        public decimal Conversion_Rate { get; set; }

    }
}
