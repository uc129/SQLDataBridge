using Dapper;
using Domain.Aggregates;
using Infrastructure.Contracts;
using Infrastructure.Dapper;


namespace Infrastructure.Repository
{
    public class InsuranceGLsRepository(DapperContext dbcontext) : IInsuranceGLsRepository
    {
        private readonly DapperContext _dbcontext = dbcontext;

        public async Task<IEnumerable<InsuranceGLs>> GetAllAsync()
        {
            string sql = "SELECT [Id] ,[GL_Code] FROM [TradeMSEDDetails_UAT].[dbo].[MASTER_TradePayables_InsurerGLs]";
            using var connection = _dbcontext.CreateConnection("default");
            var data = await connection.QueryAsync<InsuranceGLs>(sql);
            return data;
        }
    }
}
