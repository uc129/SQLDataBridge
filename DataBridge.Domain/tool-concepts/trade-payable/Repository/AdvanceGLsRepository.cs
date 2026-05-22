using Dapper;
using Domain.Aggregates;
using Infrastructure.Contracts;
using Infrastructure.Dapper;


namespace Infrastructure.Repository
{
    using Dapper;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class AdvanceGLsRepository(DapperContext dbcontext) : IAdvanceGLsRepository
    {
        private readonly DapperContext _dbcontext = dbcontext;

        // READ (All)
        public async Task<IEnumerable<AdvanceGLs>> GetAllAsync()
        {
            string sql = "SELECT [Id], [GL_Code], [IsActive] FROM [dbo].[MASTER_TradePayables_Advance_GLs]";
            using var connection = _dbcontext.CreateConnection("default");
            return await connection.QueryAsync<AdvanceGLs>(sql);
        }

        // READ (Single)
        public async Task<AdvanceGLs?> GetByIdAsync(Guid id)
        {
            string sql = "SELECT [Id], [GL_Code], [IsActive] FROM [dbo].[MASTER_TradePayables_Advance_GLs] WHERE Id = @Id";
            using var connection = _dbcontext.CreateConnection("default");
            return await connection.QueryFirstOrDefaultAsync<AdvanceGLs>(sql, new { Id = id });
        }

        // CREATE
        public async Task<int> CreateAsync(AdvanceGLs entity)
        {
            string sql = "INSERT INTO [dbo].[MASTER_TradePayables_Advance_GLs] ([GL_Code], [IsActive]) VALUES (@GL_Code, @IsActive)";
            using var connection = _dbcontext.CreateConnection("default");
            return await connection.ExecuteScalarAsync<int>(sql, entity);
        }

        // UPDATE
        public async Task<bool> UpdateAsync(AdvanceGLs entity)
        {
            string sql = "UPDATE [dbo].[MASTER_TradePayables_Advance_GLs] SET [GL_Code] = @GL_Code, [IsActive] = @IsActive WHERE [Id] = @Id";
            using var connection = _dbcontext.CreateConnection("default");
            var rowsAffected = await connection.ExecuteAsync(sql, entity);
            return rowsAffected > 0;
        }

        // SOFT DELETE
        public async Task<bool> SoftDeleteAsync(Guid id)
        {
            // We update IsActive to 0 instead of using a DELETE statement
            string sql = @"UPDATE [dbo].[MASTER_TradePayables_Advance_GLs] 
                   SET [IsActive] = 0 
                   WHERE [Id] = @Id";

            using var connection = _dbcontext.CreateConnection("default");
            var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });

            return rowsAffected > 0;
        }

        // DELETE
        public async Task<bool> DeleteAsync(Guid id)
        {
            string sql = "DELETE FROM [dbo].[MASTER_TradePayables_Advance_GLs] WHERE [Id] = @Id";
            using var connection = _dbcontext.CreateConnection("default");
            var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
            return rowsAffected > 0;
        }
    }
}
