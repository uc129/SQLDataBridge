using Dapper;
using Domain.Aggregates;
using Domain.Contracts;
using Infrastructure.Contracts;
using Infrastructure.Dapper;


namespace Infrastructure.Repository
{
    public class MSMECCRepository(DapperContext dbcontext) : IMSMECCRepository
    {
        private readonly DapperContext _dbcontext = dbcontext;

        public async Task<IEnumerable<MSMECompanyCodes>> GetAllAsync()
        {
            string sql = "SELECT [Id],[Company_Code] FROM [TradeMSEDDetails_UAT].[dbo].[MASTER_TradePayables_MSMECompanyCodes]";
            using var connection = _dbcontext.CreateConnection("default");
            var data = await connection.QueryAsync<MSMECompanyCodes>(sql);
            return data;
        }
    }
}
