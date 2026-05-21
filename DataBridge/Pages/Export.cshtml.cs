using DataBridge.Models;
using DataBridge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Collections.Concurrent;

namespace DataBridge.Pages;

[Authorize]
public class ExportModel(SqlExportService exportService, IConfiguration config) : PageModel
{
    private static readonly ConcurrentDictionary<string, CancellationTokenSource> _jobs = new();

    internal static readonly HashSet<string> AllowedTargets = new(StringComparer.OrdinalIgnoreCase)
    {
        "FNATool_QuickImport_1",                   "FNATool_QuickImport_2",                   "FNATool_QuickImport_3",
        "FNATool_VendorDetailsPipeline_1",         "FNATool_VendorDetailsPipeline_2",         "FNATool_VendorDetailsPipeline_3",
        "View_FNATool_VendorDetailsPipeline_1",    "View_FNATool_VendorDetailsPipeline_2",    "View_FNATool_VendorDetailsPipeline_3",
    };

    public void OnGet() { }

    public async Task<IActionResult> OnGetAvailableTargetsAsync()
    {
        var cs = config.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(cs))
            return new JsonResult(Array.Empty<string>());

        var inList = string.Join(",", AllowedTargets.Select(n => $"N'{n}'"));
        var sql = $@"
            SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_NAME IN ({inList})
            UNION
            SELECT TABLE_NAME FROM INFORMATION_SCHEMA.VIEWS  WHERE TABLE_NAME IN ({inList})";

        var found = new List<string>();
        await using var conn = new SqlConnection(cs);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            found.Add(reader.GetString(0));

        return new JsonResult(found);
    }

    public async Task<IActionResult> OnPostRunAsync([FromBody] ExportRunRequest req)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var cs = (User.IsInRole("Admin") && !string.IsNullOrWhiteSpace(req.ConnectionString))
            ? req.ConnectionString
            : config.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(cs))
            return BadRequest("Connection string is not configured. Set ConnectionStrings:Default in appsettings.json.");

        if (!User.IsInRole("Admin"))
        {
            if (req.IsRawQuery || !AllowedTargets.Contains(req.QueryOrView))
                return Forbid();
        }

        var cts = new CancellationTokenSource();
        _jobs[req.JobId] = cts;

        var exportReq = new ExportRequest
        {
            ConnectionString = cs,
            QueryOrView      = req.QueryOrView,
            IsRawQuery       = req.IsRawQuery,
            OutputFolder     = Path.Combine(Path.GetTempPath(), "DataBridge", req.JobId),
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

    public IActionResult OnGetDownload(string jobId, string file)
    {
        var safeName = Path.GetFileName(file);
        if (string.IsNullOrEmpty(safeName) || safeName != file)
            return BadRequest();

        var tempBase = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "DataBridge"));
        var filePath = Path.GetFullPath(Path.Combine(tempBase, jobId, safeName));
        if (!filePath.StartsWith(tempBase, StringComparison.OrdinalIgnoreCase))
            return BadRequest();

        if (!System.IO.File.Exists(filePath))
            return NotFound();

        var bytes = System.IO.File.ReadAllBytes(filePath);

        try { System.IO.File.Delete(filePath); } catch { /* best-effort */ }
        try
        {
            var dir = Path.GetDirectoryName(filePath)!;
            if (!Directory.EnumerateFiles(dir).Any()) Directory.Delete(dir);
        }
        catch { /* best-effort */ }

        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            safeName);
    }
}

public class ExportRunRequest
{
    public string JobId            { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public string QueryOrView      { get; set; } = string.Empty;
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
