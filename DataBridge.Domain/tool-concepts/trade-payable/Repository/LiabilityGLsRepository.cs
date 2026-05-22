using Dapper;
using Domain.Aggregates;
using Domain.Contracts;
using Infrastructure.Contracts;
using Infrastructure.Dapper;


namespace Infrastructure.Repository
{
    public class LiabilityGLsRepository(DapperContext dbcontext) : ILiabilityGLsRepository
    {
        private readonly DapperContext _dbcontext = dbcontext;

        public async Task<IEnumerable<LiabilityGLs>> GetAllAsync()
        {
            string sql = "SELECT [Id],[GL_Code] FROM [TradeMSEDDetails_UAT].[dbo].[MASTER_TradePayables_Liability_GLs]";
            using var connection = _dbcontext.CreateConnection("default");
            var data = await connection.QueryAsync<LiabilityGLs>(sql);
            return data;
        }
    }
}
