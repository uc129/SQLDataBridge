using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Aggregates.Static_Master_Tables
{
    public class ICPHyperionMap
    {
        public string ICP_Name { get; set; } = null!;
        public string? Hyperion_Credit { get; set; } = null!;
        public string? Hyperion_Debit { get; set; } = null!;
    }
}
