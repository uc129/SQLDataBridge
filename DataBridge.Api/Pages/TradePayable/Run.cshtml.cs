using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DataBridge.Api.Pages.TradePayable;

[Authorize]
public class TradePayableRunModel : PageModel
{
    public void OnGet() { }
}
