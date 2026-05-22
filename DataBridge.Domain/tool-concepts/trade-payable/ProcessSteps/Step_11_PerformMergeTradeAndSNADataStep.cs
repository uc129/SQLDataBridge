using Application.Extensions;
using Application.ProcessSteps.BackupTablesRepo;
using Application.ProcessSteps.ProcessStepsRepo;
using Domain.Aggregates;
using Domain.Contracts;
using Domain.Models.ProcessRun;



namespace Application.ProcessSteps
{
    public class PerformMergeTradeAndSNADataStep(
        IStepResultsRepository steprepo,
        IBackupTablesRepository backuprepo
        ) : IProcessStep
    {
        private readonly IStepResultsRepository _stepRepo = steprepo;
        private readonly IBackupTablesRepository _backuprepo = backuprepo;
        public int StepIndex => 11;
        public int NetDataTable_Local_Curr_StepIndex => 6;
        public int NetDataTable_Doc_Curr_StepIndex => StepIndex-1;
        public async Task<ProcessState> ExecuteAsync(ProcessState state)
        {

            var result = await _stepRepo.RetrieveStepResultAsIEnumerableAsync<FAGLL03NetLiability>(state.ProcessId, StepIndex);

            if (result != null && result.Any())
            {
                state.CurrentStepIndex = StepIndex;
                state.NextStepIndex = StepIndex + 1;
                return state;
            }

            System.Diagnostics.Debug.WriteLine($"Performing MergeTradeANd SNA Data for ProcessId: {state.ProcessId}");

            var appendtradeandnetlocalcurr = await _stepRepo.RetrieveStepResultAsync(state.ProcessId, NetDataTable_Local_Curr_StepIndex);

            var appendtradeandnetdoccurr = await _stepRepo.RetrieveStepResultAsync(state.ProcessId, NetDataTable_Doc_Curr_StepIndex);

            if (appendtradeandnetlocalcurr != null && appendtradeandnetdoccurr!= null)
            {

                var joineddata = DataProcessor.DataProcessor.MergeTradeAndSNAData(appendtradeandnetlocalcurr, appendtradeandnetdoccurr);
                var datawithJoinKeys = DataProcessor.DataProcessor.AddJoinKeysColumn(joineddata);

                if (joineddata != null) {
                    state.CurrentStepIndex = StepIndex;
                    state.NextStepIndex = StepIndex + 1;
                    await _stepRepo.SaveAndReplaceStepResultAsync(datawithJoinKeys, state.ProcessId, StepIndex);
                    var message = await _backuprepo.SaveAndAppendStepResultAsync(datawithJoinKeys, state.ProcessId, StepIndex);

                    System.Diagnostics.Debug.WriteLine($"Merge Trade and SNA Data Save Message: {message.Text}, Success: {message.Success}");
                }

                else
                {
                    throw new Exception("Error Merging Data in PerformMergeTradeAndSNADataStep.");
                }
            }

            return state;
        }
    }
}
