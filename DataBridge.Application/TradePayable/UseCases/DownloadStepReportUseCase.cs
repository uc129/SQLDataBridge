using DataBridge.Application.Interfaces;
using DataBridge.Domain.TradePayable.Contracts;
using System.Data;

namespace DataBridge.Application.TradePayable.UseCases;

public class DownloadStepReportUseCase(
    IStepResultRepository stepResultRepo,
    IExcelWriter          excelWriter)
{
    /// <summary>
    /// Returns the path to a temp Excel file for the given run/step.
    /// Resolution order:
    ///   1. A temp file written during the pipeline run (covers all in-memory steps).
    ///   2. The DB step result (Step02 checkpoint, or results from pre-migration runs).
    /// Throws if neither source has data.
    /// </summary>
    public async Task<string> ExecuteAsync(Guid runId, int stepIndex, bool force = false)
    {
        // 1. Check for an already-written temp Excel (fastest path, covers all steps).
        if (!force)
        {
            var tempPath = TempExcelPath(runId, stepIndex);
            if (File.Exists(tempPath))
                return tempPath;
        }

        // 2. Fall back to DB (Step02 checkpoint, or older runs where all steps were persisted).
        var dt = await stepResultRepo.RetrieveStepResultAsync(runId, stepIndex);
        if (dt.Rows.Count == 0)
            throw new FileNotFoundException(
                $"No result found for step {stepIndex}. " +
                "Run the pipeline step first, or re-run the full pipeline to regenerate the file.");

        return WriteTempExcel(dt, runId, stepIndex);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private string WriteTempExcel(DataTable dt, Guid runId, int stepIndex)
    {
        var cols   = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
        var rows   = dt.Rows.Cast<DataRow>()
                      .Select(r => cols.Select(c => r[c] == DBNull.Value ? null : r[c]).ToArray())
                      .Cast<object?[]>().ToList();

        var folder = RunFullPipelineUseCase.StepTempFolder(runId);
        Directory.CreateDirectory(folder);

        var stepName = stepIndex switch
        {
            31  => "Step03_AllGrouped",
            71  => "Step07_AllGrouped",
            var n => $"Step{n:D2}",
        };

        return excelWriter.WritePartFile(cols, rows, stepName, $"Step {stepIndex}", folder, 1);
    }

    private static string TempExcelPath(Guid runId, int stepIndex)
    {
        var folder   = RunFullPipelineUseCase.StepTempFolder(runId);
        var stepName = stepIndex switch
        {
            31  => "Step03_AllGrouped",
            71  => "Step07_AllGrouped",
            var n => $"Step{n:D2}",
        };
        return Path.Combine(folder, $"{stepName}_part01.xlsx");
    }
}
