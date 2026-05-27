using DataBridge.Domain.TradePayable.Contracts;
using DataBridge.Domain.TradePayable.Models;

namespace DataBridge.Application.TradePayable.Steps;

/// <summary>
/// Ensures raw entities are present in the memory store.
/// Under normal operation the upload use case already stored them there.
/// This step acts as the recovery path: if the store is cold (e.g. server restart
/// before the pipeline ran), it reloads from TP_FAGLL03_Raw.
/// </summary>
public class Step00_GetRawDataStep(
    IPipelineMemoryStore      memoryStore,
    IFAGLL03StagingRepository stagingRepo) : IProcessStep
{
    public int StepIndex => 0;

    public async Task<ProcessState> ExecuteAsync(ProcessState state)
    {
        if (memoryStore.Get(state.RunId) is not null) return state;

        var rows = await stagingRepo.GetByRunIdAsync(state.RunId);
        memoryStore.Store(state.RunId, rows.ToList());
        return state;
    }
}
