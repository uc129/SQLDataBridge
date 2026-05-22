using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Aggregates
{
    public class AgeingGroup
    {
        public Guid Id { get; set; } = Guid.Empty;
        public int Group_Code { get; set; }
        public string Group_Name { get; set; } = null!;
    }
}
