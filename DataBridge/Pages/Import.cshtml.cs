using DataBridge.Models;
using DataBridge.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Concurrent;

namespace DataBridge.Pages;

public class ImportModel(ExcelImportService importService) : PageModel
{
    private static readonly ConcurrentDictionary<string, CancellationTokenSource> _jobs = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostRunAsync(
        [FromForm] string jobId,
        [FromForm] string connectionString,
        [FromForm] string tableName,
        [FromForm] string schemaName,
        [FromForm] bool replaceTable,
        [FromForm] List<IFormFile> files)
    {
        if (string.IsNullOrWhiteSpace(jobId) || files.Count == 0)
            return BadRequest("Missing jobId or files.");

        var cts = new CancellationTokenSource();
        _jobs[jobId] = cts;

        var req = new ImportRequest
        {
            ConnectionString = connectionString,
            TableName = tableName,
            SchemaName = schemaName,
            ReplaceTable = replaceTable,
        };

        // Snapshot file streams before the request ends
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

