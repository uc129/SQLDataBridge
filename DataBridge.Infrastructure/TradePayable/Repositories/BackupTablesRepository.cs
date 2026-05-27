using Dapper;
using DataBridge.Domain.TradePayable.Configuration;
using DataBridge.Domain.TradePayable.Contracts;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Data;

namespace DataBridge.Infrastructure.TradePayable.Repositories;

internal sealed class BackupTablesRepository(
    TradePayableDbContext db,
    IOptions<TradePayableSettings> settings) : IBackupTablesRepository
{
    private TradePayableSettings Settings => settings.Value;

    public async Task SaveAndAppendStepResultAsync(DataTable data, Guid runId, int stepIndex)
    {
        var tableName = Settings.GetBackupTable($"Step_{stepIndex:D2}");

        if (!data.Columns.Contains("RunId"))
            data.Columns.Add("RunId", typeof(Guid));
        if (!data.Columns.Contains("StepIndex"))
            data.Columns.Add("StepIndex", typeof(int));

        foreach (DataRow row in data.Rows)
        {
            row["RunId"]     = runId;
            row["StepIndex"] = stepIndex;
        }

        await using var conn = db.OpenDefault();
        await conn.OpenAsync();

        await EnsureTableExistsAsync(conn, tableName, data);

        using var bulk = new SqlBulkCopy(conn)
        {
            DestinationTableName = $"[{tableName}]",
            BatchSize            = 500,
            BulkCopyTimeout      = 0,
        };

        foreach (DataColumn col in data.Columns)
            bulk.ColumnMappings.Add(col.ColumnName, col.ColumnName);

        await bulk.WriteToServerAsync(data);
    }

    private static async Task EnsureTableExistsAsync(SqlConnection conn, string tableName, DataTable schema)
    {
        var exists = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM sys.tables WHERE name = @tableName",
            new { tableName }) > 0;

        if (!exists)
        {
            var colDefs = string.Join(", ", schema.Columns.Cast<DataColumn>()
                .Select(c => $"[{c.ColumnName}] NVARCHAR(MAX)"));
            await conn.ExecuteAsync($"CREATE TABLE [{tableName}] ({colDefs})");
        }
        else
        {
            var existing = new HashSet<string>(
                await conn.QueryAsync<string>(
                    "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tableName",
                    new { tableName }),
                StringComparer.OrdinalIgnoreCase);

            foreach (DataColumn col in schema.Columns)
            {
                if (!existing.Contains(col.ColumnName))
                    await conn.ExecuteAsync($"ALTER TABLE [{tableName}] ADD [{col.ColumnName}] NVARCHAR(MAX)");
            }
        }
    }
}
