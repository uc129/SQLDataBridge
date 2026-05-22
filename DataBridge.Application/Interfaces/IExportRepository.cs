namespace DataBridge.Application.Interfaces;

public interface IExportRepository
{
    Task<IReadOnlyList<string>> GetAvailableTargetsAsync(IEnumerable<string> allowedNames, CancellationToken ct = default);
    Task<(IReadOnlyList<string> Columns, IReadOnlyList<object?[]> Rows)> ExecuteQueryAsync(
        string connectionString, string sql, CancellationToken ct = default);
}
