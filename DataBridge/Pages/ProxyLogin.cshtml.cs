using DataBridge.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace DataBridge.Pages;

[AllowAnonymous]
public class ProxyLoginModel(UserService userService) : PageModel
{
    private const string OtpSecret  = "112151";
    private const string AuthCookie = "DataBridgeAuth";

    public IEnumerable<DataBridgeUser> Users { get; private set; } = [];

    [BindProperty] public int    SelectedUserId { get; set; }
    [BindProperty] public string Otp            { get; set; } = string.Empty;

    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Index");

        Users = await userService.GetActiveUsersAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Otp != OtpSecret)
        {
            ErrorMessage = "Invalid OTP.";
            Users = await userService.GetActiveUsersAsync();
            return Page();
        }

        var user = await userService.GetUserByIdAsync(SelectedUserId);
        if (user is null || !user.IsActive)
        {
            ErrorMessage = "Selected user is not active.";
            Users = await userService.GetActiveUsersAsync();
            return Page();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier,  user.Email),
            new(ClaimTypes.Name,            user.DisplayName),
            new(ClaimTypes.Email,           user.Email),
            new(ClaimTypes.Role,            user.RoleCode),
            new("DataBridgeRole",           user.RoleCode),
            new("DataBridgeDisplayName",    user.DisplayName),
            new("DataBridgePSNO",           user.PSNO),
            new("DataBridgeAuthMethod",     "Proxy"),
        };

        var identity  = new ClaimsIdentity(claims, AuthCookie);
        var principal = new ClaimsPrincipal(identity);
        var props     = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc   = DateTimeOffset.UtcNow.AddHours(8),
        };

        await HttpContext.SignInAsync(AuthCookie, principal, props);

        var returnUrl = Request.Query["ReturnUrl"].FirstOrDefault();
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToPage("/Index");
    }
}
