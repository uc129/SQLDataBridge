using Dapper;
using DataBridge.Application.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace DataBridge.Infrastructure.Repositories;

internal sealed class DapperCleanRepository(IConfiguration config) : ICleanRepository
{
    private string DefaultConn => config.GetConnectionString("Default") ?? string.Empty;

    private SqlConnection Open(string? cs = null) => new SqlConnection(cs ?? DefaultConn);

    public async Task<IReadOnlySet<string>> GetTableColumnsAsync(string tableName, CancellationToken ct = default)
    {
        await using var conn = Open();
        await conn.OpenAsync(ct);
        var cols = await conn.QueryAsync<string>(
            "SELECT LOWER(COLUMN_NAME) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @t",
            new { t = tableName });
        return new HashSet<string>(cols, StringComparer.OrdinalIgnoreCase);
    }

    public async Task RefreshVendorViewAsync(string tableName, int viewSuffix,
        string? connectionString = null, CancellationToken ct = default)
    {
        await using var conn = Open(connectionString);
        await conn.OpenAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(
            "EXEC [dbo].[sp_CreateVendorDetailsPipelineView] @TableName = @tn, @ViewSuffix = @vs",
            new { tn = tableName, vs = viewSuffix },
            cancellationToken: ct));
    }

    public async Task AddTemporaryRowNumberAsync(string tableName, CancellationToken ct = default)
    {
        await using var conn = Open();
        await conn.OpenAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(
            $"ALTER TABLE [{tableName}] ADD [_rn] INT IDENTITY(1,1)",
            cancellationToken: ct));
    }

    public async Task RemoveTemporaryRowNumberAsync(string tableName, CancellationToken ct = default)
    {
        await using var conn = Open();
        await conn.OpenAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(
            $"ALTER TABLE [{tableName}] DROP COLUMN [_rn]",
            cancellationToken: ct));
    }

    public async Task<IReadOnlyList<IDictionary<string, object?>>> GetAllRowsAsync(
        string tableName, CancellationToken ct = default)
    {
        await using var conn = Open();
        await conn.OpenAsync(ct);
        var rows = await conn.QueryAsync(
            new CommandDefinition($"SELECT * FROM [{tableName}]", cancellationToken: ct));
        return rows.Cast<IDictionary<string, object?>>().ToList();
    }

    public async Task UpdateRowAsync(string tableName, int rowNumber, IDictionary<string, string?> updates,
        IReadOnlySet<string> validColumns, CancellationToken ct = default)
    {
        var filtered = updates
            .Where(kv => validColumns.Contains(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        if (filtered.Count == 0) return;

        var set  = string.Join(", ", filtered.Keys.Select(k => $"[{k}] = @{k}"));
        var pars = new DynamicParameters();
        foreach (var kv in filtered) pars.Add(kv.Key, kv.Value);
        pars.Add("rn", rowNumber);

        await using var conn = Open();
        await conn.OpenAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(
            $"UPDATE [{tableName}] SET {set} WHERE [_rn] = @rn",
            pars, cancellationToken: ct));
    }
}
