using DataBridge.Application.Interfaces;
using DataBridge.Domain.Policies;

namespace DataBridge.Application.UseCases;

public class GetAvailableExportTargetsUseCase(IExportRepository exportRepository)
{
    public Task<IReadOnlyList<string>> ExecuteAsync(CancellationToken ct = default) =>
        exportRepository.GetAvailableTargetsAsync(TableWhitelistPolicy.ExportTargets, ct);
}
