using DataBridge.Application.UseCases;
using DataBridge.Domain.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DataBridge.Api.Pages;

public class IndexModel(GetDashboardUseCase dashboardUseCase) : PageModel
{
    public DashboardMetrics? Metrics { get; private set; }

    public async Task OnGetAsync()
    {
        try { Metrics = await dashboardUseCase.ExecuteAsync(); }
        catch { /* Metrics stays null; view shows warning */ }
    }
}
