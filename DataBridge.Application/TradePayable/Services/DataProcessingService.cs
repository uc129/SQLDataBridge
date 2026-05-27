using DataBridge.Domain.TradePayable.Contracts;
using DataBridge.Domain.TradePayable.Models;

namespace DataBridge.Application.TradePayable.Services;

public class DataProcessingService(IEnumerable<IProcessStep> steps) : IDataProcessingService
{
    private readonly IReadOnlyList<IProcessStep> _orderedSteps =
        steps.OrderBy(s => s.StepIndex).ToList();

    public async Task<ProcessState> RunStepsUpTo(int targetStepIndex, ProcessState state)
    {
        var toRun = _orderedSteps
            .Where(s => s.StepIndex <= targetStepIndex && s.StepIndex > state.CurrentStepIndex)
            .ToList();

        foreach (var step in toRun)
        {
            state = await step.ExecuteAsync(state);
            state.CurrentStepIndex = step.StepIndex;
            state.NextStepIndex    = step.StepIndex + 1;
        }

        return state;
    }
}
