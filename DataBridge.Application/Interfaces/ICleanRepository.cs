namespace DataBridge.Application.Interfaces;

public interface ICleanRepository
{
    Task<IReadOnlySet<string>> GetTableColumnsAsync(string tableName, CancellationToken ct = default);
    Task RefreshVendorViewAsync(string tableName, int viewSuffix,
        string? connectionString = null, CancellationToken ct = default);
    Task AddTemporaryRowNumberAsync(string tableName, CancellationToken ct = default);
    Task RemoveTemporaryRowNumberAsync(string tableName, CancellationToken ct = default);
    Task<IReadOnlyList<IDictionary<string, object?>>> GetAllRowsAsync(string tableName, CancellationToken ct = default);
    Task UpdateRowAsync(string tableName, int rowNumber, IDictionary<string, string?> updates,
        IReadOnlySet<string> validColumns, CancellationToken ct = default);
}
