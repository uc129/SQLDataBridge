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
        // Read - Already defined by you, kept for completeness
        public async Task<IEnumerable<CapitalCreditorGLs>> GetCapitalCreditorGLs()
        {
            return await _capitalcreditorglsrepo.GetAllAsync();
        }

        // Read by ID
        public async Task<CapitalCreditorGLs?> GetCapitalCreditorGLById(Guid id)
        {
            if (id == Guid.Empty) return null;
            return await _capitalcreditorglsrepo.GetByIdAsync(id);
        }

        // Create
        public async Task<bool> CreateCapitalCreditorGL(CapitalCreditorGLs entity)
        {
            // Add business logic: Check if GL Code already exists if necessary
            // Or ensure IsActive is set to true by default
            entity.IsActive = true;

            int rowsAffected = await _capitalcreditorglsrepo.CreateAsync(entity);
            return rowsAffected > 0;
        }

        // Update
        public async Task<bool> UpdateCapitalCreditorGL(CapitalCreditorGLs entity)
        {
            if (entity.Id == Guid.Empty) return false;

            int rowsAffected = await _capitalcreditorglsrepo.UpdateAsync(entity);
            return rowsAffected > 0;
        }

        // Delete
        public async Task<bool> DeleteCapitalCreditorGL(Guid id)
        {
            if (id == Guid.Empty) return false;

            int rowsAffected = await _capitalcreditorglsrepo.DeleteAsync(id);
            return rowsAffected > 0;
        }
    }
}
