using DataBridge.Domain.TradePayable.Contracts;
using DataBridge.Domain.TradePayable.Models;

namespace DataBridge.Application.TradePayable.Steps;

/// <summary>
/// Fetches the four raw source tables (TP_Step_01, podata, m_Vendor, ICPVendorMap) in parallel
/// and performs all join/merge logic in memory, replacing the slow SQL view chain that used
/// OPENQUERY linked-server calls.
/// </summary>
public class Step02_GetMergedDataStep(
    IMergedDataService    mergedDataService,
    IStepResultRepository stepRepo) : IProcessStep
{
    public int StepIndex => 2;

    public async Task<ProcessState> ExecuteAsync(ProcessState state)
    {
        if (state.StepData.ContainsKey(StepIndex)) return state;
        state.StepData.Remove(1);

        var viewData = await mergedDataService.ComputeAsync();
        state.StepData[2] = viewData;

        // Async checkpoint: persist result with RunId for resume support.
        var snapshot = viewData.Copy();
        var runId    = state.RunId;
        _ = Task.Run(async () =>
        {
            try { await stepRepo.SaveAndReplaceStepResultAsync(snapshot, runId, StepIndex); }
            catch { /* best-effort */ }
        });

        return state;
    }
}
