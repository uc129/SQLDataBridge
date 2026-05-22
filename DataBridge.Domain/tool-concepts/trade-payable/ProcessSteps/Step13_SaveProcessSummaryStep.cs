
using Application.ProcessSteps.BackupTablesRepo;
using Application.ProcessSteps.ProcessStepsRepo;
using Domain.Aggregates;
using Domain.Contracts;
using Domain.Models.ProcessRun;
using Infrastructure.Contracts;
using static Application.DataProcessor.DataProcessor;



namespace Application.ProcessSteps
{
	public class PerformSaveProcessSummaryStep(
		IStepResultsRepository steprepo, 
		IProcessRunSummaryRepository processrunrepo
		) : IProcessStep
	{
		private readonly IStepResultsRepository _stepRepo = steprepo;
		private readonly IProcessRunSummaryRepository _processrunrepo = processrunrepo;
		public int StepIndex => 13;
		public async Task<ProcessState> ExecuteAsync(ProcessState state)
		{
			System.Diagnostics.Debug.WriteLine($"Performing Save ProcessRunSummary for ProcessId: {state.ProcessId}");

			var finalResult = await _stepRepo.RetrieveStepResultAsIEnumerableAsync<FAGLL03ProcessedResult>(state.ProcessId, StepIndex - 1);
			var gitProcessed = await _stepRepo.RetrieveStepResultAsIEnumerableAsync<FAGLL03ProcessedGITLocal>(state.ProcessId, 3);
			state.ProcessEndTime = DateTime.Now;
			var parameters = new RunParameters(){ ProcessId= state.ProcessId, QuarterDate= state.CurrentQuarter.Date, ProcessDuration= state.ProcessDuration };
			ProcessRun runsummary = GenerateProcessSummary(finalResult, gitProcessed, parameters, state);
			

			if (runsummary != null)
			{
				state.CurrentStepIndex = StepIndex;
				state.NextStepIndex = StepIndex + 1;
				await _processrunrepo.SaveProcessRunAsync(runsummary);
			}

			return state;
		}
	}
}
