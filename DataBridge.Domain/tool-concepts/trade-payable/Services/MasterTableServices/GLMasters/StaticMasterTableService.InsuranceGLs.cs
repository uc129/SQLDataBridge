using Domain.Aggregates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.MasterTableServices
{
    public partial class StaticMasterTableService
    {
        public async Task<IEnumerable<InsuranceGLs>> GetInsuranceGLs()
        {
            System.Diagnostics.Debug.WriteLine("Fetching all InsuranceGLs data.");
            var data = await _insuranceglsrepo.GetAllAsync();
            return data;
        }


    }
}
