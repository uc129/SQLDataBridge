using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Aggregates.Static_Master_Tables
{
    public class GLHyperionMap
    {
        public string GLCode { get; set; } = null!;
        public string Hyperion_Code { get; set; } = null!;
        public string Hyperion_Description { get; set; } = null!;
        public string Billed_Status { get; set; } = null!;
        public bool IsActive { get; set; }
    }
}
