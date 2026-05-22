using Dapper;
using DataBridge.Application.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace DataBridge.Infrastructure.Repositories;

internal sealed class DapperExportRepository(IConfiguration config) : IExportRepository
{
    private string DefaultConn => config.GetConnectionString("Default") ?? string.Empty;

    public async Task<IReadOnlyList<string>> GetAvailableTargetsAsync(
        IEnumerable<string> allowedNames, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(DefaultConn))
            return Array.Empty<string>();

        var allowed = allowedNames.ToList();
        if (allowed.Count == 0) return Array.Empty<string>();

        var inList = string.Join(",", allowed.Select(n => $"N'{n}'"));
        var sql = $"""
            SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_NAME IN ({inList})
            UNION
            SELECT TABLE_NAME FROM INFORMATION_SCHEMA.VIEWS WHERE TABLE_NAME IN ({inList})
            """;

        await using var conn = new SqlConnection(DefaultConn);
        await conn.OpenAsync(ct);
        var result = await conn.QueryAsync<string>(new CommandDefinition(sql, cancellationToken: ct));
        return result.AsList();
    }

    public async Task<(IReadOnlyList<string> Columns, IReadOnlyList<object?[]> Rows)>
        ExecuteQueryAsync(string connectionString, string sql, CancellationToken ct = default)
    {
        var allRows = new List<object?[]>();
        List<string> columns;

        await using var conn   = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);
        await using var cmd    = new SqlCommand(sql, conn) { CommandTimeout = 0 };
        await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess, ct);

        columns = Enumerable.Range(0, reader.FieldCount)
                            .Select(i => reader.GetName(i))
                            .ToList();

        while (await reader.ReadAsync(ct))
        {
            ct.ThrowIfCancellationRequested();
            var row = new object?[reader.FieldCount];
            reader.GetValues(row!);
            for (int i = 0; i < row.Length; i++)
                if (row[i] is DBNull) row[i] = null;
            allRows.Add(row);
        }

        return (columns, allRows);
    }
}
