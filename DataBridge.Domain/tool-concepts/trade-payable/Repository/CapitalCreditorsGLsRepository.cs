using Dapper;
using Domain.Aggregates;
using Infrastructure.Contracts;
using Infrastructure.Dapper;
using Infrastructure.Database;


namespace Infrastructure.Repository
{
    public class CapitalCreditorsGLRepository(DapperContext dbcontext, IDataSettings datasettings) : ICapitalCreditorGLsRepository
    {
        private readonly DapperContext _dbcontext = dbcontext;
        private readonly string TableName = datasettings.MasterTables.MASTER_TradePayables_CapitalGLs;

        public async Task<IEnumerable<CapitalCreditorGLs>> GetAllAsync()
        {
            string sql = $"SELECT [Id], [Gl_Code], [IsActive] FROM {TableName}";
            using var connection = _dbcontext.CreateConnection("default");
            return await connection.QueryAsync<CapitalCreditorGLs>(sql);
        }

        public async Task<CapitalCreditorGLs?> GetByIdAsync(Guid id)
        {
            string sql = $"SELECT [Id], [Gl_Code], [IsActive] FROM {TableName} WHERE Id = @Id";
            using var connection = _dbcontext.CreateConnection("default");
            return await connection.QueryFirstOrDefaultAsync<CapitalCreditorGLs>(sql, new { Id = id });
        }

        public async Task<int> CreateAsync(CapitalCreditorGLs entity)
        {
            // Generate a new Guid if it's empty
            if (entity.Id == Guid.Empty) entity.Id = Guid.NewGuid();

            string sql = $@"INSERT INTO {TableName} ([Id], [Gl_Code], [IsActive]) 
                        VALUES (@Id, @Gl_Code, @IsActive)";

            using var connection = _dbcontext.CreateConnection("default");
            return await connection.ExecuteAsync(sql, entity);
        }

        public async Task<int> UpdateAsync(CapitalCreditorGLs entity)
        {
            string sql = $@"UPDATE {TableName} 
                        SET [Gl_Code] = @Gl_Code, [IsActive] = @IsActive 
                        WHERE [Id] = @Id";

            using var connection = _dbcontext.CreateConnection("default");
            return await connection.ExecuteAsync(sql, entity);
        }

        public async Task<int> DeleteAsync(Guid id)
        {
            string sql = $"DELETE FROM {TableName} WHERE [Id] = @Id";

            using var connection = _dbcontext.CreateConnection("default");
            return await connection.ExecuteAsync(sql, new { Id = id });
        }
    }
}
