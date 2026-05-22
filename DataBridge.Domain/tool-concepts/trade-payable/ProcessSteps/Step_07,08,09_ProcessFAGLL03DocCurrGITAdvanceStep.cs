using Application.DataProcessor;
using Application.Extensions;
using Application.ProcessSteps.BackupTablesRepo;
using Application.ProcessSteps.ProcessStepsRepo;
using Domain.Contracts;
using Domain.Models.ProcessRun;
using Infrastructure.Contracts;
using Infrastructure.Database;
using Shared.Extensions;
using System.Data;


namespace Application.ProcessSteps
{
    public class ProcessFAGLL03DocCurrGITAdvanceStep( 
        IMerged_FAGLL03Repository mergedRepo, 
        IStepResultsRepository stepRepo,
        IBackupTablesRepository backupRepo,
        DataToDB dbhelper,
        GITDocCurrProcessor gitprocess,
        IDataSettings datasettings
        ) : IProcessStep
    {
        private readonly IMerged_FAGLL03Repository _merged_repo = mergedRepo;
        private readonly IStepResultsRepository _stepRepo = stepRepo;
        private readonly IBackupTablesRepository _backupRepo = backupRepo;
        private readonly DataToDB _dbhelper = dbhelper;
        private readonly GITDocCurrProcessor _gitprocess = gitprocess;
        private readonly IDataSettings _datasettings = datasettings;


        public int StepIndex => 7;
        public async Task<ProcessState> ExecuteAsync(ProcessState state)
        {
            System.Diagnostics.Debug.WriteLine($"Performing ProcessFAGLLO3GIT_DocCurr for ProcessId: {state.ProcessId}"); ;

            var thisStepData1 = await _stepRepo.RetrieveStepResultAsync(state.ProcessId, StepIndex);
            var thisStepData2 = await _stepRepo.RetrieveStepResultAsync(state.ProcessId, StepIndex + 1);
            var thisStepData3 = await _stepRepo.RetrieveStepResultAsync(state.ProcessId, StepIndex + 2);

            if (thisStepData1.Rows.Count > 0 && thisStepData2.Rows.Count > 0 && thisStepData3.Rows.Count > 0)
            {
                state.CurrentStepIndex = StepIndex +2;
                state.NextStepIndex = StepIndex +3;
                return state;
            }

            var prevStepData = await _merged_repo.GetAllAsync();

            //foreach(var record in prevStepData)
            //{
            //    if (record.GL_Account == "14710")
            //    {
            //        System.Diagnostics.Debug.WriteLine(record.IsSNACompany);
            //    }
            //}
            var mergedDataTable = prevStepData.ToDataTable();
            List<DataTable> GitData = _gitprocess.ProcessFAGLL03DocCurrGITData(mergedDataTable);

            if (GitData != null && GitData.Count > 0)
            {
                await _stepRepo.SaveAndReplaceStepResultAsync(DataProcessor.DataProcessor.AddJoinKeysColumn(GitData[0]), state.ProcessId, StepIndex);
                await _backupRepo.SaveAndAppendStepResultAsync(DataProcessor.DataProcessor.AddJoinKeysColumn(GitData[0]), state.ProcessId, StepIndex);

                state.CurrentStepIndex = StepIndex;
                state.NextStepIndex = StepIndex+1;

                await _stepRepo.SaveAndReplaceStepResultAsync(GitData[1], state.ProcessId, StepIndex + 1);
                await _backupRepo.SaveAndAppendStepResultAsync(GitData[1], state.ProcessId, StepIndex + 1);

                state.CurrentStepIndex = StepIndex + 1;
                state.NextStepIndex = StepIndex + 2;


                await _stepRepo.SaveAndReplaceStepResultAsync(DataProcessor.DataProcessor.AddJoinKeysColumn(GitData[2]), state.ProcessId, StepIndex + 2);
                await _backupRepo.SaveAndAppendStepResultAsync(DataProcessor.DataProcessor.AddJoinKeysColumn(GitData[2]), state.ProcessId, StepIndex + 2);

                state.CurrentStepIndex = StepIndex + 2;
                state.NextStepIndex = StepIndex + 3;

                var dbName = _datasettings.GetTableConfigDataByKey("TradePayableDbName");
                var tableName = _datasettings.GetTableConfigDataByKey("Step_7_1");
                var backupTableName = _datasettings.GetTableConfigDataByKey("Backup_Step_7_1");

                await _dbhelper.ProcessAndSaveLargeData(GitData[3], tableName);
                await _dbhelper.ProcessAndInsertData(GitData[3], backupTableName, dbName, state.ProcessId, 77);
            }
            return state;
        }
    }
}
