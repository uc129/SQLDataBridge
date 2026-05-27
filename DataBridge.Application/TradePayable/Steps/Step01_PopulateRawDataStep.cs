using DataBridge.Application.TradePayable.Extensions;
using DataBridge.Application.TradePayable.Processing;
using DataBridge.Domain.TradePayable.Configuration;
using DataBridge.Domain.TradePayable.Contracts;
using DataBridge.Domain.TradePayable.Models;

namespace DataBridge.Application.TradePayable.Steps;

/// <summary>
/// Extracts vendor and PO data from raw entities, then persists the populated result
/// to TP_Step_01 (TRUNCATE + INSERT, no RunId) so the SQL view chain can read from it.
/// </summary>
public class Step01_PopulateRawDataStep(
    IPipelineMemoryStore memoryStore,
    DataProcessor dataProcessor,
    IStepResultRepository stepRepo,
    TradePayableSettings settings) : IProcessStep
{
    public int StepIndex => 1;

    public async Task<ProcessState> ExecuteAsync(ProcessState state)
    {
        if (state.StepData.ContainsKey(StepIndex)) return state;

        var rawEntities = memoryStore.Get(state.RunId)
            ?? throw new InvalidOperationException(
                $"Raw entities not found in memory store for run {state.RunId}. " +
                "Ensure Step00 ran successfully.");

        var populated = dataProcessor.ProcessRawData(rawEntities, state.RunId);
        var dt = populated.ToDataTable("Step01_Populated");

        // Save to TP_Step_01 (feeds the SQL view chain for cross-server joins).
        await stepRepo.TruncateAndInsertAsync(dt, settings.GetStepTable("Step_01"));

        state.StepData[1] = dt;

        // Raw entities no longer needed — free them now to reclaim memory.
        memoryStore.Remove(state.RunId);

        return state;
    }
}
