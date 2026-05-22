using Dapper;
using Domain.Aggregates.Static_Master_Tables;
using Infrastructure.Contracts;
using Infrastructure.Dapper;


namespace Infrastructure.Repository
{


    public class GLHyperionMapRepository(DapperContext dbcontext) : IGLHyperionMapRepository
    {
        private readonly DapperContext _dbcontext = dbcontext;

        // READ ALL
        public async Task<IEnumerable<GLHyperionMap>> GetAllAsync()
        {
            string sql = "SELECT * FROM [dbo].[MASTER_TradePayables_GLHyperionMap]";
            using var connection = _dbcontext.CreateConnection("default");
            try
            {
                var data = await connection.QueryAsync<GLHyperionMap>(sql);
                return data;
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                return [];
            }
            
        }

        // READ SINGLE
        public async Task<GLHyperionMap?> GetByCodeAsync(string glCode)
        {
            string sql = "SELECT [GLCode], [Hyperion_Code], [Hyperion_Description], [Billed_Status] FROM [dbo].[MASTER_TradePayables_GLHyperionMap] WHERE [GLCode] = @GLCode";
            using var connection = _dbcontext.CreateConnection("default");
            return await connection.QueryFirstOrDefaultAsync<GLHyperionMap>(sql, new { GLCode = glCode });
        }

        // CREATE
        public async Task<bool> CreateAsync(GLHyperionMap entity)
        {
            string sql = @"INSERT INTO [dbo].[MASTER_TradePayables_GLHyperionMap] ([GLCode], [Hyperion_Code], [Hyperion_Description], [Billed_Status]) 
                       VALUES (@GLCode, @Hyperion_Code, @Hyperion_Description, @Billed_Status)";
            using var connection = _dbcontext.CreateConnection("default");
            var rowsAffected = await connection.ExecuteAsync(sql, entity);
            return rowsAffected > 0;
        }

        // UPDATE
        public async Task<bool> UpdateAsync(GLHyperionMap entity)
        {
            string sql = @"UPDATE [dbo].[MASTER_TradePayables_GLHyperionMap] 
                       SET [Hyperion_Code] = @Hyperion_Code, 
                           [Hyperion_Description] = @Hyperion_Description, 
                           [Billed_Status] = @Billed_Status,
                           [IsActive] =@IsActive
                       WHERE [GLCode] = @GLCode";
            using var connection = _dbcontext.CreateConnection("default");
            try
            {
                var rowsAffected = await connection.ExecuteAsync(sql, entity);
                return rowsAffected > 0;
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception occured" + ex.Message);
                return false;
            }
            
        }

        // DELETE
        public async Task<bool> DeleteAsync(string glCode)
        {
            string sql = "DELETE FROM [dbo].[MASTER_TradePayables_GLHyperionMap] WHERE [GLCode] = @GLCode";
            using var connection = _dbcontext.CreateConnection("default");
            var rowsAffected = await connection.ExecuteAsync(sql, new { GLCode = glCode });
            return rowsAffected > 0;
        }
    }
}
