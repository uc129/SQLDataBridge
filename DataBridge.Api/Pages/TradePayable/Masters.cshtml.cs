using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DataBridge.Api.Pages.TradePayable;

[Authorize]
public class TradePayableMastersModel : PageModel
{
    public void OnGet() { }
}
