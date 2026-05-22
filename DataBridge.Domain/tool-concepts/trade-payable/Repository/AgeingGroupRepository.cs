using Dapper;
using Domain.Aggregates;
using Infrastructure.Contracts;
using Infrastructure.Dapper;


namespace Infrastructure.Repository
{
    public class AgeingGroupRepository(DapperContext dbcontext) : IAgeingGroupRepository
    {
        private readonly DapperContext _dbcontext = dbcontext;

        public async Task<IEnumerable<AgeingGroup>> GetAllAsync()
        {
            string sql = "SELECT [Id],[Group_Code],[Group_Name] FROM [TradeMSEDDetails_UAT].[dbo].[MASTER_TradePayables_Ageing_Group]";
            using var connection = _dbcontext.CreateConnection("default");
            var data = await connection.QueryAsync<AgeingGroup>(sql);
            return data;
        }
    }
}
