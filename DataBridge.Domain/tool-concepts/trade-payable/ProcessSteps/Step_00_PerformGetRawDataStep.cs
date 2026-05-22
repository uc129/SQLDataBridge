using Application.DataProcessor;
using Application.ProcessSteps.BackupTablesRepo;
using Domain.Contracts;
using Domain.Models.ProcessRun;
using Infrastructure.Contracts;

namespace Application.ProcessSteps
{
    public class PerformGetRawDataStep( IFAGLL03Repository fagllrepo) : IProcessStep
    {
        private readonly IFAGLL03Repository _fagllrepo = fagllrepo;

        public int StepIndex => 0;

        public async Task<ProcessState> ExecuteAsync(ProcessState state)
        {
            if (!string.IsNullOrEmpty(state.RevisionNumber) && !string.IsNullOrWhiteSpace(state.RevisionNumber))
                System.Diagnostics.Debug.WriteLine($"Starting Data Processing for Raw Data Revision Number {state.RevisionNumber}"); 

            else
                System.Diagnostics.Debug.WriteLine($"Starting Data Processing for all Raw Data Revisions"); ;

                _ = await _fagllrepo.GetPaginatedDataAsync(10, 10);
                System.Diagnostics.Debug.WriteLine($"Performing GetRawDataStep for ProcessId: {state.ProcessId}"); ;
                state.CurrentQuarter = HelperFunctions.GetLastDateOfCurrentQuarter(HelperFunctions.GetCurrentQuarterDetails(state.InitialInputs));
                state.CurrentStepIndex = 0;
                state.NextStepIndex += 1;
                return state;
        }

        
    }
}
