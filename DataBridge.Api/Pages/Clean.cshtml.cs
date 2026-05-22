using DataBridge.Application.Commands;
using DataBridge.Application.UseCases;
using DataBridge.Domain.Policies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace DataBridge.Api.Pages;

public class CleanModel(
    CleanDataUseCase cleanUseCase,
    ImportAndCleanUseCase importAndCleanUseCase,
    GetTableInfoUseCase tableInfoUseCase,
    CancelJobUseCase cancelUseCase,
    IConfiguration config) : PageModel
{
    public void OnGet() { }

    public async Task<IActionResult> OnGetTableInfoAsync(string tableName)
    {
        if (!TableWhitelistPolicy.CleanTables.Contains(tableName))
            return BadRequest("Invalid table name.");

        var (metrics, columns, columnMap) = await tableInfoUseCase.ExecuteAsync(tableName);
        return new JsonResult(new { exists = metrics.Exists, rowCount = metrics.RowCount, columns, mapping = columnMap });
    }

    public IActionResult OnPostRun([FromBody] CleanRunRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.JobId) || string.IsNullOrWhiteSpace(req.TableName))
            return BadRequest("Missing jobId or tableName.");

        if (!TableWhitelistPolicy.CleanTables.Contains(req.TableName))
            return BadRequest("Invalid table name.");

        var cmd = new CleanCommand
        {
            JobId           = req.JobId,
            TableName       = req.TableName,
            ColumnMap       = req.ColumnMap ?? [],
            PoLeadingDigits = req.PoLeadingDigits ?? [7, 8, 3],
        };

        _ = Task.Run(() => cleanUseCase.ExecuteAsync(cmd));
        return new JsonResult(new { jobId = req.JobId }) { StatusCode = StatusCodes.Status202Accepted };
    }

    public async Task<IActionResult> OnPostImportAndRunAsync(
        string jobId, string tableName, string schemaName,
        bool replaceTable, string? connectionString,
        string? columnMapJson, string? poDigitsJson,
        List<IFormFile> files)
    {
        if (string.IsNullOrWhiteSpace(jobId) || files.Count == 0)
            return BadRequest("Missing jobId or files.");

        if (!TableWhitelistPolicy.CleanTables.Contains(tableName))
            return BadRequest("Invalid table name.");

        var cs = (User.IsInRole("Admin") && !string.IsNullOrWhiteSpace(connectionString))
            ? connectionString
            : config.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(cs))
            return BadRequest("No connection string configured.");

        Dictionary<string, string?> colMap = [];
        int[] poDigits = [7, 8, 3];
        try
        {
            if (!string.IsNullOrWhiteSpace(columnMapJson))
                colMap = JsonSerializer.Deserialize<Dictionary<string, string?>>(columnMapJson) ?? [];
            if (!string.IsNullOrWhiteSpace(poDigitsJson))
                poDigits = JsonSerializer.Deserialize<int[]>(poDigitsJson) ?? [7, 8, 3];
        }
        catch { /* use defaults */ }

        var snapshots = new List<(string FileName, Stream Stream)>();
        foreach (var f in files)
        {
            var ms = new MemoryStream();
            await f.CopyToAsync(ms);
            ms.Position = 0;
            snapshots.Add((f.FileName, ms));
        }

        var cmd = new ImportAndCleanCommand
        {
            JobId            = jobId,
            ConnectionString = cs,
            TableName        = tableName,
            SchemaName       = schemaName ?? "dbo",
            ReplaceTable     = replaceTable,
            ColumnMap        = colMap,
            PoLeadingDigits  = poDigits,
            Files            = snapshots,
        };

        _ = Task.Run(() => importAndCleanUseCase.ExecuteAsync(cmd));
        return new JsonResult(new { jobId }) { StatusCode = StatusCodes.Status202Accepted };
    }

    public IActionResult OnPostCancel([FromBody] CancelBody body)
    {
        cancelUseCase.Execute(body.JobId);
        return new OkResult();
    }

    public record CleanRunRequest(
        string JobId, string TableName,
        Dictionary<string, string?>? ColumnMap,
        int[]? PoLeadingDigits);

    public record CancelBody(string JobId);
}
