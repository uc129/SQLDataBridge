using Application.DataProcessor;
using Application.ProcessSteps.BackupTablesRepo;
using Application.ProcessSteps.ProcessStepsRepo;
using Domain.Aggregates;
using Domain.Contracts;
using Domain.Models.ProcessRun;
using Infrastructure.Contracts.ServiceContracts;
using Shared.Extensions;




namespace Application.ProcessSteps
{
    public class PerformFixCPAgeingAndHyperionClassification(
        IStepResultsRepository steprepo,
        IBackupTablesRepository backuprepo,
        DataProcessor.DataProcessor processor,
        ISNABalanceApproveService snaservice
        ) : IProcessStep
    {
        private readonly IStepResultsRepository _stepRepo = steprepo;
        private readonly IBackupTablesRepository _backuprepo = backuprepo;
        private readonly DataProcessor.DataProcessor _processor = processor;
        private readonly ISNABalanceApproveService _snaservice = snaservice;
        public int StepIndex => 12;

        public async Task<ProcessState> ExecuteAsync(ProcessState state)
        {

            var result = await _stepRepo.RetrieveStepResultAsIEnumerableAsync<FAGLL03NetLiability>(state.ProcessId, StepIndex);

            if (result != null && result.Any())
            {
                state.CurrentStepIndex = StepIndex;
                state.NextStepIndex = StepIndex + 1;
                return state;
            }

            System.Diagnostics.Debug.WriteLine($"Performing FixCPAgeingAndHyperionClassification for ProcessId: {state.ProcessId}");

            var previousStepResult = await _stepRepo.RetrieveStepResultAsIEnumerableAsync<FAGLL03NetLiability>(state.ProcessId, StepIndex - 1);


            var fixedCPdata = _processor.MSMECreditPeriodFixEnumerable(previousStepResult);
            var dataWithBaseHyperions = _processor.AssignBaseHyperionsEnumerable(fixedCPdata);
            var dataWithAgeingAndHyperion = _processor.HyperionClassificationEnumerable(dataWithBaseHyperions, state.CurrentQuarter.Date);


            //var dataWithVertical = DataProcessor.DataProcessor.AssignCorporateLabelEnumerable(dataWithJournalEntry);
            var dataWithICPHyperion = _processor.AssignICPHyperionCodesEnumerable(dataWithAgeingAndHyperion);
            var dataWithERV = _processor.SNAERVCalculationEnumerable(dataWithICPHyperion, state.CurrentQuarter.Date);
            var mergedData = DataProcessor.DataProcessor.MergeICPHyperionAndAmountDocINR(dataWithERV);
            var dataWithJournalEntry = HelperFunctions.CalculateJournalEntryEnumerable(dataWithAgeingAndHyperion);


            if (dataWithJournalEntry != null)
            {
                var datatable = dataWithJournalEntry.ToDataTable();
                state.CurrentStepIndex = StepIndex;
                state.NextStepIndex = StepIndex + 1;
                
                await _stepRepo.SaveAndReplaceStepResultAsync(DataProcessor.DataProcessor.AddJoinKeysColumn(datatable), state.ProcessId, StepIndex);

                var message = await _backuprepo.SaveAndAppendStepResultAsync(DataProcessor.DataProcessor.AddJoinKeysColumn(datatable), state.ProcessId, StepIndex);

                System.Diagnostics.Debug.WriteLine($"FixCPAgeingAndHyperionClassification Save Message: {message.Text}, Success: {message.Success}");

                var snaData = await _snaservice.SaveSNABalanceData(mergedData, state.ProcessId);
                if (snaData.Any()) {
                    //System.Diagnostics.Debug.WriteLine("Performing SNA Balance Compariosn with previous revisions");
                 var comapreResult =  await _snaservice.ProcessAndSaveLatestApprovals(snaData,state.CurrentQuarter.Date); 
                }
            }

            return state;
        }
    }
}
