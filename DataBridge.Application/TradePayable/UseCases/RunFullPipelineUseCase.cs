using DataBridge.Application.Interfaces;
using DataBridge.Application.TradePayable.Processing;
using DataBridge.Application.TradePayable.Services;
using DataBridge.Application.TradePayable.UseCases.Commands;
using DataBridge.Domain.Models;
using DataBridge.Domain.TradePayable.Contracts;
using DataBridge.Domain.TradePayable.Models;
using System.Data;
using System.Diagnostics;

namespace DataBridge.Application.TradePayable.UseCases;

public class RunFullPipelineUseCase(
    IDataProcessingService  dataProcessingService,
    IPipelineRunRepository  pipelineRunRepo,
    IStepResultRepository   stepResultRepo,
    IExcelWriter            excelWriter,
    IProgressNotifier       progressNotifier,
    IJobRegistry            jobRegistry,
    IProcessSummaryRepository summaryRepo)
{
    // Registered step class indices — used only for progress percentage math.
    private static readonly int[] RegisteredSteps = [0, 1, 2, 3, 6, 7, 10, 11, 12, 13];

    public async Task ExecuteAsync(RunFullPipelineCommand cmd)
    {
        var ct = jobRegistry.Register(cmd.JobId);
        try
        {
            var run = await pipelineRunRepo.GetByRunIdAsync(cmd.RunId)
                ?? throw new InvalidOperationException($"Pipeline run {cmd.RunId} not found.");

            await pipelineRunRepo.UpdateStatusAsync(cmd.RunId, PipelineRunStatus.Running);

            var state = new ProcessState
            {
                RunId            = run.RunId,
                CurrentQuarter   = run.QuarterDate,
                RevisionNumber   = run.RevisionNumber,
                ReportDate       = run.QuarterDate,
                ProcessStartTime = DateTime.UtcNow,
            };

            // Resume from the highest available checkpoint in the DB.
            int lastRegistered = RegisteredSteps[^1];
            foreach (var (setTo, slots) in RunPipelineStepUseCase.CheckpointTiers)
            {
                if (lastRegistered <= setTo) continue;
                if (run.CurrentStepIndex < setTo) continue;

                var loaded    = new Dictionary<int, DataTable>(slots.Length);
                bool allFound = true;
                foreach (var slot in slots)
                {
                    var dt = await stepResultRepo.RetrieveStepResultAsync(run.RunId, slot);
                    if (dt.Rows.Count == 0) { allFound = false; break; }
                    loaded[slot] = dt;
                }
                if (!allFound) continue;

                foreach (var (slot, dt) in loaded)
                    state.StepData[slot] = dt;
                state.CurrentStepIndex = setTo;
                break;
            }

            int    totalSteps    = RegisteredSteps.Length;
            string? accStatsJson = run.StepStatsJson;
            var    totalSw       = Stopwatch.StartNew();

            for (int i = 0; i < RegisteredSteps.Length; i++)
            {
                int targetStep = RegisteredSteps[i];
                if (targetStep <= state.CurrentStepIndex) continue;

                ct.ThrowIfCancellationRequested();

                int pct = (int)((double)i / totalSteps * 95);
                await progressNotifier.NotifyAsync(cmd.JobId, new ProgressMessage
                {
                    JobId     = cmd.JobId,
                    Stage     = "Running",
                    Message   = $"Running step {targetStep}…",
                    Percent   = pct,
                    RowsDone  = i,
                    RowsTotal = totalSteps,
                });

                var stepSw = Stopwatch.StartNew();
                state = await dataProcessingService.RunStepsUpTo(targetStep, state);
                stepSw.Stop();

                await pipelineRunRepo.UpdateStepIndexAsync(cmd.RunId, state.CurrentStepIndex);

                accStatsJson = StepStatsComputer.MergeStepStats(accStatsJson, state.CurrentStepIndex, state, stepSw.Elapsed);
            }

            totalSw.Stop();
            accStatsJson = StepStatsComputer.SetPipelineRuntime(accStatsJson, totalSw.Elapsed);

            state.ProcessEndTime = DateTime.UtcNow;

            if (state.Summary is { } completedSummary)
                await summaryRepo.UpsertAsync(cmd.RunId, run.QuarterDate, completedSummary);

            await pipelineRunRepo.UpdateStatusAsync(cmd.RunId, PipelineRunStatus.Completed);
            await pipelineRunRepo.UpdateStepStatsAsync(cmd.RunId, accStatsJson ?? string.Empty);

            // Write the final processed result to a temp Excel so it is downloadable.
            WriteResultExcel(state, cmd.RunId);

            var summaryMsg = state.Summary is { } s
                ? $"  Net liability: {s.NetLiabilityAmountLocal:N0}"
                : string.Empty;

            await progressNotifier.NotifyAsync(cmd.JobId, new ProgressMessage
            {
                JobId      = cmd.JobId,
                Stage      = "Done",
                Message    = $"Pipeline complete in {state.ProcessDuration.TotalSeconds:F1}s.{summaryMsg}",
                Percent    = 100,
                RowsDone   = totalSteps,
                RowsTotal  = totalSteps,
                IsComplete = true,
            });
        }
        catch (OperationCanceledException)
        {
            await pipelineRunRepo.UpdateStatusAsync(cmd.RunId, PipelineRunStatus.Cancelled);
            await progressNotifier.NotifyAsync(cmd.JobId,
                new ProgressMessage { JobId = cmd.JobId, Stage = "Cancelled", Message = "Pipeline cancelled.", Percent = 0 });
        }
        catch (Exception ex)
        {
            await pipelineRunRepo.UpdateStatusAsync(cmd.RunId, PipelineRunStatus.Failed);
            await progressNotifier.NotifyAsync(cmd.JobId,
                new ProgressMessage { JobId = cmd.JobId, Stage = "Error", Message = ex.Message, Percent = 0, IsError = true });
        }
        finally
        {
            jobRegistry.Remove(cmd.JobId);
        }
    }

    private void WriteResultExcel(ProcessState state, Guid runId)
    {
        if (!state.StepData.TryGetValue(12, out var dt) || dt.Rows.Count == 0) return;
        try
        {
            var folder = StepTempFolder(runId);
            Directory.CreateDirectory(folder);
            var cols = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
            var rows = dt.Rows.Cast<DataRow>()
                         .Select(r => cols.Select(c => r[c] == DBNull.Value ? null : r[c]).ToArray())
                         .Cast<object?[]>().ToList();
            excelWriter.WritePartFile(cols, rows, "Step12", "Final Result", folder, 1);
        }
        catch { /* best-effort */ }
    }

    internal static string StepTempFolder(Guid runId) =>
        Path.Combine(Path.GetTempPath(), "DataBridge", "steps", runId.ToString());
}
