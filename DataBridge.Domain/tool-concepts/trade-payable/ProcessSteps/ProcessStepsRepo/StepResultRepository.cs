using Application.Extensions;
using Dapper;
using Domain.Shared;
using Infrastructure.Dapper;
using Infrastructure.Database;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Application.ProcessSteps.ProcessStepsRepo
{
    public class StepResultRepository(
        DapperContext dbcontext, 
        IDataSettings dataSettings, 
        DataToDB dbhelper
        ) : IStepResultsRepository
    {
        private readonly DapperContext _dbcontext = dbcontext;
        private readonly IDataSettings _datasettings = dataSettings;
        private readonly DataToDB _dbhelper = dbhelper;

        public async Task<Message> SaveAndReplaceStepResultAsync(DataTable data, Guid processId, int stepIndex)
        {
            var tablename = GetStepsTableNameByStepIndex(stepIndex);
            var message = new Message(
                title: $"ProcessId={processId}",
                text: $"Saving Data in {tablename} for step {stepIndex}",
                success: true);

            if (!data.Columns.Contains("ProcessId"))
            {
                data.Columns.Add("ProcessId", typeof(Guid));
            }

            if (!data.Columns.Contains("StepIndex"))
            {
                data.Columns.Add("StepIndex", typeof(int));
            }

            foreach (DataRow row in data.Rows)
            {
                row["ProcessId"] = processId;
                row["StepIndex"] = stepIndex;
            }


            try
            {
                var connection = _dbcontext.CreateConnection("default");
                var success = await _dbhelper.ProcessAndSaveLargeData(data, tablename);
                message.Success = success;
                return message;
            }
            catch
            {
                message.Success = false;
                return message;
            }

        }

        public async Task<Message> SaveAndAppendStepResultAsync(DataTable data, Guid processId, int stepIndex)
        {
            var tablename = GetStepsTableNameByStepIndex(stepIndex);
            var dbName = _datasettings.GetTableConfigDataByKey("TradePayableDbName");

            var message = new Message(
                title: $"ProcessId={processId}",
                text: $"Appending Data to {tablename} for step {stepIndex}",
                success: true);

            if (!data.Columns.Contains("ProcessId")) data.Columns.Add("ProcessId", typeof(Guid));
            if (!data.Columns.Contains("StepIndex")) data.Columns.Add("StepIndex", typeof(int));

            foreach (DataRow row in data.Rows)
            {
                row["ProcessId"] = processId;
                row["StepIndex"] = stepIndex;
            }

            var saveMessage = await _dbhelper.ProcessAndInsertData(data, tablename, dbName, processId, stepIndex);
            if (saveMessage.Success)
            {
                System.Diagnostics.Debug.WriteLine($"Success inserting data into table {tablename} for stepIndex {stepIndex}");
                return message;

            }
            else
                return saveMessage;
        }

        public async Task<DataTable> RetrieveStepResultAsync(Guid processId, int stepIndex)
        {
            // 1. Get the table name securely
            var tablename = GetStepsTableNameByStepIndex(stepIndex);
            var dbName = _datasettings.GetTableConfigDataByKey("TradePayableDbName");
            var fullTableName = $"${dbName}.{tablename}";
            DataTable resultTable = new($"Results_Step_{stepIndex}");

            try
            {
                using var connection = _dbcontext.CreateConnection("default");
                string checkSql = $"SELECT 1 FROM sys.tables WHERE name = @TableName";
                var tableExists = await connection.QuerySingleOrDefaultAsync<int?>(
                    checkSql,
                    new { TableName = tablename }
                );

                if (!tableExists.HasValue)
                {
                    System.Diagnostics.Debug.WriteLine($"Table {tablename} not found in database. Returning empty table.");
                    return resultTable;
                }


                string sql = $"SELECT * FROM {tablename} WHERE [ProcessId]=@ProcessId AND [StepIndex]=@StepIndex";
                await ((SqlConnection)connection).OpenAsync();
                using var command = new SqlCommand(sql, (SqlConnection)connection);
                command.Parameters.AddWithValue("@ProcessId", processId);
                command.Parameters.AddWithValue("@StepIndex", stepIndex);
                using var adapter = new SqlDataAdapter(command);
                await Task.Run(() => adapter.Fill(resultTable));

                if (resultTable.Rows.Count > 0)
                    return resultTable;
                else
                    // Data not found (query returned 0 rows)
                    return resultTable;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CRITICAL SQL Exception in RetrieveStepResultAsync: {ex.Message}");
                return new DataTable("Error table");
            }
        }
        public async Task<IEnumerable<T>> RetrieveStepResultAsIEnumerableAsync<T>(Guid processId, int stepIndex) where T : new()
        {
            var tablename = GetStepsTableNameByStepIndex(stepIndex);
            var dbName = _datasettings.GetTableConfigDataByKey("TradePayableDbName");
            var fullTableName = $"{dbName}.{tablename}";
            IEnumerable<T> resultTable = [];

            using var connection = _dbcontext.CreateConnection("default");
            string checkSql = $"SELECT 1 FROM sys.tables WHERE name = @TableName";
            var tableExists = await connection.QuerySingleOrDefaultAsync<int?>( checkSql,new { TableName = tablename });

            if (!tableExists.HasValue || tableExists == 0)
            {
                System.Diagnostics.Debug.WriteLine($"Table {tablename} not found in database. Returning empty table.");
                return resultTable;
            }

            try
            {
                string sql = $"SELECT * FROM {tablename} WHERE [ProcessId]=@ProcessId AND [StepIndex]=@StepIndex";

                var result = await connection.QueryAsync<T>(
                    sql,
                    new { ProcessId = processId, StepIndex = stepIndex }
                );

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CRITICAL SQL Exception in RetrieveStepResultAsIEnumerableAsync: {ex.Message}");
                return [];
            }
        }
        public async Task<IEnumerable<T>> RetrievePaginatedStepResultAsIEnumerableAsync<T>(Guid processId, int stepIndex, int pageSize, int skip) where T : new()
        {
            var tablename = GetStepsTableNameByStepIndex(stepIndex);
            var dbName = _datasettings.GetTableConfigDataByKey("TradePayableDbName");
            var fullTableName = $"${dbName}.{tablename}";
            IEnumerable<T> resultTable = [];

            using var connection = _dbcontext.CreateConnection("default");
            string checkSql = $"SELECT 1 FROM sys.tables WHERE name = @TableName";
            var tableExists = await connection.QuerySingleOrDefaultAsync<int?>(
                checkSql,
                new { TableName = tablename }
            );

            if (!tableExists.HasValue)
            {
                System.Diagnostics.Debug.WriteLine($"Table {tablename} not found in database. Returning empty table.");
                return resultTable;
            }

            try
            {
                string sql = $"SELECT * FROM {tablename} WHERE [ProcessId]=@ProcessId AND [StepIndex]=@StepIndex " +
                    $"ORDER BY [Document_Number] ASC " +
                    $"OFFSET @Skip ROWS " +
                    $"FETCH NEXT @PageSize ROWS ONLY;";



                var result = await connection.QueryAsync<T>(
                    sql,
                    new { ProcessId = processId, StepIndex = stepIndex, Skip =skip, PageSize=pageSize}
                );

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CRITICAL SQL Exception in RetrieveStepResultAsIEnumerableAsync: {ex.Message}");
                return [];
            }
        }

        private string GetStepsTableNameByStepIndex(int stepIndex)
        {
            return stepIndex switch
            {
                0 => _datasettings.ProcessSteps.Step_00,
                1 => _datasettings.ProcessSteps.Step_01,
                2 => _datasettings.ProcessSteps.Step_02,
                3 => _datasettings.ProcessSteps.Step_03,
                4 => _datasettings.ProcessSteps.Step_04,
                5 => _datasettings.ProcessSteps.Step_05,
                6 => _datasettings.ProcessSteps.Step_06,
                7 => _datasettings.ProcessSteps.Step_07,
                8 => _datasettings.ProcessSteps.Step_08,
                9 => _datasettings.ProcessSteps.Step_09,
                10 => _datasettings.ProcessSteps.Step_10,
                11 => _datasettings.ProcessSteps.Step_11,
                12 => _datasettings.ProcessSteps.Step_12,
                _ => throw new NotImplementedException("Invalid Step Index"),
            };
        }
        
    }
}



