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
            data.Columns.Add("RunId", typeof(string));
        if (!data.Columns.Contains("StepIndex"))
            data.Columns.Add("StepIndex", typeof(string));

        foreach (DataRow row in data.Rows)
        {
            row["RunId"]     = runId.ToString();
            row["StepIndex"] = stepIndex.ToString();
        }

        var normalized = NormalizeForNVarchar(data);

        await using var conn = db.OpenDefault();
        await conn.OpenAsync();

        await EnsureTableExistsAsync(conn, tableName, normalized);

        using var bulk = new SqlBulkCopy(conn)
        {
            DestinationTableName = $"[{tableName}]",
            BatchSize            = 500,
            BulkCopyTimeout      = 0,
        };

        foreach (DataColumn col in normalized.Columns)
            bulk.ColumnMappings.Add(col.ColumnName, col.ColumnName);

        await bulk.WriteToServerAsync(normalized);
    }

    private static DataTable NormalizeForNVarchar(DataTable source)
    {
        var columnsToConvert = source.Columns.Cast<DataColumn>()
            .Where(c => c.DataType != typeof(string))
            .Select(c => c.ColumnName)
            .ToList();

        if (columnsToConvert.Count == 0) return source;

        var copy = source.Copy();
        foreach (var colName in columnsToConvert)
        {
            int ordinal = copy.Columns[colName]!.Ordinal;
            var tempName = colName + "__tmp";

            copy.Columns.Add(tempName, typeof(string));
            foreach (DataRow row in copy.Rows)
                row[tempName] = row[colName] == DBNull.Value ? DBNull.Value : (object)row[colName].ToString()!;

            copy.Columns.Remove(colName);
            copy.Columns[tempName]!.ColumnName = colName;
            copy.Columns[colName]!.SetOrdinal(ordinal);
        }

        return copy;
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
