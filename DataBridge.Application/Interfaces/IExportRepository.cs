using DataBridge.Application.Models;

namespace DataBridge.Application.Interfaces;

public interface IExportRepository
{
    Task<IReadOnlyList<string>> GetAvailableTargetsAsync(IEnumerable<string> allowedNames, CancellationToken ct = default);
    Task<ExportQueryStream> StreamQueryAsync(string connectionString, string sql, CancellationToken ct = default);
}
