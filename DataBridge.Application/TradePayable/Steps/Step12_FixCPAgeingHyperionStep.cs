using DataBridge.Application.TradePayable.Extensions;
using DataBridge.Application.TradePayable.Processing;
using DataBridge.Domain.TradePayable.Aggregates;
using DataBridge.Domain.TradePayable.Contracts;
using DataBridge.Domain.TradePayable.Models;

namespace DataBridge.Application.TradePayable.Steps;

/// <summary>
/// Applies MSME CP fix, ageing calculation, Hyperion classification, ICP Hyperion,
/// ERV calculation, and journal entry amounts to produce the final processed result.
/// Reads from slot 11 via ToEnumerable (DataTable → typed entities), frees slot 11 after.
/// Asynchronously persists slot 12 to TP_Step_12.
/// </summary>
public class Step12_FixCPAgeingHyperionStep(
    DataProcessor dataProcessor,
    HelperFunctions helper,
    IStepResultRepository stepRepo) : IProcessStep
{
    public int StepIndex => 12;

    public async Task<ProcessState> ExecuteAsync(ProcessState state)
    {
        if (state.StepData.ContainsKey(StepIndex)) return state;

        await helper.InitializeAsync();

        try
        {
            var step11Data = state.StepData[11].ToEnumerable<FAGLL03NetLiability>();
            var cpFixed = dataProcessor.MSMECreditPeriodFixEnumerable(step11Data);
            var baseHyp = dataProcessor.AssignBaseHyperionsEnumerable(cpFixed);
            var classified = dataProcessor.HyperionClassificationEnumerable(baseHyp, state.CurrentQuarter);
            var icpHyp = dataProcessor.AssignICPHyperionCodesEnumerable(classified);
            var withErv = dataProcessor.SNAERVCalculationEnumerable(icpHyp, state.CurrentQuarter);
            var merged = DataProcessor.MergeICPHyperionAndAmountDocINR(withErv);
            var withJournal = HelperFunctions.CalculateJournalEntryEnumerable(merged);


            var dt = withJournal.ToDataTable("Step12_Processed");
            DataProcessor.AddJoinKeysColumn(dt);
            state.StepData[12] = dt;

            state.StepData.Remove(11);

            var snapshot = dt.Copy();
            var runId = state.RunId;
            _ = Task.Run(async () =>
            {
                try { await stepRepo.SaveAndReplaceStepResultAsync(snapshot, runId, StepIndex); }
                catch { /* best-effort */ }
            });

            return state;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.Message);
            throw;
        }
    }
}
