using DataBridge.Application.TradePayable.Processing;
using DataBridge.Domain.TradePayable.Contracts;
using DataBridge.Domain.TradePayable.Models;

namespace DataBridge.Application.TradePayable.Steps;

/// <summary>
/// Runs document-currency GIT advance processing.
/// Produces four in-memory slots: processedGIT→7, netLiability→8, modifiedOriginal→9, allGrouped→71.
/// Asynchronously persists slot 7 (advance calculation result) to TP_Step_07.
/// </summary>
public class Step07_ProcessGITDocCurrStep(
    GITDocCurrProcessor   gitDocCurrProcessor,
    HelperFunctions       helper,
    IStepResultRepository stepRepo) : IProcessStep
{
    public int StepIndex => 7;

    public async Task<ProcessState> ExecuteAsync(ProcessState state)
    {
        if (state.StepData.ContainsKey(StepIndex)) return state;

        await helper.InitializeAsync();

        var results = gitDocCurrProcessor.ProcessFAGLL03DocCurrGITData(state.StepData[6]);

        state.StepData[7]  = results[0]; // processedGIT
        state.StepData[8]  = results[1]; // netLiability
        state.StepData[9]  = results[2]; // modifiedOriginal
        state.StepData[71] = results[3]; // allGrouped

        var runId = state.RunId;
        foreach (var (slotIdx, table) in new[] { (7, results[0]), (8, results[1]), (9, results[2]), (71, results[3]) })
        {
            var snap = table.Copy();
            var si   = slotIdx;
            _ = Task.Run(async () =>
            {
                try { await stepRepo.SaveAndReplaceStepResultAsync(snap, runId, si); }
                catch { }
            });
        }

        return state;
    }
}
