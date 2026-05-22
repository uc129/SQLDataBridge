using Application.Data_Cleaning;
using Application.DataProcessor;
using Application.Extensions;
using Application.ProcessSteps.BackupTablesRepo;
using Application.ProcessSteps.ProcessStepsRepo;
using Application.Services;
using Domain.Contracts;
using Domain.Models.ProcessRun;
using System.Data;


namespace Application.ProcessSteps
{
    public class PerformAppendTradeAndDocCurrNetLiabilityStep( 
        IStepResultsRepository stepRepo,
        IBackupTablesRepository backupRepo,
        GITDocCurrProcessor gitprocess
        ) : IProcessStep
    {
        private readonly IStepResultsRepository _stepRepo = stepRepo;
        private readonly IBackupTablesRepository _backupRepo = backupRepo;
        private readonly GITDocCurrProcessor _gitprocess = gitprocess;
        public int StepIndex => 10;

        public async Task<ProcessState> ExecuteAsync(ProcessState state)
        {
            var result= await _stepRepo.RetrieveStepResultAsync(state.ProcessId, StepIndex);
            if (result != null && result.Rows.Count > 0)
            {
                state.CurrentStepIndex = StepIndex;
                state.NextStepIndex = StepIndex + 1;
                return state;
            }
            System.Diagnostics.Debug.WriteLine($"Performing AppendTradeAndNetLiabilityDocCurrStep for ProcessId: {state.ProcessId}");
            DataTable populatedData = await _stepRepo.RetrieveStepResultAsync(state.ProcessId, StepIndex - 1);
            DataTable netLiability = await _stepRepo.RetrieveStepResultAsync(state.ProcessId, StepIndex - 2);
            DataTable netDataTable = _gitprocess.AppendTradeAndGITLiabilityDocCurrTable(populatedData, netLiability);
            if (netDataTable != null)
            {
                //var dataWithBaseHyperions = DataProcessor.DataProcessor.AssignBaseHyperions(netDataTable);
                await _stepRepo.SaveAndReplaceStepResultAsync(DataProcessor.DataProcessor.AddJoinKeysColumn(netDataTable), state.ProcessId, StepIndex);
                await _backupRepo.SaveAndAppendStepResultAsync(DataProcessor.DataProcessor.AddJoinKeysColumn(netDataTable), state.ProcessId, StepIndex);
                state.CurrentStepIndex = StepIndex;
                state.NextStepIndex += 1;
            }
            return state;
        }
    }
}
