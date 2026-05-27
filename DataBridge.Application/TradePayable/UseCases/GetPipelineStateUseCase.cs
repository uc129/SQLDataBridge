using DataBridge.Domain.TradePayable.Contracts;
using DataBridge.Domain.TradePayable.Models;

namespace DataBridge.Application.TradePayable.UseCases;

public class PipelineState
{
    public required PipelineRun         Run            { get; set; }
    public required IReadOnlyList<int>  CompletedSteps { get; set; }
}

public class GetPipelineStateUseCase(IPipelineRunRepository pipelineRunRepo)
{
    // Canonical step class indices in execution order.
    private static readonly int[] StepOrder = [0, 1, 2, 3, 6, 7, 10, 11, 12, 13];

    public async Task<PipelineState?> ExecuteAsync(Guid runId)
    {
        var run = await pipelineRunRepo.GetByRunIdAsync(runId);
        if (run is null) return null;

        return new PipelineState
        {
            Run            = run,
            CompletedSteps = DeriveCompletedSteps(run.CurrentStepIndex),
        };
    }

    /// <summary>
    /// Derives which step slots are complete from the persisted CurrentStepIndex.
    /// Step03 produces sub-slots 4, 5, 31; Step07 produces 8, 9, 71.
    /// </summary>
    private static IReadOnlyList<int> DeriveCompletedSteps(int currentStepIndex)
    {
        var result = new List<int>();
        foreach (var idx in StepOrder)
        {
            if (idx > currentStepIndex) break;
            result.Add(idx);
            if (idx == 3)  result.AddRange([4, 5, 31]);
            if (idx == 7)  result.AddRange([8, 9, 71]);
        }
        return result;
    }
}
