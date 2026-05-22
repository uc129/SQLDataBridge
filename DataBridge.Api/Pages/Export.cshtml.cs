using DataBridge.Application.Commands;
using DataBridge.Application.UseCases;
using DataBridge.Domain.Policies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DataBridge.Api.Pages;

public class ExportModel(
    ExportDataUseCase exportUseCase,
    GetAvailableExportTargetsUseCase targetsUseCase,
    CancelJobUseCase cancelUseCase,
    IConfiguration config) : PageModel
{
    public IReadOnlyList<string> AvailableTargets { get; private set; } = [];

    public async Task OnGetAsync()
    {
        try { AvailableTargets = await targetsUseCase.ExecuteAsync(); }
        catch { AvailableTargets = TableWhitelistPolicy.ExportTargets.OrderBy(x => x).ToList(); }
    }

    public IActionResult OnPostRun([FromBody] ExportRunRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.JobId) || string.IsNullOrWhiteSpace(req.QueryOrView))
            return BadRequest("Missing jobId or query.");

        if (User.IsInRole("Admin"))
        {
            if (!req.IsRawQuery && !TableWhitelistPolicy.ExportTargets.Contains(req.QueryOrView))
                return BadRequest("Invalid export target.");
        }
        else
        {
            if (!TableWhitelistPolicy.ExportTargets.Contains(req.QueryOrView))
                return BadRequest("Invalid export target.");
        }

        var cs = (User.IsInRole("Admin") && !string.IsNullOrWhiteSpace(req.ConnectionString))
            ? req.ConnectionString
            : config.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(cs))
            return BadRequest("No connection string configured.");

        var cmd = new ExportCommand
        {
            JobId            = req.JobId,
            ConnectionString = cs,
            QueryOrView      = req.QueryOrView,
            IsRawQuery       = req.IsRawQuery,
            OutputFolder     = Path.Combine(Path.GetTempPath(), "DataBridge", req.JobId),
            FilePrefix       = req.FilePrefix ?? "export",
            SheetName        = req.SheetName ?? "Data",
            MaxRowsPerFile   = req.MaxRowsPerFile > 0 ? req.MaxRowsPerFile : 1_000_000,
        };

        _ = Task.Run(() => exportUseCase.ExecuteAsync(cmd));
        return new JsonResult(new { jobId = req.JobId }) { StatusCode = StatusCodes.Status202Accepted };
    }

    public IActionResult OnPostCancel([FromBody] CancelBody body)
    {
        cancelUseCase.Execute(body.JobId);
        return new OkResult();
    }

    public record ExportRunRequest(
        string JobId, string QueryOrView, bool IsRawQuery,
        string? ConnectionString, string? FilePrefix, string? SheetName, int MaxRowsPerFile);

    public record CancelBody(string JobId);
}
