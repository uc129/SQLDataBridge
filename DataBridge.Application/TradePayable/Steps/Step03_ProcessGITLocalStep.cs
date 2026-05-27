using DataBridge.Application.TradePayable.Processing;
using DataBridge.Domain.TradePayable.Contracts;
using DataBridge.Domain.TradePayable.Models;

namespace DataBridge.Application.TradePayable.Steps;

/// <summary>
/// Runs local-currency GIT advance processing.
/// Produces four in-memory slots: processedGIT→3, netLiability→4, modifiedOriginal→5, allGrouped→31.
/// Asynchronously persists slot 3 (advance calculation result) to TP_Step_03.
/// </summary>
public class Step03_ProcessGITLocalStep(
    GITProcessor          gitProcessor,
    HelperFunctions       helper,
    IStepResultRepository stepRepo) : IProcessStep
{
    public int StepIndex => 3;

    public async Task<ProcessState> ExecuteAsync(ProcessState state)
    {
        if (state.StepData.ContainsKey(StepIndex)) return state;

        await helper.InitializeAsync();

        var results = gitProcessor.ProcessFAGLL03GITData(state.StepData[2]);

        state.StepData[3]  = results[0]; // processedGIT
        state.StepData[4]  = results[1]; // netLiability
        state.StepData[5]  = results[2]; // modifiedOriginal
        state.StepData[31] = results[3]; // allGrouped

        state.StepData.Remove(2);

        var snapshot = state.StepData[3].Copy();
        var runId    = state.RunId;
        _ = Task.Run(async () =>
        {
            try { await stepRepo.SaveAndReplaceStepResultAsync(snapshot, runId, StepIndex); }
            catch { /* best-effort */ }
        });

        return state;
    }
}
