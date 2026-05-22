using Application.Data_Cleaning;
using Application.DataProcessFlow;
using Domain.Contracts;
using Domain.Models.Process;
using System.Data;


namespace Application.Process.ProcessSteps
{
    public class PerformHyperionClassificationStep(DataCleaner cleaner, IStepResultRepository stepRepo, BusinessLogicFunctions busilogic) : IProcessStep
    {
        private readonly DataCleaner _cleaner = cleaner;
        private readonly IStepResultRepository _stepRepo = stepRepo;
        private readonly BusinessLogicFunctions _busilogic = busilogic;
        public int StepIndex => 9;

        public async Task<ProcessState> ExecuteAsync(ProcessState state)
        {
            var result = await _stepRepo.RetrieveStepResultAsync(state.ProcessId, StepIndex);

            if (result != null && result.Rows.Count > 0)
            {
                state.CurrentStepIndex = StepIndex;
                state.NextStepIndex = StepIndex + 1;
                return state;
            }

            System.Diagnostics.Debug.WriteLine($"Performing PerformHyperionClassificationStep for ProcessId: {state.ProcessId}");
            DataTable previousStepResult = await _stepRepo.RetrieveStepResultAsync(state.ProcessId, StepIndex - 1);
            DataTable Data = _busilogic.HyperionCodeClassification(previousStepResult);

            if (Data != null)
            {
                await _stepRepo.SaveStepResultAsync(Data, state.ProcessId, StepIndex);
                state.CurrentStepIndex = StepIndex;
                state.NextStepIndex += 1;
            }
            return state;

            // Actual execution logic...
            // No need to check or call previous steps!
            // The orchestrator handles the sequence.
            // ...
        }
    }
}
