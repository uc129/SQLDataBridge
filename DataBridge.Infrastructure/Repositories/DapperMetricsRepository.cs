using Dapper;
using DataBridge.Application.Interfaces;
using DataBridge.Domain.Models;
using DataBridge.Domain.Policies;
using DataBridge.Domain.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace DataBridge.Infrastructure.Repositories;

internal sealed class DapperMetricsRepository(IConfiguration config) : IMetricsRepository
{
    private string Conn => config.GetConnectionString("Default") ?? string.Empty;

    public async Task<DashboardMetrics> GetDashboardAsync(CancellationToken ct = default)
    {
        var viewName = config["DataBridge:MetricsViewName"] ?? "QTR34022";
        var metrics  = new DashboardMetrics { ViewName = viewName };

        if (string.IsNullOrWhiteSpace(Conn))
        {
            foreach (var t in TableWhitelistPolicy.CleanTables)
                metrics.Tables.Add(new TableMetrics { TableName = t });
            metrics.ViewRowCount = -1;
            return metrics;
        }

        await using var conn = new SqlConnection(Conn);
        await conn.OpenAsync(ct);

        foreach (var table in TableWhitelistPolicy.CleanTables)
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

    public async Task<TableMetrics> GetTableMetricsAsync(string tableName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(Conn))
            return new TableMetrics { TableName = tableName };

        await using var conn = new SqlConnection(Conn);
        await conn.OpenAsync(ct);
        return await FetchTableMetricsAsync(conn, tableName);
    }

    public async Task<(TableMetrics Metrics, IReadOnlyList<string> Columns, Dictionary<string, string?> ColumnMap)>
        GetTableInfoAsync(string tableName, CancellationToken ct = default)
    {
        var metrics = new TableMetrics { TableName = tableName };

        if (string.IsNullOrWhiteSpace(Conn))
            return (metrics, Array.Empty<string>(), ColumnMappingPolicy.AutoMapColumns(new List<string>()));

        await using var conn = new SqlConnection(Conn);
        await conn.OpenAsync(ct);

        var exists = await conn.ExecuteScalarAsync<int?>(
            "SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @t AND TABLE_TYPE = 'BASE TABLE'",
            new { t = tableName });
        metrics.Exists = exists.HasValue;

        if (!metrics.Exists)
            return (metrics, Array.Empty<string>(), ColumnMappingPolicy.AutoMapColumns(new List<string>()));

        metrics.RowCount = await conn.ExecuteScalarAsync<long>($"SELECT COUNT(*) FROM [{tableName}]");

        var cols = (await conn.QueryAsync<string>(
            "SELECT LOWER(COLUMN_NAME) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @t ORDER BY ORDINAL_POSITION",
            new { t = tableName })).ToList();

        var mapping = ColumnMappingPolicy.AutoMapColumns(cols);

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

        var mapping = ColumnMappingPolicy.AutoMapColumns(cols);

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
