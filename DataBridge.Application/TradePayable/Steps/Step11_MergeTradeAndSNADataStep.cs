using DataBridge.Application.TradePayable.Processing;
using DataBridge.Domain.TradePayable.Contracts;
using DataBridge.Domain.TradePayable.Models;

namespace DataBridge.Application.TradePayable.Steps;

/// <summary>
/// LEFT JOINs local-currency adjusted data (slot 6) with doc-currency adjusted SNA data (slot 10)
/// on Invoice_Key to produce the unified trade payable dataset.
/// Frees slots 6 and 10 after merging.
/// Asynchronously persists slot 11 to TP_Step_11.
/// </summary>
public class Step11_MergeTradeAndSNADataStep(IStepResultRepository stepRepo) : IProcessStep
{
    public int StepIndex => 11;

    public Task<ProcessState> ExecuteAsync(ProcessState state)
    {
        if (state.StepData.ContainsKey(StepIndex)) return Task.FromResult(state);

        state.StepData[11] = DataProcessor.MergeTradeAndSNAData(
            state.StepData[6], state.StepData[10]);

        state.StepData.Remove(6);
        state.StepData.Remove(10);

        var snapshot = state.StepData[11].Copy();
        var runId    = state.RunId;
        _ = Task.Run(async () =>
        {
            try { await stepRepo.SaveAndReplaceStepResultAsync(snapshot, runId, StepIndex); }
            catch { /* best-effort */ }
        });

        return Task.FromResult(state);
    }
}
