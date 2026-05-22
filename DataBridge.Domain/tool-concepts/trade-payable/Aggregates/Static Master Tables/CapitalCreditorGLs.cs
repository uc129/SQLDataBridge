using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Aggregates
{
    public class CapitalCreditorGLs
    {
        public Guid Id { get; set; } = Guid.Empty;
        public string Gl_Code { get; set; } = null!;
        public bool IsActive { get; set; }
    }
}
