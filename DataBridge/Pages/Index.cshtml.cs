using DataBridge.Models;
using DataBridge.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DataBridge.Pages;

public class IndexModel(MetricsService metricsService) : PageModel
{
    public DashboardMetrics? Metrics { get; private set; }

    public async Task OnGetAsync()
    {
        try { Metrics = await metricsService.GetDashboardAsync(); }
        catch { Metrics = null; }
    }
}
