using DataBridge.Api.Requests;
using DataBridge.Application.Commands;
using DataBridge.Application.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataBridge.Api.Controllers;

[ApiController]
[Route("api/v1/export")]
[Authorize]
public class ExportController(
    ExportDataUseCase exportUseCase,
    GetAvailableExportTargetsUseCase targetsUseCase,
    CancelJobUseCase cancelUseCase,
    IConfiguration config) : ControllerBase
{
    [HttpGet("targets")]
    public async Task<IActionResult> Targets()
    {
        var targets = await targetsUseCase.ExecuteAsync();
        return Ok(targets);
    }

    [HttpPost("run")]
    public IActionResult Run([FromBody] ExportRunHttpRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.JobId) || string.IsNullOrWhiteSpace(req.QueryOrView))
            return BadRequest("Missing jobId or queryOrView.");

        var cs = (User.IsInRole("Admin") && !string.IsNullOrWhiteSpace(req.ConnectionString))
            ? req.ConnectionString
            : config.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(cs))
            return BadRequest("Connection string is not configured.");

        if (!User.IsInRole("Admin"))
        {
            if (req.IsRawQuery || !TableWhitelistPolicy.ExportTargets.Contains(req.QueryOrView))
                return Forbid();
        }

        var cmd = new ExportCommand
        {
            JobId            = req.JobId,
            ConnectionString = cs,
            QueryOrView      = req.QueryOrView,
            IsRawQuery       = req.IsRawQuery,
            OutputFolder     = Path.Combine(Path.GetTempPath(), "DataBridge", req.JobId),
            FilePrefix       = req.FilePrefix,
            SheetName        = req.SheetName,
            MaxRowsPerFile   = req.MaxRowsPerFile,
        };

        _ = Task.Run(() => exportUseCase.ExecuteAsync(cmd));
        return Accepted(new { jobId = req.JobId });
    }

    [HttpPost("cancel")]
    public IActionResult Cancel([FromBody] CancelBody body)
    {
        cancelUseCase.Execute(body.JobId);
        return Ok();
    }

    [HttpGet("download")]
    public IActionResult Download([FromQuery] string jobId, [FromQuery] string file)
    {
        var safeName = Path.GetFileName(file);
        if (string.IsNullOrEmpty(safeName) || safeName != file)
            return BadRequest();

        var tempBase = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "DataBridge"));
        var filePath = Path.GetFullPath(Path.Combine(tempBase, jobId, safeName));
        if (!filePath.StartsWith(tempBase, StringComparison.OrdinalIgnoreCase))
            return BadRequest();

        if (!System.IO.File.Exists(filePath))
            return NotFound();

        var bytes = System.IO.File.ReadAllBytes(filePath);

        try { System.IO.File.Delete(filePath); } catch { /* best-effort */ }
        try
        {
            var dir = Path.GetDirectoryName(filePath)!;
            if (!Directory.EnumerateFiles(dir).Any()) Directory.Delete(dir);
        }
        catch { /* best-effort */ }

        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            safeName);
    }
}
