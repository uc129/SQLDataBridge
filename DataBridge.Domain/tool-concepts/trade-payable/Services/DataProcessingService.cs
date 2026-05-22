using Domain.Contracts;
using Domain.Models.ProcessRun;


namespace Application.Services
{
    public class DataProcessingService(IEnumerable<IProcessStep> allSteps) : IDataProcessingService // Now the Orchestrator
    {
        private readonly IEnumerable<IProcessStep> _allSteps = allSteps.OrderBy(s => s.StepIndex);

        public async Task<ProcessState> RunStepsUpTo(int targetStepIndex, ProcessState state)
        {
            var stepsToRun = _allSteps.Where(s => s.StepIndex >= state.CurrentStepIndex && s.StepIndex <= targetStepIndex);

            foreach (var step in stepsToRun)
            {
                state = await step.ExecuteAsync(state);
                state.CurrentStepIndex = step.StepIndex;
                state.NextStepIndex = step.StepIndex + 1;
            }

            return state;
        }
    }
}
