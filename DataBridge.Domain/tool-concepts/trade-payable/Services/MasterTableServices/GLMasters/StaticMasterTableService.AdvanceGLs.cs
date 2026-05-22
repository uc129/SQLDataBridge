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
        // READ (Single)
        public async Task<AdvanceGLs?> GetAdvanceGLById(Guid id)
        {
            System.Diagnostics.Debug.WriteLine($"Fetching AdvanceGL with ID: {id}");
            return await _advanceglrepo.GetByIdAsync(id);
        }

        // READ (All)
        public async Task<IEnumerable<AdvanceGLs>> GetAdvanceGLs()
        {
            System.Diagnostics.Debug.WriteLine("Fetching all AdvanceGL data.");
            var data = await _advanceglrepo.GetAllAsync();
            return data;
        }

        // CREATE
        public async Task<int> CreateAdvanceGL(AdvanceGLs entity)
        {
            if (string.IsNullOrWhiteSpace(entity.GL_Code))
                throw new ArgumentException("GL Code cannot be empty.");

            var existingData = await _advanceglrepo.GetAllAsync();
            if (existingData.Any(x => x.GL_Code == entity.GL_Code))
                throw new InvalidOperationException("This GL Code already exists.");

            System.Diagnostics.Debug.WriteLine($"Inserting new AdvanceGL: {entity.GL_Code}");
            entity.IsActive = true;
            return await _advanceglrepo.CreateAsync(entity);
        }

        // UPDATE
        public async Task<bool> UpdateAdvanceGL(AdvanceGLs entity)
        {
            System.Diagnostics.Debug.WriteLine($"Updating AdvanceGL ID: {entity.Id}");
            return await _advanceglrepo.UpdateAsync(entity);
        }

        // SOFT DELETE
        public async Task<bool> DeleteAdvanceGL(Guid id)
        {
            System.Diagnostics.Debug.WriteLine($"Soft deleting AdvanceGL ID: {id}");
            return await _advanceglrepo.SoftDeleteAsync(id);
        }

        // HARD DELETE (Optional - use with caution)
        public async Task<bool> PermanentDeleteAdvanceGL(Guid id)
        {
            System.Diagnostics.Debug.WriteLine($"Permanently removing AdvanceGL ID: {id}");
            return await _advanceglrepo.DeleteAsync(id);
        }
    }
}
