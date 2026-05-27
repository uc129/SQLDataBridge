using DataBridge.Application.Interfaces;
using DataBridge.Application.TradePayable.Services;
using DataBridge.Application.TradePayable.UseCases.Commands;
using DataBridge.Domain.Models;
using DataBridge.Domain.TradePayable.Contracts;
using DataBridge.Domain.TradePayable.Models;
using System.Data;

namespace DataBridge.Application.TradePayable.UseCases;

public class RunFullPipelineUseCase(
    IDataProcessingService dataProcessingService,
    IPipelineRunRepository pipelineRunRepo,
    IStepResultRepository  stepResultRepo,
    IExcelWriter           excelWriter,
    IProgressNotifier      progressNotifier,
    IJobRegistry           jobRegistry)
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

            // If the pipeline previously completed Step02, load that checkpoint so we
            // don't re-read from the staging table or re-run Steps 0-2.
            if (run.CurrentStepIndex >= 2)
            {
                var checkpoint = await stepResultRepo.RetrieveStepResultAsync(run.RunId, 2);
                if (checkpoint.Rows.Count > 0)
                {
                    state.StepData[2]    = checkpoint;
                    state.CurrentStepIndex = 2;
                }
            }

            int totalSteps = RegisteredSteps.Length;

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

                state = await dataProcessingService.RunStepsUpTo(targetStep, state);
                await pipelineRunRepo.UpdateStepIndexAsync(cmd.RunId, state.CurrentStepIndex);
            }

            state.ProcessEndTime = DateTime.UtcNow;
            await pipelineRunRepo.UpdateStatusAsync(cmd.RunId, PipelineRunStatus.Completed);

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
