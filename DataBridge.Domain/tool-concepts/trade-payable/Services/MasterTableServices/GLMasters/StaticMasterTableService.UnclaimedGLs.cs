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
        public async Task<IEnumerable<UnclaimedGLs>> GetUnclaimedGls()
        {
            System.Diagnostics.Debug.WriteLine("Fetching all UnclaimedGls data.");
            var data = await _unclaimedglsrepo.GetAllAsync();
            return data;
        }
    }
}
