using Application.DataProcessor;
using Application.Extensions;
using Application.ProcessSteps.BackupTablesRepo;
using Application.ProcessSteps.ProcessStepsRepo;
using Domain.Aggregates;
using Domain.Contracts;
using Domain.Models.ProcessRun;
using Infrastructure.Contracts;
using Infrastructure.Database;
using Shared.Extensions;
using System.Data;


namespace Application.ProcessSteps
{
    public class ProcessFagll03GITAdvanceStep( 
        IMerged_FAGLL03Repository mergedRepo, 
        IStepResultsRepository stepRepo,
        IBackupTablesRepository backuprepo,
        DataToDB dbhelper, 
        GITProcessor gitprocess,
        IDataSettings datasettings
        ) : IProcessStep
    {
        private readonly IMerged_FAGLL03Repository _merged_repo = mergedRepo;
        private readonly IStepResultsRepository _stepRepo = stepRepo;
        private readonly IBackupTablesRepository _backuprepo = backuprepo;
        private readonly DataToDB _dbhelper = dbhelper;
        private readonly GITProcessor _gitprocess = gitprocess;
        private readonly IDataSettings _datasettings = datasettings;


        public int StepIndex => 3;
        public async Task<ProcessState> ExecuteAsync(ProcessState state)
        {
            System.Diagnostics.Debug.WriteLine($"Performing ProcessFAGLLO3GIT_local for ProcessId: {state.ProcessId}"); ;

            var thisStepData1 = await _stepRepo.RetrieveStepResultAsync(state.ProcessId, StepIndex);
            var thisStepData2 = await _stepRepo.RetrieveStepResultAsync(state.ProcessId, StepIndex + 1);
            var thisStepData3 = await _stepRepo.RetrieveStepResultAsync(state.ProcessId, StepIndex + 2);

            if (thisStepData1.Rows.Count > 0 && thisStepData2.Rows.Count > 0 && thisStepData3.Rows.Count > 0)
            {
                state.CurrentStepIndex = StepIndex +2;
                state.NextStepIndex = StepIndex +3;
                return state;
            }


            IEnumerable<FAGLL03_JoinedAndMerged> prevStepData;

            //if (!string.IsNullOrEmpty(state.RevisionNumber) && !string.IsNullOrWhiteSpace(state.RevisionNumber))
            //    prevStepData = await _merged_repo.GetByRevisionNumber(state.RevisionNumber);
            //else
                prevStepData = await _merged_repo.GetAllAsync();

            var mergedDataTable = prevStepData.ToDataTable();
            List<DataTable> GitData = _gitprocess.ProcessFAGLL03GITData(mergedDataTable);

            if (GitData != null && GitData.Count > 0)
            {
                var dataWithJoinKeys = DataProcessor.DataProcessor.AddJoinKeysColumn(GitData[0]);
                await _stepRepo.SaveAndReplaceStepResultAsync(dataWithJoinKeys, state.ProcessId, StepIndex); //Processed GIT
                await _backuprepo.SaveAndAppendStepResultAsync(dataWithJoinKeys, state.ProcessId, StepIndex); //Processed GIT

                state.CurrentStepIndex = StepIndex;
                state.NextStepIndex = StepIndex+1;

                await _stepRepo.SaveAndReplaceStepResultAsync(GitData[1],  state.ProcessId, StepIndex + 1); //Unpivoted GIT
                await _backuprepo.SaveAndAppendStepResultAsync(GitData[1], state.ProcessId, StepIndex + 1); //Unpivoted GIT

                state.CurrentStepIndex = StepIndex + 1;
                state.NextStepIndex = StepIndex + 2;

                await _stepRepo.SaveAndReplaceStepResultAsync(DataProcessor.DataProcessor.AddJoinKeysColumn(GitData[2]), state.ProcessId, StepIndex + 2); //Populated Data With Group Keys

                await _backuprepo.SaveAndAppendStepResultAsync(DataProcessor.DataProcessor.AddJoinKeysColumn(GitData[2]), state.ProcessId, StepIndex + 2); //Populated Data With Group Keys

                var dbName = _datasettings.GetTableConfigDataByKey("TradePayableDbName");
                var tableName4 = _datasettings.GetTableConfigDataByKey("Step_3_1");
                var backuptable4 = _datasettings.GetTableConfigDataByKey("Backup_Step_3_1");

                await _dbhelper.ProcessAndSaveLargeData(GitData[3],tableName4);
                await _dbhelper.ProcessAndInsertData(GitData[3], backuptable4, dbName, state.ProcessId, 33);

                state.CurrentStepIndex = StepIndex + 2;
                state.NextStepIndex = StepIndex + 3;
            }
            return state;
        }
    }
}
