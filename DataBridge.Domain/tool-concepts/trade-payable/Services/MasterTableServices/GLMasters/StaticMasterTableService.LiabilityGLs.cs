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
        public async Task<IEnumerable<LiabilityGLs>> GetLiabilityGLs()
        {
            System.Diagnostics.Debug.WriteLine("Fetching all LiabilityGLs data.");
            var data = await _liabilityglsrepo.GetAllAsync();
            return data;
        }


    }
}
