using Dapper;
using Domain.Aggregates;
using Domain.Aggregates.Static_Master_Tables;
using Infrastructure.Contracts;
using Infrastructure.Dapper;


namespace Infrastructure.Repository
{
    public class ForexMonthEndRepository(DapperContext dbcontext): IForexMonthEndMapRepository
    {
         private readonly DapperContext _dbcontext = dbcontext;

        public async Task<IEnumerable<ForexMonthEndMap>> GetAllAsync()
        {
            string sql = "SELECT * FROM [TradeMSEDDetails_UAT].[dbo].[MASTER_TradePayables_MonthEndForex]";
            using var connection = _dbcontext.CreateConnection("default"); // Current correct way
            var data = await connection.QueryAsync<ForexMonthEndMap>(sql);
            return data;
        }
    }
}
