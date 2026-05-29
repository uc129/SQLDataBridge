using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace DataBridge.Api.Pages;

[AllowAnonymous]
public class LogoutModel : PageModel
{
    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated != true)
            return Redirect("/Login");

        var authMethod = User.FindFirstValue("DataBridgeAuthMethod") ?? "";

        if (authMethod == "Proxy")
        {
            return SignOut(
                new AuthenticationProperties { RedirectUri = "/Login" },
                "DataBridgeAuth");
        }

        // Azure AD: clear the app cookie AND sign out of the OIDC session so
        // Azure AD doesn't silently re-authenticate on the next visit.
        return SignOut(
            new AuthenticationProperties { RedirectUri = "/" },
            "DataBridgeAuth",
            OpenIdConnectDefaults.AuthenticationScheme);
    }
}
