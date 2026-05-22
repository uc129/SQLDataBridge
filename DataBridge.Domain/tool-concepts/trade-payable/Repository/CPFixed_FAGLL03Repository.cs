using Dapper;
using Domain.Aggregates;
using Infrastructure.Contracts;
using Infrastructure.Dapper;

namespace Infrastructure.Repository
{
    public class CPFixed_FAGLL03Repository(DapperContext dbcontext) : ICPFixed_FAGLL03Repository
    {
        private readonly DapperContext _dbcontext = dbcontext;

        public async Task<IEnumerable<FAGLL03ProcessedResult>> GetAllAsync()
        {
            string sql = "SELECT * FROM [TradeMSEDDetails_UAT].[dbo].[V_TradePayable_MSMECreditPeriodFixed_Step7]";
            using var connection = _dbcontext.CreateConnection("default");
            var data =  await connection.QueryAsync<FAGLL03ProcessedResult>(sql);
            return data;
        }

        public async Task<IEnumerable<FAGLL03ProcessedResult>> GetPaginatedData(int pageSize, int skip)
        {
            string sql = @"SELECT * FROM [TradeMSEDDetails_UAT].[dbo].[V_TradePayable_MSMECreditPeriodFixed_Step7]
                        ORDER BY [Document_Number] ASC
                        OFFSET @Skip ROWS
                        FETCH NEXT @PageSize ROWS ONLY;";

            using var connection = _dbcontext.CreateConnection("default");
            var data = await connection.QueryAsync<FAGLL03ProcessedResult>(sql, new { Skip = skip, PageSize = pageSize });
            if (data != null && data.Any()) { 
                return data;
            }
            else
            {
                throw new Exception("Error retreiving");
            }
        }

        public Task<FAGLL03ProcessedResult> GetByDocumentNumberAsync(string DocNum)
        {
            throw new NotImplementedException();
        }

        public Task<FAGLL03ProcessedResult> GetByPOAsync(string PO)
        {
            throw new NotImplementedException();
        }

        public Task<FAGLL03ProcessedResult> GetByProfitCenterAsync(string PC)
        {
            throw new NotImplementedException();
        }

        public Task<FAGLL03ProcessedResult> GetByVendorCodeAsync(string vendorCode)
        {
            throw new NotImplementedException();
        }
    }
}
