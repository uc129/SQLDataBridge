using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DataBridge.Api.Pages;

[AllowAnonymous]
public class AccessDeniedModel : PageModel { }
