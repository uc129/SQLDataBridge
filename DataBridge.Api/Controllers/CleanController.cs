using DataBridge.Api.Requests;
using DataBridge.Application.Commands;
using DataBridge.Application.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace DataBridge.Api.Controllers;

[ApiController]
[Route("api/v1/clean")]
[Authorize]
public class CleanController(
    CleanDataUseCase cleanUseCase,
    ImportAndCleanUseCase importAndCleanUseCase,
    GetTableInfoUseCase tableInfoUseCase,
    CancelJobUseCase cancelUseCase,
    IConfiguration config) : ControllerBase
{
    [HttpGet("table-info")]
    public async Task<IActionResult> TableInfo([FromQuery] string tableName)
    {
        if (!TableWhitelistPolicy.CleanTables.Contains(tableName))
            return BadRequest("Invalid table name.");

        var (metrics, columns, mapping) = await tableInfoUseCase.ExecuteAsync(tableName);
        return Ok(new
        {
            exists         = metrics.Exists,
            rowCount       = metrics.RowCount,
            vendorNotFound = metrics.VendorNotFound,
            poNotFound     = metrics.PoNotFound,
            columns,
            mapping,
        });
    }

    [HttpPost("import-and-run")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportAndRun([FromForm] CleanImportAndRunHttpRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.JobId) || req.Files.Count == 0)
            return BadRequest("Missing jobId or files.");

        if (!TableWhitelistPolicy.CleanTables.Contains(req.TableName))
            return BadRequest("Invalid table name.");

        var cs = (User.IsInRole("Admin") && !string.IsNullOrWhiteSpace(req.ConnectionString))
            ? req.ConnectionString
            : config.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(cs))
            return BadRequest("Connection string is not configured.");

        var columnMap = string.IsNullOrWhiteSpace(req.ColumnMapJson)
            ? null
            : JsonSerializer.Deserialize<Dictionary<string, string?>>(req.ColumnMapJson);

        var poDigits = string.IsNullOrWhiteSpace(req.PoDigitsJson)
            ? new[] { 7, 8, 3 }
            : JsonSerializer.Deserialize<int[]>(req.PoDigitsJson) ?? new[] { 7, 8, 3 };

        var fileSnapshots = new List<(string FileName, Stream Stream)>();
        foreach (var f in req.Files)
        {
            var ms = new MemoryStream();
            await f.CopyToAsync(ms);
            ms.Position = 0;
            fileSnapshots.Add((f.FileName, ms));
        }

        var cmd = new ImportAndCleanCommand
        {
            JobId            = req.JobId,
            ConnectionString = cs,
            TableName        = req.TableName,
            SchemaName       = req.SchemaName,
            ReplaceTable     = req.ReplaceTable,
            ColumnMap        = columnMap,
            PoLeadingDigits  = poDigits,
            Files            = fileSnapshots,
        };

        _ = Task.Run(() => importAndCleanUseCase.ExecuteAsync(cmd));
        return Accepted(new { jobId = req.JobId });
    }

    [HttpPost("run")]
    public IActionResult Run([FromBody] CleanRunHttpRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.JobId))
            return BadRequest("Missing jobId.");

        if (!TableWhitelistPolicy.CleanTables.Contains(req.TableName))
            return BadRequest("Invalid table name.");

        var cmd = new CleanCommand
        {
            JobId           = req.JobId,
            TableName       = req.TableName,
            ColumnMap       = req.ColumnMap,
            PoLeadingDigits = req.PoLeadingDigits,
        };

        _ = Task.Run(() => cleanUseCase.ExecuteAsync(cmd));
        return Accepted(new { jobId = req.JobId });
    }

    [HttpPost("cancel")]
    public IActionResult Cancel([FromBody] CancelBody body)
    {
        cancelUseCase.Execute(body.JobId);
        return Ok();
    }
}
