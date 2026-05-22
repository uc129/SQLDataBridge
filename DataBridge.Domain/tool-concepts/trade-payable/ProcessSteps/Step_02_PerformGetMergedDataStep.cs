using Application.Extensions;
using Application.ProcessSteps.ProcessStepsRepo;
using Application.Services;
using Domain.Aggregates;
using Domain.Contracts;
using Domain.Models.ProcessRun;
using Infrastructure.Contracts;



namespace Application.ProcessSteps
{
    public class PerformGetMergedDataStep( IMerged_FAGLL03Repository merged_repo) : IProcessStep
    {
        private readonly IMerged_FAGLL03Repository _merged_repo = merged_repo;
        public int StepIndex => 2;

        public Task<ProcessState> ExecuteAsync(ProcessState state)
        {
            System.Diagnostics.Debug.WriteLine($"Performing PerformGetMergedDataStep for ProcessId: {state.ProcessId}"); ;
            state.CurrentStepIndex = StepIndex;
            state.NextStepIndex = StepIndex + 1;
            return Task.FromResult(state);
        }
    }
}
