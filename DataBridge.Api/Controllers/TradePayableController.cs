using DataBridge.Application.TradePayable.UseCases;
using DataBridge.Application.TradePayable.UseCases.Commands;
using DataBridge.Application.UseCases;
using DataBridge.Domain.TradePayable.MasterTables;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace DataBridge.Api.Controllers;

[ApiController]
[Route("api/v1/tradepayable")]
[Authorize]
public class TradePayableController(
    UploadFAGLL03UseCase       uploadUseCase,
    RunPipelineStepUseCase     runStepUseCase,
    RunFullPipelineUseCase     runFullUseCase,
    DownloadStepReportUseCase  downloadUseCase,
    GetPipelineRunsUseCase     getRunsUseCase,
    GetPipelineStateUseCase    getStateUseCase,
    MasterTableUseCase         masterUseCase,
    CancelJobUseCase           cancelUseCase) : ControllerBase
{
    // ── Upload ─────────────────────────────────────────────────────────────────

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(
        [FromForm] string jobId,
        [FromForm] string quarterDate,
        [FromForm] string revisionNumber,
        [FromForm] IFormFileCollection files)
    {
        if (string.IsNullOrWhiteSpace(jobId) || files is null || files.Count == 0)
            return BadRequest("Missing jobId or files.");
        if (!DateTime.TryParse(quarterDate, out var qd))
            return BadRequest("Invalid quarterDate.");
        if (string.IsNullOrWhiteSpace(revisionNumber))
            revisionNumber = "R01";

        // Buffer all files to memory before the request scope ends.
        var fileList = new List<(string FileName, Stream FileStream)>(files.Count);
        foreach (var file in files)
        {
            var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;
            fileList.Add((file.FileName, ms));
        }

        var cmd = new UploadFAGLL03Command
        {
            JobId          = jobId,
            QuarterDate    = qd,
            RevisionNumber = revisionNumber,
            Files          = fileList,
            UserName       = User.Identity?.Name,
        };

        _ = Task.Run(() => uploadUseCase.ExecuteAsync(cmd));
        return Accepted(new { jobId });
    }

    // ── Run Step ───────────────────────────────────────────────────────────────

    [HttpPost("run-step")]
    public IActionResult RunStep([FromBody] RunStepRequest req)
    {
        if (req.RunId == Guid.Empty || string.IsNullOrWhiteSpace(req.JobId))
            return BadRequest("Missing runId or jobId.");

        var cmd = new RunPipelineStepCommand
        {
            RunId           = req.RunId,
            TargetStepIndex = req.StepIndex,
            JobId           = req.JobId,
        };

        _ = Task.Run(() => runStepUseCase.ExecuteAsync(cmd));
        return Accepted(new { jobId = req.JobId });
    }

    // ── Run Full Pipeline ──────────────────────────────────────────────────────

    [HttpPost("run-pipeline")]
    public IActionResult RunPipeline([FromBody] RunPipelineRequest req)
    {
        if (req.RunId == Guid.Empty || string.IsNullOrWhiteSpace(req.JobId))
            return BadRequest("Missing runId or jobId.");

        var cmd = new RunFullPipelineCommand { RunId = req.RunId, JobId = req.JobId };
        _ = Task.Run(() => runFullUseCase.ExecuteAsync(cmd));
        return Accepted(new { jobId = req.JobId });
    }

    // ── Cancel ─────────────────────────────────────────────────────────────────

    [HttpPost("cancel")]
    public IActionResult Cancel([FromBody] CancelBody body)
    {
        cancelUseCase.Execute(body.JobId);
        return Ok();
    }

    // ── Download step result Excel ─────────────────────────────────────────────

    [HttpGet("download")]
    public async Task<IActionResult> Download(
        [FromQuery] Guid runId, [FromQuery] int step, [FromQuery] bool force = false)
    {
        if (runId == Guid.Empty) return BadRequest("Missing runId.");
        try
        {
            var filePath = await downloadUseCase.ExecuteAsync(runId, step, force);
            var bytes    = await System.IO.File.ReadAllBytesAsync(filePath);
            var fileName = Path.GetFileName(filePath);
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }
    }

    // ── Pipeline state ─────────────────────────────────────────────────────────

    [HttpGet("state")]
    public async Task<IActionResult> State([FromQuery] Guid runId)
    {
        if (runId == Guid.Empty) return BadRequest("Missing runId.");
        var state = await getStateUseCase.ExecuteAsync(runId);
        if (state is null) return NotFound();
        return Ok(state);
    }

    // ── Pipeline runs list ─────────────────────────────────────────────────────

    [HttpGet("runs")]
    public async Task<IActionResult> Runs()
    {
        var runs = await getRunsUseCase.ExecuteAsync();
        return Ok(runs);
    }

    // ── Master table CRUD ──────────────────────────────────────────────────────

    [HttpGet("masters/{tableName}")]
    public async Task<IActionResult> GetMasterRows(string tableName) =>
        tableName.ToLowerInvariant() switch
        {
            "advance-gls"    => Ok(await masterUseCase.GetAdvanceGLsAsync()),
            "liability-gls"  => Ok(await masterUseCase.GetLiabilityGLsAsync()),
            "not-due-gls"    => Ok(await masterUseCase.GetNotDueGLsAsync()),
            "msme-codes"     => Ok(await masterUseCase.GetMSMECodesAsync()),
            "capital-gls"    => Ok(await masterUseCase.GetCapitalGLsAsync()),
            "insurance-gls"  => Ok(await masterUseCase.GetInsuranceGLsAsync()),
            "non-msme-gls"   => Ok(await masterUseCase.GetNonMSMEGLsAsync()),
            "unclaimed-gls"  => Ok(await masterUseCase.GetUnclaimedGLsAsync()),
            "gl-hyperion"    => Ok(await masterUseCase.GetGLHyperionMapAsync()),
            "icp-hyperion"   => Ok(await masterUseCase.GetICPHyperionMapAsync()),
            "icp-vendor"     => Ok(await masterUseCase.GetICPVendorMapAsync()),
            "forex"          => Ok(await masterUseCase.GetForexMonthEndMapAsync()),
            "ageing-groups"  => Ok(await masterUseCase.GetAgeingGroupsAsync()),
            _                => BadRequest("Unknown table."),
        };

    [HttpPost("masters/{tableName}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpsertMasterRow(string tableName, [FromBody] JsonElement body)
    {
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        try
        {
            switch (tableName.ToLowerInvariant())
            {
                case "advance-gls":   await masterUseCase.UpsertAdvanceGLAsync(body.Deserialize<AdvanceGLs>(opts)!);         break;
                case "liability-gls": await masterUseCase.UpsertLiabilityGLAsync(body.Deserialize<LiabilityGLs>(opts)!);     break;
                case "not-due-gls":   await masterUseCase.UpsertNotDueGLAsync(body.Deserialize<NotDueGLs>(opts)!);           break;
                case "msme-codes":    await masterUseCase.UpsertMSMECodeAsync(body.Deserialize<MSMECompanyCodes>(opts)!);     break;
                case "capital-gls":   await masterUseCase.UpsertCapitalGLAsync(body.Deserialize<CapitalCreditorGLs>(opts)!); break;
                case "insurance-gls": await masterUseCase.UpsertInsuranceGLAsync(body.Deserialize<InsuranceGLs>(opts)!);     break;
                case "non-msme-gls":  await masterUseCase.UpsertNonMSMEGLAsync(body.Deserialize<NonMSMEGLs>(opts)!);         break;
                case "unclaimed-gls": await masterUseCase.UpsertUnclaimedGLAsync(body.Deserialize<UnclaimedGLs>(opts)!);     break;
                case "gl-hyperion":   await masterUseCase.UpsertGLHyperionMapAsync(body.Deserialize<GLHyperionMap>(opts)!);  break;
                case "icp-hyperion":  await masterUseCase.UpsertICPHyperionMapAsync(body.Deserialize<ICPHyperionMap>(opts)!);break;
                case "icp-vendor":    await masterUseCase.UpsertICPVendorMapAsync(body.Deserialize<ICPVendorMap>(opts)!);    break;
                case "forex":         await masterUseCase.UpsertForexMonthEndMapAsync(body.Deserialize<ForexMonthEndMap>(opts)!); break;
                case "ageing-groups": await masterUseCase.UpsertAgeingGroupAsync(body.Deserialize<AgeingGroup>(opts)!);      break;
                default: return BadRequest("Unknown table.");
            }
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("masters/{tableName}/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteMasterRow(string tableName, int id)
    {
        try
        {
            switch (tableName.ToLowerInvariant())
            {
                case "advance-gls":   await masterUseCase.DeleteAdvanceGLAsync(id);   break;
                case "liability-gls": await masterUseCase.DeleteLiabilityGLAsync(id); break;
                case "not-due-gls":   await masterUseCase.DeleteNotDueGLAsync(id);    break;
                case "msme-codes":    await masterUseCase.DeleteMSMECodeAsync(id);    break;
                case "capital-gls":   await masterUseCase.DeleteCapitalGLAsync(id);   break;
                case "insurance-gls": await masterUseCase.DeleteInsuranceGLAsync(id); break;
                case "non-msme-gls":  await masterUseCase.DeleteNonMSMEGLAsync(id);   break;
                case "unclaimed-gls": await masterUseCase.DeleteUnclaimedGLAsync(id); break;
                case "gl-hyperion":   await masterUseCase.DeleteGLHyperionMapAsync(id); break;
                case "icp-hyperion":  await masterUseCase.DeleteICPHyperionMapAsync(id); break;
                case "icp-vendor":    await masterUseCase.DeleteICPVendorMapAsync(id); break;
                case "forex":         await masterUseCase.DeleteForexMonthEndMapAsync(id); break;
                case "ageing-groups": await masterUseCase.DeleteAgeingGroupAsync(id); break;
                default: return BadRequest("Unknown table.");
            }
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // ── Request / response types ───────────────────────────────────────────────

    public record RunStepRequest(Guid RunId, int StepIndex, string JobId);
    public record RunPipelineRequest(Guid RunId, string JobId);
    public record CancelBody(string JobId);
}
