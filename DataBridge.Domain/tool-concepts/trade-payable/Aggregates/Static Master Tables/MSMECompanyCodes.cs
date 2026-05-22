using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Aggregates
{
    public class MSMECompanyCodes
    {
        public Guid Id { get; set; } = Guid.Empty;
        public string Company_Code { get; set; } = null!;
    }
}
