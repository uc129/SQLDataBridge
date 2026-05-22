using DataBridge.Domain.Models;

namespace DataBridge.Application.Interfaces;

public interface IMetricsRepository
{
    Task<DashboardMetrics> GetDashboardAsync(CancellationToken ct = default);
    Task<TableMetrics> GetTableMetricsAsync(string tableName, CancellationToken ct = default);
    Task<(TableMetrics Metrics, IReadOnlyList<string> Columns, Dictionary<string, string?> ColumnMap)>
        GetTableInfoAsync(string tableName, CancellationToken ct = default);
}
