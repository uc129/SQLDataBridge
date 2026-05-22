using System.Data;

namespace DataBridge.Application.Interfaces;

public interface IImportRepository
{
    Task DropAndCreateTableAsync(string schema, string table, IReadOnlyList<string> columns,
        string? connectionString = null, CancellationToken ct = default);
    Task EnsureColumnsExistAsync(string schema, string table, IReadOnlyList<string> columns,
        string? connectionString = null, CancellationToken ct = default);
    Task BulkInsertAsync(string schema, string table, DataTable data, string jobId,
        string? connectionString = null, CancellationToken ct = default);
}
