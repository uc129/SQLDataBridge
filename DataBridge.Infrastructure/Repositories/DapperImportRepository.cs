using Dapper;
using DataBridge.Application.Interfaces;
using DataBridge.Domain.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace DataBridge.Infrastructure.Repositories;

internal sealed class DapperImportRepository(
    IConfiguration config,
    IProgressNotifier progressNotifier) : IImportRepository
{
    private string DefaultConn => config.GetConnectionString("Default") ?? string.Empty;

    private SqlConnection Open(string? cs = null) => new SqlConnection(cs ?? DefaultConn);

    public async Task DropAndCreateTableAsync(string schema, string table, IReadOnlyList<string> columns,
        string? connectionString = null, CancellationToken ct = default)
    {
        await using var conn = Open(connectionString);
        await conn.OpenAsync(ct);

        var drop   = $"IF OBJECT_ID('[{schema}].[{table}]', 'U') IS NOT NULL DROP TABLE [{schema}].[{table}]";
        var colDef = string.Join(",\n  ", columns.Select(c => $"[{c}] NVARCHAR(MAX)"));
        var create = $"CREATE TABLE [{schema}].[{table}] (\n  {colDef}\n)";

        await conn.ExecuteAsync(new CommandDefinition(drop,   cancellationToken: ct));
        await conn.ExecuteAsync(new CommandDefinition(create, cancellationToken: ct));
    }

    public async Task EnsureColumnsExistAsync(string schema, string table, IReadOnlyList<string> columns,
        string? connectionString = null, CancellationToken ct = default)
    {
        await using var conn = Open(connectionString);
        await conn.OpenAsync(ct);

        var existing = new HashSet<string>(
            await conn.QueryAsync<string>(new CommandDefinition(
                @"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS
                  WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table",
                new { schema, table }, cancellationToken: ct)),
            StringComparer.OrdinalIgnoreCase);

        foreach (var col in columns.Where(c => !existing.Contains(c)))
            await conn.ExecuteAsync(new CommandDefinition(
                $"ALTER TABLE [{schema}].[{table}] ADD [{col}] NVARCHAR(MAX)",
                cancellationToken: ct));
    }

    public async Task BulkInsertAsync(string schema, string table, DataTable data, string jobId,
        string? connectionString = null, CancellationToken ct = default)
    {
        var qualifiedTable = $"[{schema}].[{table}]";
        await using var conn = Open(connectionString);
        await conn.OpenAsync(ct);

        using var bulk = new SqlBulkCopy(conn)
        {
            DestinationTableName = qualifiedTable,
            BatchSize            = 500,
            BulkCopyTimeout      = 0,
            NotifyAfter          = 5_000,
        };

        foreach (DataColumn col in data.Columns)
            bulk.ColumnMappings.Add(col.ColumnName, col.ColumnName);

        bulk.SqlRowsCopied += async (_, e) =>
        {
            await progressNotifier.NotifyAsync(jobId, new ProgressMessage
            {
                JobId    = jobId,
                Stage    = "Importing",
                Message  = $"{e.RowsCopied:N0} rows inserted…",
                Percent  = 0,
                RowsDone = e.RowsCopied,
            });
        };

        await bulk.WriteToServerAsync(data, ct);
    }
}
