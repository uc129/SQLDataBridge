using Dapper;
using Domain.Aggregates;
using Domain.Aggregates.Static_Master_Tables;
using Infrastructure.Contracts;
using Infrastructure.Dapper;


namespace Infrastructure.Repository
{
    public class ICPHyperionMapRepository(DapperContext dbcontext) : IICPHyperionMapRepository
    {
        private readonly DapperContext _dbcontext = dbcontext;

        public async Task<IEnumerable<ICPHyperionMap>> GetAllAsync()
        {
            string sql = "SELECT * FROM [TradeMSEDDetails_UAT].[dbo].[MASTER_TradePayables_ICPHyperionMap]";
            using var connection = _dbcontext.CreateConnection("default");
            var data = await connection.QueryAsync<ICPHyperionMap>(sql);
            return data;
        }
    }
}
