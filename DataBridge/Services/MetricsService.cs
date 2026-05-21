using Dapper;
using DataBridge.Models;
using Microsoft.Data.SqlClient;

namespace DataBridge.Services;

public class MetricsService(IConfiguration config)
{
    private static readonly string[] Tables =
        ["FNATool_VendorDetailsPipeline_1", "FNATool_VendorDetailsPipeline_2", "FNATool_VendorDetailsPipeline_3"];

    public async Task<DashboardMetrics> GetDashboardAsync()
    {
        var cs       = config.GetConnectionString("Default") ?? string.Empty;
        var viewName = config["DataBridge:MetricsViewName"] ?? "QTR34022";
        var metrics  = new DashboardMetrics { ViewName = viewName };

        if (string.IsNullOrWhiteSpace(cs))
        {
            foreach (var t in Tables)
                metrics.Tables.Add(new TableMetrics { TableName = t });
            metrics.ViewRowCount = -1;
            return metrics;
        }

        await using var conn = new SqlConnection(cs);
        await conn.OpenAsync();

        foreach (var table in Tables)
            metrics.Tables.Add(await FetchTableMetricsAsync(conn, table));

        try
        {
            metrics.ViewRowCount = await conn.ExecuteScalarAsync<long>($"SELECT COUNT(*) FROM [{viewName}]");
        }
        catch
        {
            metrics.ViewRowCount = -1;
        }

        return metrics;
    }

    public async Task<TableMetrics> GetTableMetricsAsync(string tableName)
    {
        var cs = config.GetConnectionString("Default") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(cs))
            return new TableMetrics { TableName = tableName };

        await using var conn = new SqlConnection(cs);
        await conn.OpenAsync();
        return await FetchTableMetricsAsync(conn, tableName);
    }

    // Returns metrics + column list + auto-detected column mapping in one DB round-trip.
    public async Task<(TableMetrics Metrics, List<string> Columns, Dictionary<string, string?> Mapping)>
        GetTableInfoAsync(string tableName)
    {
        var cs = config.GetConnectionString("Default") ?? string.Empty;
        var metrics = new TableMetrics { TableName = tableName };

        if (string.IsNullOrWhiteSpace(cs))
            return (metrics, new(), CleanService.AutoMapColumns(new List<string>()));

        await using var conn = new SqlConnection(cs);
        await conn.OpenAsync();

        var exists = await conn.ExecuteScalarAsync<int?>(
            "SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @t AND TABLE_TYPE = 'BASE TABLE'",
            new { t = tableName });
        metrics.Exists = exists.HasValue;

        if (!metrics.Exists)
            return (metrics, new(), CleanService.AutoMapColumns(new List<string>()));

        metrics.RowCount = await conn.ExecuteScalarAsync<long>($"SELECT COUNT(*) FROM [{tableName}]");

        var cols = (await conn.QueryAsync<string>(
            "SELECT LOWER(COLUMN_NAME) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @t ORDER BY ORDINAL_POSITION",
            new { t = tableName })).ToList();

        var mapping = CleanService.AutoMapColumns(cols);

        if (mapping.TryGetValue("vendor", out var vendorCol) && vendorCol != null)
            metrics.VendorNotFound = await conn.ExecuteScalarAsync<long>(
                $"SELECT COUNT(*) FROM [{tableName}] WHERE [{vendorCol}] IS NULL OR [{vendorCol}] = '' OR [{vendorCol}] = 'Not Found'");

        if (mapping.TryGetValue("purchasingDocument", out var poCol) && poCol != null)
            metrics.PoNotFound = await conn.ExecuteScalarAsync<long>(
                $"SELECT COUNT(*) FROM [{tableName}] WHERE [{poCol}] IS NULL OR [{poCol}] = '' OR [{poCol}] = 'Not Found'");

        return (metrics, cols, mapping);
    }

    private static async Task<TableMetrics> FetchTableMetricsAsync(SqlConnection conn, string table)
    {
        var m = new TableMetrics { TableName = table };

        var exists = await conn.ExecuteScalarAsync<int?>(
            "SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @t AND TABLE_TYPE = 'BASE TABLE'",
            new { t = table });
        m.Exists = exists.HasValue;

        if (!m.Exists) return m;

        m.RowCount = await conn.ExecuteScalarAsync<long>($"SELECT COUNT(*) FROM [{table}]");

        var cols = (await conn.QueryAsync<string>(
            "SELECT LOWER(COLUMN_NAME) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @t",
            new { t = table })).ToList();

        var mapping = CleanService.AutoMapColumns(cols);

        var vendorCol = mapping.GetValueOrDefault("vendor");
        if (vendorCol != null)
            m.VendorNotFound = await conn.ExecuteScalarAsync<long>(
                $"SELECT COUNT(*) FROM [{table}] WHERE [{vendorCol}] IS NULL OR [{vendorCol}] = '' OR [{vendorCol}] = 'Not Found'");

        var poCol = mapping.GetValueOrDefault("purchasingDocument");
        if (poCol != null)
            m.PoNotFound = await conn.ExecuteScalarAsync<long>(
                $"SELECT COUNT(*) FROM [{table}] WHERE [{poCol}] IS NULL OR [{poCol}] = '' OR [{poCol}] = 'Not Found'");

        return m;
    }
}
