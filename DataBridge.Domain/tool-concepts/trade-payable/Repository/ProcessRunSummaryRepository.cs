using Dapper;
using Domain.Aggregates;
using Infrastructure.Contracts;
using Infrastructure.Dapper;



namespace Infrastructure.Repository
{
    using Dapper; // Keep Dapper for the connection execution methods
    using Domain.Models.ProcessRun;
    using Infrastructure.Database;
    using System.Collections.Generic;
    using System.Data;

    public class ProcessRunSummaryRepository(DapperContext dbcontext, IDataSettings datasettings) : IProcessRunSummaryRepository
    {
        private readonly DapperContext _dbcontext = dbcontext;
        private readonly IDataSettings _datasettings = datasettings;

        public async Task<ProcessRun?> GetLastRunAsync()
        {
            var dbName = _datasettings.GetTableConfigDataByKey("TradeMSEDDetails_UAT.dbo");
            var tableName = _datasettings.GetTableConfigDataByKey("TradePayable_Automation_ProcessRunHistory");
            var fullName = $"{dbName}.{tableName}";

            using var connection = _dbcontext.CreateConnection("default");
            var sql = $"SELECT TOP 1 * FROM {fullName} ORDER BY RunDateTime DESC";
            var flatDto = await connection.QueryFirstOrDefaultAsync<ProcessRunFlatDto>(sql, new {TableName=fullName});

            if (flatDto == null)
                return null; 
            return ProcessRun.ToDomainObject(flatDto);
        }

        public async Task<ProcessRun?> GetRunByProcessIdAsync(Guid ProcessId)
        {

            var dbName = _datasettings.GetTableConfigDataByKey("TradeMSEDDetails_UAT.dbo");
            var tableName = _datasettings.GetTableConfigDataByKey("TradePayable_Automation_ProcessRunHistory");
            var fullName = $"{dbName}.{tableName}";

            using var connection = _dbcontext.CreateConnection("default");
            var parameters = new DynamicParameters();
            parameters.Add("@processId", ProcessId, DbType.Guid);

            var sql = $"SELECT * FROM {fullName} WHERE ProcessId = @processId";

            var flatDto = await connection.QueryFirstOrDefaultAsync<ProcessRunFlatDto>(sql, parameters);

            if (flatDto == null)
                return null;

            return ProcessRun.ToDomainObject(flatDto);
        }

        public async Task<IEnumerable<ProcessRun>> GetAllRunsAsync()
        {

            var dbName = _datasettings.GetTableConfigDataByKey("TradePayableDbName");
            var tableName = _datasettings.GetTableConfigDataByKey("TradePayable_Automation_ProcessRunHistory");
            var fullName = $"{dbName}.{tableName}";

            using var connection = _dbcontext.CreateConnection("default");

            // Ordering by latest run first
            var sql = $"SELECT * FROM {fullName} ORDER BY RunDateTime DESC"; 
            var flatDtos = await connection.QueryAsync<ProcessRunFlatDto>(sql);

            if (flatDtos == null || !flatDtos.Any())
                return []; 

            var domainObjects = flatDtos.Select(ProcessRun.ToDomainObject).ToList();
            return domainObjects;
        }

        public async Task SaveProcessRunAsync(ProcessRun run)
        {
            var flatDto = ProcessRunFlatDto.ToFlatDto(run);
            const string storedProcName = "dbo.usp_SaveProcessRunHistory";
            using var connection = _dbcontext.CreateConnection("default");
            await connection.ExecuteAsync(
                storedProcName,
                flatDto, 
                commandType: CommandType.StoredProcedure
            );
        }
    }
}
