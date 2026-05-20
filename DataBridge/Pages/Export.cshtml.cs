using DataBridge.Models;
using DataBridge.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Concurrent;

namespace DataBridge.Pages;

public class ExportModel(SqlExportService exportService, IConfiguration config) : PageModel
{
    private static readonly ConcurrentDictionary<string, CancellationTokenSource> _jobs = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostRunAsync([FromBody] ExportRunRequest req)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var cs = config.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(cs))
            return BadRequest("Connection string is not configured. Set ConnectionStrings:Default in appsettings.json.");

        var cts = new CancellationTokenSource();
        _jobs[req.JobId] = cts;

        var exportReq = new ExportRequest
        {
            ConnectionString = cs,
            QueryOrView      = req.QueryOrView,
            IsRawQuery       = req.IsRawQuery,
            OutputFolder     = req.IsDryRun ? Path.GetTempPath() : req.OutputFolder,
            FilePrefix       = req.FilePrefix,
            SheetName        = req.SheetName,
            MaxRowsPerFile   = req.MaxRowsPerFile,
        };

        _ = Task.Run(async () =>
        {
            try   { await exportService.ExportAsync(exportReq, req.JobId, cts.Token); }
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

public class ExportRunRequest
{
    public string JobId          { get; set; } = string.Empty;
    public string QueryOrView    { get; set; } = string.Empty;
    public bool   IsRawQuery     { get; set; }
    public string OutputFolder   { get; set; } = string.Empty;
    public string FilePrefix     { get; set; } = "export";
    public string SheetName      { get; set; } = "Data";
    public int    MaxRowsPerFile { get; set; } = 1_000_000;
    public bool   IsDryRun       { get; set; }
}

public class CancelRequest
{
    public string JobId { get; set; } = string.Empty;
}
