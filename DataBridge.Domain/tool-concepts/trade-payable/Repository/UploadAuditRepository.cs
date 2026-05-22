using Dapper;
using Domain.Models.DataUpload;
using Infrastructure.Contracts;
using Infrastructure.Dapper;
using Infrastructure.Database;


namespace Infrastructure.Repository
{


    public class UploadAuditRepository(DapperContext dbcontext, IDataSettings datasettings) : IUploadAuditRepository
    {
        private readonly DapperContext _dbcontext = dbcontext;
        private readonly IDataSettings _datasettings = datasettings;


        public async Task<IEnumerable<DataUploadAuditEntity>> GetRecentUploadsAsync(int number)
        {
            var dbName = _datasettings.GetTableConfigDataByKey("TradePayableDbName");
            var tableName = _datasettings.GetTableConfigDataByKey("DataUploadAuditTable");
            var fullName = $"{dbName}.{tableName}";

            using var connection = _dbcontext.CreateConnection("default");

            //var sql = $"SELECT TOP (@NumberOfRecords) * FROM {fullName} ORDER BY UploadedDate DESC";
            var sql = $"SELECT TOP ({number}) * FROM {fullName} ORDER BY UploadedDate DESC";

            var data = await connection.QueryAsync<DataUploadAuditEntity>(sql, new { NumberOfRecords = number });

            if (data == null || !data.Any())
            {
                System.Diagnostics.Debug.WriteLine($"No data found in Table {fullName}");
                return [];
            }

            return data;
        }

        public async Task LogUploadAsync(DataUploadAuditEntity audit)
        {
            var dbName = _datasettings.GetTableConfigDataByKey("TradePayableDbName");
            var tableName = _datasettings.GetTableConfigDataByKey("DataUploadAuditTable");
            var fullName = $"{dbName}.{tableName}";
            using var connection = _dbcontext.CreateConnection("default");
            var sql = $@"INSERT INTO {fullName} 
                        (SourceFileName, UploadedBy, UploadedDate, FileURL, QuarterEndDate, RevisionNumber) 
                        VALUES 
                        (@SourceFileName, @UploadedBy, @UploadedDate, @FileURL, @QuarterEndDate, @RevisionNumber)";
            try
            {
                await connection.ExecuteAsync(sql, audit);
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception occured: " + ex.Message);
            }
        }
    }
}
