using DataBridge.Models;
using DataBridge.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Concurrent;

namespace DataBridge.Pages;

public class CleanModel(CleanService cleanService, MetricsService metricsService) : PageModel
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
