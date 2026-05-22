using DataBridge.Application.Interfaces;
using DataBridge.Domain.Models;

namespace DataBridge.Application.UseCases;

public class GetDashboardUseCase(IMetricsRepository metricsRepository)
{
    public Task<DashboardMetrics> ExecuteAsync(CancellationToken ct = default) =>
        metricsRepository.GetDashboardAsync(ct);
}
