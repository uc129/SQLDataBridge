using DataBridge.Application.Interfaces;
using DataBridge.Application.TradePayable.Services;
using DataBridge.Application.TradePayable.UseCases.Commands;
using DataBridge.Domain.Models;
using DataBridge.Domain.TradePayable.Contracts;
using DataBridge.Domain.TradePayable.Models;
using System.Data;

namespace DataBridge.Application.TradePayable.UseCases;

public class RunPipelineStepUseCase(
    IDataProcessingService  dataProcessingService,
    IPipelineRunRepository  pipelineRunRepo,
    IStepResultRepository   stepResultRepo,
    IPipelineMemoryStore    memoryStore,
    IFAGLL03StagingRepository stagingRepo,
    IExcelWriter            excelWriter,
    IProgressNotifier       progressNotifier,
    IJobRegistry            jobRegistry)
{
    public async Task ExecuteAsync(RunPipelineStepCommand cmd)
    {
        var ct = jobRegistry.Register(cmd.JobId);
        try
        {
            var run = await pipelineRunRepo.GetByRunIdAsync(cmd.RunId)
                ?? throw new InvalidOperationException($"Pipeline run {cmd.RunId} not found.");

            await pipelineRunRepo.UpdateStatusAsync(cmd.RunId, PipelineRunStatus.Running);
            await progressNotifier.NotifyAsync(cmd.JobId, Notify("Running",
                $"Bootstrapping state for step {cmd.TargetStepIndex}…", 5));

            ct.ThrowIfCancellationRequested();

            var state = await BootstrapStateAsync(run, cmd.TargetStepIndex);
            state = await dataProcessingService.RunStepsUpTo(cmd.TargetStepIndex, state);

            await pipelineRunRepo.UpdateStepIndexAsync(cmd.RunId, state.CurrentStepIndex);
            await pipelineRunRepo.UpdateStatusAsync(cmd.RunId, PipelineRunStatus.StepComplete);

            var writtenFiles = WriteStepExcels(state, cmd.RunId);
            var fileNames    = writtenFiles.Count > 0
                ? writtenFiles.Select(Path.GetFileName).OfType<string>().ToList()
                : null;

            await progressNotifier.NotifyAsync(cmd.JobId, new ProgressMessage
            {
                JobId       = cmd.JobId,
                Stage       = "Done",
                Message     = $"Step {state.CurrentStepIndex} complete.",
                Percent     = 100,
                IsComplete  = true,
                OutputFiles = fileNames,
            });
        }
        catch (OperationCanceledException)
        {
            await pipelineRunRepo.UpdateStatusAsync(cmd.RunId, PipelineRunStatus.Cancelled);
            await progressNotifier.NotifyAsync(cmd.JobId, Notify("Cancelled", "Step cancelled.", 0));
        }
        catch (Exception ex)
        {
            await pipelineRunRepo.UpdateStatusAsync(cmd.RunId, PipelineRunStatus.Failed);
            await progressNotifier.NotifyAsync(cmd.JobId, Notify("Error", ex.Message, 0, isError: true));
        }
        finally
        {
            jobRegistry.Remove(cmd.JobId);
        }
    }

    /// <summary>
    /// Populates ProcessState.StepData with the appropriate starting point:
    /// - If target ≤ 2 or no checkpoint exists → load raw entities (from memory store or DB) and start from Step00.
    /// - If target > 2 and checkpoint exists → load Step02 result from DB and skip Steps 0-2.
    /// </summary>
    private async Task<ProcessState> BootstrapStateAsync(PipelineRun run, int targetStep)
    {
        var state = new ProcessState
        {
            RunId            = run.RunId,
            CurrentQuarter   = run.QuarterDate,
            RevisionNumber   = run.RevisionNumber,
            ReportDate       = run.QuarterDate,
            ProcessStartTime = DateTime.UtcNow,
        };

        if (targetStep > 2)
        {
            var checkpoint = await stepResultRepo.RetrieveStepResultAsync(run.RunId, 2);
            if (checkpoint.Rows.Count > 0)
            {
                state.StepData[2]      = checkpoint;
                state.CurrentStepIndex = 2;
                return state;
            }
        }

        // No checkpoint, or target is within the pre-checkpoint region: ensure raw entities are in store.
        if (memoryStore.Get(run.RunId) is null)
        {
            var rows = await stagingRepo.GetByRunIdAsync(run.RunId);
            memoryStore.Store(run.RunId, rows.ToList());
        }

        state.CurrentStepIndex = 0;
        return state;
    }

    // Steps that produce more than one output slot.
    private static readonly Dictionary<int, int[]> MultiOutputSlots = new()
    {
        [3] = [3, 4, 5, 31],
        [7] = [7, 8, 9, 71],
    };

    /// <summary>
    /// Writes all output slots for the completed step to a temp folder keyed by runId
    /// so the download endpoint can serve them via GET /download?runId=&amp;step=N.
    /// </summary>
    private List<string> WriteStepExcels(ProcessState state, Guid runId)
    {
        var slots = MultiOutputSlots.TryGetValue(state.CurrentStepIndex, out var multi)
            ? multi
            : [state.CurrentStepIndex];

        var written = new List<string>();
        var folder  = RunFullPipelineUseCase.StepTempFolder(runId);
        Directory.CreateDirectory(folder);

        foreach (var slot in slots)
        {
            if (!state.StepData.TryGetValue(slot, out var dt) || dt.Rows.Count == 0) continue;
            try
            {
                var cols     = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
                var rows     = dt.Rows.Cast<DataRow>()
                                 .Select(r => cols.Select(c => r[c] == DBNull.Value ? null : r[c]).ToArray())
                                 .Cast<object?[]>().ToList();
                var stepName = slot switch
                {
                    31  => "Step03_AllGrouped",
                    71  => "Step07_AllGrouped",
                    var n => $"Step{n:D2}",
                };
                var path = excelWriter.WritePartFile(cols, rows, stepName, $"Step {slot}", folder, 1);
                if (path is not null) written.Add(path);
            }
            catch { /* best-effort */ }
        }

        return written;
    }

    private static ProgressMessage Notify(string stage, string message, int percent, bool isError = false) =>
        new() { Stage = stage, Message = message, Percent = percent, IsError = isError };
}
