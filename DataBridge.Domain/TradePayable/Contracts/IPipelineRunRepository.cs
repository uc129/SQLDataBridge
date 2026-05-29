using DataBridge.Domain.TradePayable.Models;

namespace DataBridge.Domain.TradePayable.Contracts;

public interface IPipelineRunRepository
{
    Task CreateAsync(PipelineRun run);
    Task<PipelineRun?> GetByRunIdAsync(Guid runId);
    Task<IEnumerable<PipelineRun>> GetAllAsync();
    Task UpdateStepIndexAsync(Guid runId, int stepIndex);
    Task UpdateStatusAsync(Guid runId, PipelineRunStatus status);
    Task<PipelineRun?> GetByRevisionAsync(DateTime quarterDate, string revisionNumber);
    Task DeleteAsync(Guid runId);
    Task UpdateStepStatsAsync(Guid runId, string statsJson);
}
