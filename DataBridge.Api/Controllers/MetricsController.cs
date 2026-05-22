using DataBridge.Application.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataBridge.Api.Controllers;

[ApiController]
[Route("api/v1/metrics")]
[Authorize]
public class MetricsController(GetDashboardUseCase dashboardUseCase) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        try
        {
            var metrics = await dashboardUseCase.ExecuteAsync();
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            return Ok(new { error = ex.Message });
        }
    }
}
