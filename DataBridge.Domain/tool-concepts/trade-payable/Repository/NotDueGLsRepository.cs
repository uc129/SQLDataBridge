using Dapper;
using Domain.Aggregates;
using Infrastructure.Contracts;
using Infrastructure.Dapper;


namespace Infrastructure.Repository
{
    public class NotDueGLsRepository(DapperContext dbcontext) : INotDueGLsRepository
    {
        private readonly DapperContext _dbcontext = dbcontext;

        public async Task<IEnumerable<NotDueGLs>> GetAllAsync()
        {
            string sql = "SELECT [Id],[GL_Code] FROM [TradeMSEDDetails_UAT].[dbo].[MASTER_TradePayables_NotDueGL]";
            using var connection = _dbcontext.CreateConnection("default");
            var data = await connection.QueryAsync<NotDueGLs>(sql);
            return data;
        }
    }
}
