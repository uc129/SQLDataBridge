using Dapper;
using Domain.Aggregates;
using Infrastructure.Dapper;
using Infrastructure.Contracts;



namespace Infrastructure.Repository
{
    public class UnclaimedGLsRepository(DapperContext dbcontext) : IUnclaimedGlsRepository
    {
        private readonly DapperContext _dbcontext = dbcontext;

        public async Task<IEnumerable<UnclaimedGLs>> GetAllAsync()
        {
            string sql = "SELECT  [Id] ,[GL_Code] FROM [TradeMSEDDetails_UAT].[dbo].[MASTER_TradePayables_UnclaimedGLs]";
            using var connection = _dbcontext.CreateConnection("default");
            var data = await connection.QueryAsync<UnclaimedGLs>(sql);
            return data;
        }
    }
}
