using Application.Extensions;
using Application.ProcessSteps.BackupTablesRepo;
using Application.ProcessSteps.ProcessStepsRepo;
using Domain.Aggregates;
using Domain.Contracts;
using Domain.Models.ProcessRun;
using Infrastructure.Contracts;
using Shared.Extensions;

namespace Application.ProcessSteps
{
    public class PopulateRawDataStep(
        IFAGLL03Repository fagll03repo, 
        IStepResultsRepository stepRepo, 
        IBackupTablesRepository backuprepo,
        DataProcessor.DataProcessor dataprocess
        


        ) : IProcessStep
    {
        private readonly IFAGLL03Repository _fagll03repo = fagll03repo;
        private readonly IStepResultsRepository _stepRepo = stepRepo;
        private readonly IBackupTablesRepository _backuprepo = backuprepo;
        private readonly DataProcessor.DataProcessor _dataprocess = dataprocess;
        public int StepIndex => 1;
        public async Task<ProcessState> ExecuteAsync(ProcessState state)
        {
            var result = await _stepRepo.RetrieveStepResultAsIEnumerableAsync<FAGLL03RAWEntity>(state.ProcessId, StepIndex);

            if (result != null && result.Any())
            {
                state.CurrentStepIndex = StepIndex;
                state.NextStepIndex = StepIndex + 1;
                return state;
            }

            System.Diagnostics.Debug.WriteLine($"Performing PopulateRawDataStep for ProcessId: {state.ProcessId}");

            IEnumerable<FAGLL03RAWEntity> rawdata;
            //if (!string.IsNullOrEmpty(state.RevisionNumber) && !string.IsNullOrWhiteSpace(state.RevisionNumber))
            //    rawdata = await _fagll03repo.GetByRevisionNumber(state.RevisionNumber);
            //else
                rawdata = await _fagll03repo.GetAllAsync();

            foreach(var record in rawdata)
                record.QuarterEndDate = state.CurrentQuarter.Date;

            await _backuprepo.SaveAndAppendStepResultAsync(rawdata.ToDataTable(),state.ProcessId,0);


            var populatedData = _dataprocess.ProcessRawData(rawdata);
                if (populatedData != null) { 
                    state.CurrentStepIndex = StepIndex;
                    state.NextStepIndex = StepIndex + 1;
                var datatable = populatedData.ToDataTable();
                await _stepRepo.SaveAndReplaceStepResultAsync(datatable, state.ProcessId, StepIndex);
                await _backuprepo.SaveAndAppendStepResultAsync(datatable, state.ProcessId, StepIndex);

            }
            return state;
        }
    }
}
