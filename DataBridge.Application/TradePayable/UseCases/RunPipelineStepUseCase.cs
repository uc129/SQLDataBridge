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

public class RunPipelineStepUseCase(
    IDataProcessingService  dataProcessingService,
    IPipelineRunRepository  pipelineRunRepo,
    IStepResultRepository   stepResultRepo,
    IPipelineMemoryStore    memoryStore,
    IFAGLL03StagingRepository stagingRepo,
    IExcelWriter            excelWriter,
    IProgressNotifier       progressNotifier,
    IJobRegistry            jobRegistry,
    IProcessSummaryRepository summaryRepo)
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

            using var heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var heartbeat = SimulateProgressAsync(cmd.JobId, cmd.TargetStepIndex, heartbeatCts.Token);

            var sw    = Stopwatch.StartNew();
            var state = await BootstrapStateAsync(run, cmd.TargetStepIndex);
            state = await dataProcessingService.RunStepsUpTo(cmd.TargetStepIndex, state);
            sw.Stop();

            if (state.CurrentStepIndex == 13 && state.Summary is { } stepSummary)
                await summaryRepo.UpsertAsync(cmd.RunId, run.QuarterDate, stepSummary);

            await heartbeatCts.CancelAsync();
            try { await heartbeat; } catch (OperationCanceledException) { }

            await pipelineRunRepo.UpdateStepIndexAsync(cmd.RunId, state.CurrentStepIndex);
            await pipelineRunRepo.UpdateStatusAsync(cmd.RunId, PipelineRunStatus.StepComplete);

            var statsJson = StepStatsComputer.MergeStepStats(run.StepStatsJson, state.CurrentStepIndex, state, sw.Elapsed);
            await pipelineRunRepo.UpdateStepStatsAsync(cmd.RunId, statsJson);

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

        foreach (var (setTo, slots) in CheckpointTiers)
        {
            if (targetStep <= setTo) continue;

            var loaded   = new Dictionary<int, DataTable>(slots.Length);
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
            return state;
        }

        // No checkpoint available — load raw entities and start from Step 0.
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

    // Checkpoint tiers ordered from most-advanced to least-advanced.
    // Each tier: (CurrentStepIndex to set, DB slots that must all be non-empty).
    internal static readonly (int StepIndexToSet, int[] SlotsToLoad)[] CheckpointTiers =
    [
        (12, [12]),
        (11, [11]),
        (10, [6, 10]),
        ( 9, [6, 8, 9]),
        ( 6, [6]),
        ( 5, [4, 5]),
        ( 2, [2]),
    ];

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

    private async Task SimulateProgressAsync(string jobId, int stepIndex, CancellationToken ct)
    {
        var rng = new Random();
        int pct = 10;
        while (!ct.IsCancellationRequested && pct < 85)
        {
            try { await Task.Delay(rng.Next(2000, 5000), ct); }
            catch (OperationCanceledException) { break; }

            pct = Math.Min(85, pct + rng.Next(5, 16));
            await progressNotifier.NotifyAsync(jobId,
                Notify("Running", $"Processing step {stepIndex}…", pct));
        }
    }

    private static ProgressMessage Notify(string stage, string message, int percent, bool isError = false) =>
        new() { Stage = stage, Message = message, Percent = percent, IsError = isError };
}
