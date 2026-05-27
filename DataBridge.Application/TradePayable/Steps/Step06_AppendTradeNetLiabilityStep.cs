using DataBridge.Application.TradePayable.Processing;
using DataBridge.Domain.TradePayable.Contracts;
using DataBridge.Domain.TradePayable.Models;

namespace DataBridge.Application.TradePayable.Steps;

/// <summary>
/// Appends local-currency GIT net liability to the trade payable data (slots 5 + 4).
/// Frees slots 3, 4, 5, 31 after merging — they are no longer referenced downstream.
/// Asynchronously persists slot 6 to TP_Step_06.
/// </summary>
public class Step06_AppendTradeNetLiabilityStep(
    GITProcessor          gitProcessor,
    HelperFunctions       helper,
    IStepResultRepository stepRepo) : IProcessStep
{
    public int StepIndex => 6;

    public async Task<ProcessState> ExecuteAsync(ProcessState state)
    {
        if (state.StepData.ContainsKey(StepIndex)) return state;

        await helper.InitializeAsync();

        state.StepData[6] = gitProcessor.AppendTradeAndGITLiabilityTable(
            state.StepData[5], state.StepData[4]);

        state.StepData.Remove(3);
        state.StepData.Remove(4);
        state.StepData.Remove(5);
        state.StepData.Remove(31);

        var snapshot = state.StepData[6].Copy();
        var runId    = state.RunId;
        _ = Task.Run(async () =>
        {
            try { await stepRepo.SaveAndReplaceStepResultAsync(snapshot, runId, StepIndex); }
            catch { /* best-effort */ }
        });

        return state;
    }
}
