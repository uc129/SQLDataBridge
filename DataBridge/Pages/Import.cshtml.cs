using DataBridge.Models;
using DataBridge.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Concurrent;

namespace DataBridge.Pages;

public class ImportModel(ExcelImportService importService, IConfiguration config) : PageModel
{
    private static readonly ConcurrentDictionary<string, CancellationTokenSource> _jobs = new();

    private static readonly HashSet<string> _allowedTables = new(StringComparer.OrdinalIgnoreCase)
        { "FNATool_DataBridge1", "FNATool_DataBridge2", "FNATool_DataBridge3" };

    public void OnGet() { }

    public async Task<IActionResult> OnPostRunAsync(
        [FromForm] string jobId,
        [FromForm] string tableName,
        [FromForm] string schemaName,
        [FromForm] bool replaceTable,
        [FromForm] List<IFormFile> files)
    {
        if (string.IsNullOrWhiteSpace(jobId) || files.Count == 0)
            return BadRequest("Missing jobId or files.");

        if (!_allowedTables.Contains(tableName))
            return BadRequest("Invalid table name.");

        var cs = config.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(cs))
            return BadRequest("Connection string is not configured. Set ConnectionStrings:Default in appsettings.json.");

        var cts = new CancellationTokenSource();
        _jobs[jobId] = cts;

        var req = new ImportRequest
        {
            ConnectionString = cs,
            TableName        = tableName,
            SchemaName       = schemaName ?? "dbo",
            ReplaceTable     = replaceTable,
        };

        var fileSnapshots = new List<(string FileName, Stream Stream)>();
        foreach (var f in files)
        {
            var ms = new MemoryStream();
            await f.CopyToAsync(ms);
            ms.Position = 0;
            fileSnapshots.Add((f.FileName, ms));
        }

        _ = Task.Run(async () =>
        {
            try { await importService.ImportAsync(req, fileSnapshots, jobId, cts.Token); }
            finally { _jobs.TryRemove(jobId, out _); }
        }, cts.Token);

        return new OkResult();
    }

    public IActionResult OnPostCancel([FromBody] CancelRequest req)
    {
        if (_jobs.TryRemove(req.JobId, out var cts))
            cts.Cancel();
        return new OkResult();
    }
}
