using DataBridge.Application.TradePayable.UseCases;
using DataBridge.Domain.TradePayable.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DataBridge.Api.Pages.TradePayable;

[Authorize]
public class TradePayableIndexModel(GetPipelineRunsUseCase getRunsUseCase) : PageModel
{
    public IEnumerable<PipelineRun> Runs { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Runs = await getRunsUseCase.ExecuteAsync();
    }
}
