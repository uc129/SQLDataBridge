using Dapper;
using Domain.Aggregates;
using Domain.Contracts;
using Infrastructure.Contracts;
using Infrastructure.Dapper;


namespace Infrastructure.Repository
{
    public class NonMSMEGLsRepository(DapperContext dbcontext) : INonMSMEGlsRepository
    {
        private readonly DapperContext _dbcontext = dbcontext;

        public async Task<IEnumerable<NonMSMEGLs>> GetAllAsync()
        {
            string sql = "SELECT [Id],[GL_Code] FROM [TradeMSEDDetails_UAT].[dbo].[MASTER_TradePayables_NonMSMEGLs]";
            using var connection = _dbcontext.CreateConnection("default");
            var data = await connection.QueryAsync<NonMSMEGLs>(sql);
            return data;
        }
    }
}
