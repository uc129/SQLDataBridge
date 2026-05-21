using DataBridge.Models;
using DataBridge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Concurrent;
using System.Text.Json;

namespace DataBridge.Pages;

[Authorize]
public class CleanModel(CleanService cleanService, MetricsService metricsService, IConfiguration config) : PageModel
{
    private static readonly ConcurrentDictionary<string, CancellationTokenSource> _jobs = new();

    public void OnGet() { }

    public async Task<IActionResult> OnGetTableInfoAsync([FromQuery] string tableName)
    {
        if (!CleanService.AllowedTables.Contains(tableName, StringComparer.OrdinalIgnoreCase))
            return BadRequest("Invalid table name.");

        var (metrics, columns, mapping) = await metricsService.GetTableInfoAsync(tableName);
        return new JsonResult(new
        {
            exists         = metrics.Exists,
            rowCount       = metrics.RowCount,
            vendorNotFound = metrics.VendorNotFound,
            poNotFound     = metrics.PoNotFound,
            columns,
            mapping,
        });
    }

    // Combined Import + Clean: files → in-memory DataTable → clean → bulk insert → view
    public async Task<IActionResult> OnPostImportAndRunAsync(
        [FromForm] string jobId,
        [FromForm] string tableName,
        [FromForm] string schemaName,
        [FromForm] bool replaceTable,
        [FromForm] string? connectionString,
        [FromForm] string? columnMapJson,
        [FromForm] string? poDigitsJson,
        [FromForm] List<IFormFile> files)
    {
        if (string.IsNullOrWhiteSpace(jobId) || files.Count == 0)
            return BadRequest("Missing jobId or files.");

        if (!CleanService.AllowedTables.Contains(tableName, StringComparer.OrdinalIgnoreCase))
            return BadRequest("Invalid table name.");

        var cs = (User.IsInRole("Admin") && !string.IsNullOrWhiteSpace(connectionString))
            ? connectionString
            : config.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(cs))
            return BadRequest("Connection string is not configured.");

        var columnMap = string.IsNullOrWhiteSpace(columnMapJson)
            ? null
            : JsonSerializer.Deserialize<Dictionary<string, string?>>(columnMapJson);

        var poDigits = string.IsNullOrWhiteSpace(poDigitsJson)
            ? new[] { 7, 8, 3 }
            : JsonSerializer.Deserialize<int[]>(poDigitsJson) ?? new[] { 7, 8, 3 };

        var importReq = new ImportRequest
        {
            ConnectionString = cs,
            TableName        = tableName,
            SchemaName       = schemaName ?? "dbo",
            ReplaceTable     = replaceTable,
        };

        var cleanReq = new CleanRequest
        {
            JobId            = jobId,
            TableName        = tableName,
            ColumnMap        = columnMap,
            PoLeadingDigits  = poDigits,
        };

        var fileSnapshots = new List<(string FileName, Stream Stream)>();
        foreach (var f in files)
        {
            var ms = new MemoryStream();
            await f.CopyToAsync(ms);
            ms.Position = 0;
            fileSnapshots.Add((f.FileName, ms));
        }

        var cts = new CancellationTokenSource();
        _jobs[jobId] = cts;

        _ = Task.Run(async () =>
        {
            try { await cleanService.RunImportAndCleanAsync(cleanReq, importReq, fileSnapshots, cts.Token); }
            finally { _jobs.TryRemove(jobId, out _); }
        }, cts.Token);

        return new OkResult();
    }

    public async Task<IActionResult> OnPostRunAsync([FromBody] CleanRequest req)
    {
        if (!CleanService.AllowedTables.Contains(req.TableName, StringComparer.OrdinalIgnoreCase))
            return BadRequest("Invalid table name.");

        var cts = new CancellationTokenSource();
        _jobs[req.JobId] = cts;

        _ = Task.Run(async () =>
        {
            try { await cleanService.RunAsync(req, cts.Token); }
            finally { _jobs.TryRemove(req.JobId, out _); }
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
