using DataBridge.Api.Requests;
using DataBridge.Application.Commands;
using DataBridge.Application.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace DataBridge.Api.Controllers;

[ApiController]
[Route("api/v1/import")]
[Authorize]
public class ImportController(
    ImportDataUseCase importUseCase,
    CancelJobUseCase cancelUseCase,
    IConfiguration config) : ControllerBase
{
    private static readonly Regex _validTableName = new(@"^[a-zA-Z0-9_]+$", RegexOptions.Compiled);

    [HttpPost("run")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Run([FromForm] ImportHttpRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.JobId) || req.Files.Count == 0)
            return BadRequest("Missing jobId or files.");

        if (User.IsInRole("Admin"))
        {
            if (!_validTableName.IsMatch(req.TableName ?? ""))
                return BadRequest("Table name contains invalid characters.");
        }
        else
        {
            if (!TableWhitelistPolicy.ImportTables.Contains(req.TableName))
                return BadRequest("Invalid table name.");
        }

        var cs = (User.IsInRole("Admin") && !string.IsNullOrWhiteSpace(req.ConnectionString))
            ? req.ConnectionString
            : config.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(cs))
            return BadRequest("Connection string is not configured.");

        var fileSnapshots = new List<(string FileName, Stream Stream)>();
        foreach (var f in req.Files)
        {
            var ms = new MemoryStream();
            await f.CopyToAsync(ms);
            ms.Position = 0;
            fileSnapshots.Add((f.FileName, ms));
        }

        var cmd = new ImportCommand
        {
            JobId            = req.JobId,
            ConnectionString = cs,
            TableName        = req.TableName!,
            SchemaName       = req.SchemaName,
            ReplaceTable     = req.ReplaceTable,
            Files            = fileSnapshots,
        };

        _ = Task.Run(() => importUseCase.ExecuteAsync(cmd));
        return Accepted(new { jobId = req.JobId });
    }

    [HttpPost("cancel")]
    public IActionResult Cancel([FromBody] CancelBody body)
    {
        cancelUseCase.Execute(body.JobId);
        return Ok();
    }
}
