using Dapper;
using DataBridge.Domain.TradePayable.Configuration;
using DataBridge.Domain.TradePayable.Contracts;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Data;

namespace DataBridge.Infrastructure.TradePayable.Repositories;

internal sealed class StepResultRepository(
    TradePayableDbContext db,
    IOptions<TradePayableSettings> settings) : IStepResultRepository
{
    private TradePayableSettings Settings => settings.Value;

    public async Task SaveAndReplaceStepResultAsync(DataTable data, Guid runId, int stepIndex)
    {
        var tableName = Settings.GetStepTable($"Step_{stepIndex:D2}");
        AddMetaColumns(data, runId, stepIndex);

        await using var conn = db.OpenDefault();
        await conn.OpenAsync();

        await EnsureTableExistsAsync(conn, tableName, data);
        await DeleteExistingRowsAsync(conn, tableName, runId, stepIndex);
        await BulkInsertAsync(conn, tableName, data);
    }

    public async Task SaveAndAppendStepResultAsync(DataTable data, Guid runId, int stepIndex)
    {
        var tableName = Settings.GetStepTable($"Step_{stepIndex:D2}");
        AddMetaColumns(data, runId, stepIndex);

        await using var conn = db.OpenDefault();
        await conn.OpenAsync();

        await EnsureTableExistsAsync(conn, tableName, data);
        await BulkInsertAsync(conn, tableName, data);
    }

    public async Task<DataTable> RetrieveStepResultAsync(Guid runId, int stepIndex)
    {
        var tableName = Settings.GetStepTable($"Step_{stepIndex:D2}");
        var result = new DataTable($"Step_{stepIndex}");

        await using var conn = db.OpenDefault();
        await conn.OpenAsync();

        if (!await TableExistsAsync(conn, tableName))
            return result;

        var sql = $"SELECT * FROM [{tableName}] WHERE [RunId] = @runId AND [StepIndex] = @stepIndex";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@runId", runId);
        cmd.Parameters.AddWithValue("@stepIndex", stepIndex);
        using var adapter = new SqlDataAdapter(cmd);
        await Task.Run(() => adapter.Fill(result));
        return result;
    }

    public async Task<IEnumerable<T>> RetrieveStepResultAsIEnumerableAsync<T>(Guid runId, int stepIndex) where T : new()
    {
        var tableName = Settings.GetStepTable($"Step_{stepIndex:D2}");

        await using var conn = db.OpenDefault();
        await conn.OpenAsync();

        if (!await TableExistsAsync(conn, tableName))
            return [];

        var sql = $"SELECT * FROM [{tableName}] WHERE [RunId] = @runId AND [StepIndex] = @stepIndex";
        return await conn.QueryAsync<T>(sql, new { runId, stepIndex });
    }

    public async Task<bool> StepHasResultsAsync(Guid runId, int stepIndex)
    {
        var tableName = Settings.GetStepTable($"Step_{stepIndex:D2}");

        await using var conn = db.OpenDefault();
        await conn.OpenAsync();

        if (!await TableExistsAsync(conn, tableName))
            return false;

        var count = await conn.ExecuteScalarAsync<int>(
            $"SELECT COUNT(1) FROM [{tableName}] WHERE [RunId] = @runId AND [StepIndex] = @stepIndex",
            new { runId, stepIndex });

        return count > 0;
    }

    public async Task TruncateAndInsertAsync(DataTable data, string tableName)
    {
        try
        {
            await using var conn = db.OpenDefault();
            await conn.OpenAsync();

            await EnsureTableExistsAsync(conn, tableName, data);
            await conn.ExecuteAsync($"TRUNCATE TABLE [{tableName}]");
            await BulkInsertAsync(conn, tableName, data);
        }
        catch (Exception ex)
        {

            throw;
        }
    }

    public async Task<DataTable> ReadFromViewAsync(string viewName)
    {
        try
        {
            var result = new DataTable(viewName);

            await using var conn = db.OpenDefault();
            await conn.OpenAsync();

            using var cmd = new SqlCommand($"SELECT * FROM [{viewName}]", conn);
            using var adapter = new SqlDataAdapter(cmd);
            await Task.Run(() => adapter.Fill(result));
            return result;
        }
        catch (Exception ex)
        {

            throw;
        }
    }

    // ── helpers ────────────────────────────────────────────────────────────────

    private static void AddMetaColumns(DataTable data, Guid runId, int stepIndex)
    {
        if (!data.Columns.Contains("RunId"))
            data.Columns.Add("RunId", typeof(string));

        if (!data.Columns.Contains("StepIndex"))
            data.Columns.Add("StepIndex", typeof(int));

        foreach (DataRow row in data.Rows)
        {
            row["RunId"] = runId.ToString();
            row["StepIndex"] = stepIndex;
        }
    }

    private static async Task<bool> TableExistsAsync(SqlConnection conn, string tableName)
    {
        var count = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM sys.tables WHERE name = @tableName",
            new { tableName });
        return count > 0;
    }

    private static async Task EnsureTableExistsAsync(SqlConnection conn, string tableName, DataTable schema)
    {
        var exists = await TableExistsAsync(conn, tableName);
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

    private static async Task DeleteExistingRowsAsync(SqlConnection conn, string tableName, Guid runId, int stepIndex) =>
        await conn.ExecuteAsync(
            $"DELETE FROM [{tableName}] WHERE [RunId] = @runId AND [StepIndex] = @stepIndex",
            new { runId, stepIndex });

    private static async Task BulkInsertAsync(SqlConnection conn, string tableName, DataTable data)
    {
        using var bulk = new SqlBulkCopy(conn)
        {
            DestinationTableName = $"[{tableName}]",
            BatchSize = 500,
            BulkCopyTimeout = 0,
        };

        foreach (DataColumn col in data.Columns)
            bulk.ColumnMappings.Add(col.ColumnName, col.ColumnName);

        await bulk.WriteToServerAsync(data);
    }
}
