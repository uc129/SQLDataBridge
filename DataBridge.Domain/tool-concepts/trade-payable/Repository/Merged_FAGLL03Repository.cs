using Dapper;
using Domain.Aggregates;
using Infrastructure.Contracts;
using Infrastructure.Dapper;
using Infrastructure.Database;

namespace Infrastructure.Repository
{
    public class Merged_FAGLL03Repository(DapperContext dbcontext, IDataSettings datasettings) : IMerged_FAGLL03Repository
    {
        private readonly DapperContext _dbcontext = dbcontext;
        private readonly IDataSettings _dataSettings = datasettings;


        public async Task<IEnumerable<FAGLL03_JoinedAndMerged>> GetAllAsync()
        {
            string schema = _dataSettings.TradePayableDbName;
            string tableName = _dataSettings.TradePayable_MergedDataTableName;
            string sql = $@"SELECT * FROM {schema}.{tableName}";
            const int LONG_TIMEOUT_SECONDS = 300; // 5 minutes
            using var connection = _dbcontext.CreateConnection("default");
            var data =  await connection.QueryAsync<FAGLL03_JoinedAndMerged>(sql, commandTimeout:LONG_TIMEOUT_SECONDS);
            var sortedData = data.OrderBy(item => item.GL_Account).ToList();
            return sortedData;
        }

        public async Task<IEnumerable<FAGLL03_JoinedAndMerged>> GetPaginatedData(int pageSize, int skip)
        {

            string schema = _dataSettings.TradePayableDbName;
            string tableName = _dataSettings.TradePayable_MergedDataTableName;

            string sql = $@"SELECT * FROM {schema}.{tableName}  ORDER BY [Document_Number] ASC
                        OFFSET @Skip ROWS
                        FETCH NEXT @PageSize ROWS ONLY;";


            using var connection = _dbcontext.CreateConnection("default");
            var data = await connection.QueryAsync<FAGLL03_JoinedAndMerged>(sql, new { Skip = skip, PageSize = pageSize });
            if (data != null && data.Any()) { 
                return data;
            }
            else
            {
                throw new Exception("Error retreiving");
            }
        }
        public async Task<IEnumerable<FAGLL03_JoinedAndMerged>> GetByRevisionNumber(string rev)
        {
            string schema = _dataSettings.TradePayableDbName;
            string tableName = _dataSettings.TradePayable_MergedDataTableName;

            string sql = $@"SELECT * FROM {schema}.{tableName}  WHERE RevisionNumber = @RevisionNumber";

            var parameters = new { RevisionNumber = rev };

            try
            {
                using var connection = _dbcontext.CreateConnection("default");
                var data = await connection.QueryAsync<FAGLL03_JoinedAndMerged>(sql, parameters);
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





        public Task<FAGLL03_JoinedAndMerged> GetByDocumentNumberAsync(string DocNum)
        {
            throw new NotImplementedException();
        }

        public Task<FAGLL03_JoinedAndMerged> GetByPOAsync(string PO)
        {
            throw new NotImplementedException();
        }

        public Task<FAGLL03_JoinedAndMerged> GetByProfitCenterAsync(string PC)
        {
            throw new NotImplementedException();
        }

        public Task<FAGLL03_JoinedAndMerged> GetByVendorCodeAsync(string vendorCode)
        {
            throw new NotImplementedException();
        }
    }
}
