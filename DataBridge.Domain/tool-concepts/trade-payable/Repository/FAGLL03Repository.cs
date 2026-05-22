using Dapper;
using Domain.Aggregates;
using Infrastructure.Contracts;
using Infrastructure.Dapper;
using Infrastructure.Database;

namespace Infrastructure.Repository
{
    public class FAGLL03Repository(DapperContext dbcontext, IDataSettings datasettings) : IFAGLL03Repository
    {
        private readonly DapperContext _dbcontext = dbcontext;
        private readonly IDataSettings _dataSettings = datasettings;

        public async Task<IEnumerable<FAGLL03RAWEntity>> GetAllAsync()
        {
            string schema = _dataSettings.TradePayableDbName;
            string tableName = _dataSettings.TradePayable_RawDataTableName;

            string sql = $@"SELECT * FROM {schema}.{tableName}";
            using var connection = _dbcontext.CreateConnection("default");
            var data =  await connection.QueryAsync<FAGLL03RAWEntity>(sql);

            if (!data.Any()) {
                throw new Exception("Error Retrieving Raw Data");
            }
            return data;
        }
        public async Task<IEnumerable<FAGLL03RAWEntity>> GetPaginatedDataAsync(int pageSize, int skip)
        {
            string schema = _dataSettings.TradePayableDbName;
            string tableName = _dataSettings.TradePayable_RawDataTableName;

            string sql = $@"SELECT * FROM {schema}.{tableName} ORDER BY [Document_Number] ASC
                        OFFSET @Skip ROWS
                        FETCH NEXT @PageSize ROWS ONLY;";

            //string sql = @"SELECT * FROM [TradeMSEDDetails_UAT].[dbo].[TradePayabaleDataDumpExtended]
            //            ORDER BY [Document_Number] ASC
            //            OFFSET @Skip ROWS
            //            FETCH NEXT @PageSize ROWS ONLY;";

            using var connection = _dbcontext.CreateConnection("default");
            var data = await connection.QueryAsync<FAGLL03RAWEntity>(sql,new { Skip = skip, PageSize = pageSize });
            if (!data.Any())
            {
                throw new Exception("Error Retrieving Raw Data");
            }
            return data;
        }
        public async Task<IEnumerable<FAGLL03RAWEntity>> GetByRevisionNumber(string rev)
        {
            string schema = _dataSettings.TradePayableDbName;
            string tableName = _dataSettings.TradePayable_RawDataTableName;

            string sql =$@"SELECT * FROM {schema}.{tableName}  WHERE RevisionNumber = @RevisionNumber";

            var parameters = new { RevisionNumber = rev };

            try
            {
                using var connection = _dbcontext.CreateConnection("default");
                var data = await connection.QueryAsync<FAGLL03RAWEntity>(sql, parameters);
                var resultList = data.ToList();

                if (resultList.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"No data found for Raw Data. RevisonNumber:{rev}!");
                    System.Diagnostics.Debugger.Break();
                }

                return resultList;
            }
            catch (System.Data.Common.DbException ex)
            {
                throw new ApplicationException($"Database operation failed while retrieving data for revision {rev}.", ex);
            }
        }


        public Task<FAGLL03RAWEntity> GetByDocumentNumberAsync(string DocNum)
        {
            throw new NotImplementedException();
        }
        public Task<FAGLL03RAWEntity> GetByPOAsync(string PO)
        {
            throw new NotImplementedException();
        }
        public Task<FAGLL03RAWEntity> GetByProfitCenterAsync(string PC)
        {
            throw new NotImplementedException();
        }
        public Task<FAGLL03RAWEntity> GetByVendorCodeAsync(string vendorCode)
        {
            throw new NotImplementedException();
        }
    }
}
