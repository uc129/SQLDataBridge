using DataBridge.Application.TradePayable.Processing;
using DataBridge.Domain.TradePayable.Contracts;
using DataBridge.Domain.TradePayable.Models;

namespace DataBridge.Application.TradePayable.Steps;

/// <summary>
/// Appends document-currency GIT net liability to the trade payable data (slots 9 + 8).
/// Frees slots 7, 8, 9, 71 after merging — they are no longer referenced downstream.
/// Asynchronously persists slot 10 to TP_Step_10.
/// </summary>
public class Step10_AppendTradeDocCurrNetLiabilityStep(
    GITDocCurrProcessor   gitDocCurrProcessor,
    HelperFunctions       helper,
    IStepResultRepository stepRepo) : IProcessStep
{
    public int StepIndex => 10;

    public async Task<ProcessState> ExecuteAsync(ProcessState state)
    {
        if (state.StepData.ContainsKey(StepIndex)) return state;

        await helper.InitializeAsync();

        state.StepData[10] = gitDocCurrProcessor.AppendTradeAndGITLiabilityDocCurrTable(
            state.StepData[9], state.StepData[8]);

        state.StepData.Remove(7);
        state.StepData.Remove(8);
        state.StepData.Remove(9);
        state.StepData.Remove(71);

        var snapshot = state.StepData[10].Copy();
        var runId    = state.RunId;
        _ = Task.Run(async () =>
        {
            try { await stepRepo.SaveAndReplaceStepResultAsync(snapshot, runId, StepIndex); }
            catch { /* best-effort */ }
        });

        return state;
    }
}
