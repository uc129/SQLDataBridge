using DataBridge.Application.Interfaces;
using DataBridge.Domain.Models;

namespace DataBridge.Application.UseCases;

public class GetTableInfoUseCase(IMetricsRepository metricsRepository)
{
    public Task<(TableMetrics Metrics, IReadOnlyList<string> Columns, Dictionary<string, string?> ColumnMap)>
        ExecuteAsync(string tableName, CancellationToken ct = default) =>
        metricsRepository.GetTableInfoAsync(tableName, ct);
}
