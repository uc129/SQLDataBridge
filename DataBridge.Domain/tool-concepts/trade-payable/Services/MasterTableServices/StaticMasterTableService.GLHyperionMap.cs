using Domain.Aggregates.Static_Master_Tables;
using Domain.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.MasterTableServices
{
    //_glhyperionmaprepo

    public partial class StaticMasterTableService
    {
        public async Task<IEnumerable<GLHyperionMap>> GetGLHyperionMaps()
        {
            return await _glhyperionmaprepo.GetAllAsync();
        }

        public async Task<bool> CreateGLHyperionMap(GLHyperionMap entity)
        {
            // Validation: Check if GLCode already exists
            var existing = await _glhyperionmaprepo.GetByCodeAsync(entity.GLCode);
            if (existing != null)
                throw new InvalidOperationException($"Mapping for GL Code {entity.GLCode} already exists.");

            return await _glhyperionmaprepo.CreateAsync(entity);
        }

        public async Task<bool> UpdateGLHyperionMap(GLHyperionMap entity)
        {
            return await _glhyperionmaprepo.UpdateAsync(entity);
        }

        public async Task<bool> DeleteGLHyperionMap(string glCode)
        {
            return await _glhyperionmaprepo.DeleteAsync(glCode);
        }
    }
}
