using DataBridge.Application.Commands;
using DataBridge.Application.UseCases;
using DataBridge.Domain.Policies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.RegularExpressions;

namespace DataBridge.Api.Pages;

public class ImportModel(
    ImportDataUseCase importUseCase,
    CancelJobUseCase cancelUseCase,
    IConfiguration config) : PageModel
{
    private static readonly Regex _validTable = new(@"^[a-zA-Z0-9_]+$", RegexOptions.Compiled);

    public void OnGet() { }

    public async Task<IActionResult> OnPostRunAsync(
        string jobId, string tableName, string schemaName,
        bool replaceTable, string? connectionString, List<IFormFile> files)
    {
        if (string.IsNullOrWhiteSpace(jobId) || files.Count == 0)
            return BadRequest("Missing jobId or files.");

        if (User.IsInRole("Admin"))
        {
            if (!_validTable.IsMatch(tableName ?? ""))
                return BadRequest("Table name contains invalid characters.");
        }
        else
        {
            if (!TableWhitelistPolicy.ImportTables.Contains(tableName))
                return BadRequest("Invalid table name.");
        }

        var cs = (User.IsInRole("Admin") && !string.IsNullOrWhiteSpace(connectionString))
            ? connectionString
            : config.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(cs))
            return BadRequest("No connection string configured.");

        var snapshots = new List<(string FileName, Stream Stream)>();
        foreach (var f in files)
        {
            var ms = new MemoryStream();
            await f.CopyToAsync(ms);
            ms.Position = 0;
            snapshots.Add((f.FileName, ms));
        }

        var cmd = new ImportCommand
        {
            JobId            = jobId,
            ConnectionString = cs,
            TableName        = tableName!,
            SchemaName       = schemaName ?? "dbo",
            ReplaceTable     = replaceTable,
            Files            = snapshots,
        };

        _ = Task.Run(() => importUseCase.ExecuteAsync(cmd));
        return new JsonResult(new { jobId }) { StatusCode = StatusCodes.Status202Accepted };
    }

    public IActionResult OnPostCancel([FromBody] CancelBody body)
    {
        cancelUseCase.Execute(body.JobId);
        return new OkResult();
    }

    public record CancelBody(string JobId);
}
