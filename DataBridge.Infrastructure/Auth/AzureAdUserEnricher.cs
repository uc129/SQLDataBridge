using DataBridge.Application.Interfaces;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Security.Claims;

namespace DataBridge.Infrastructure.Auth;

public sealed class AzureAdUserEnricher(IUserRepository userRepository)
{
    public async Task OnTokenValidatedAsync(TokenValidatedContext ctx)
    {
        var upn = ctx.Principal?.FindFirstValue("preferred_username")
               ?? ctx.Principal?.FindFirstValue(ClaimTypes.Email)
               ?? string.Empty;

        if (string.IsNullOrWhiteSpace(upn))
        {
            ctx.Fail("No UPN/email claim found in Azure AD token.");
            return;
        }

        var user = await userRepository.FindActiveUserAsync(upn.ToLowerInvariant());

        if (user is null)
        {
            ctx.Fail($"'{upn}' is not registered or inactive in DataBridge.");
            return;
        }

        var identity = (ClaimsIdentity)ctx.Principal!.Identity!;
        identity.AddClaim(new Claim(ClaimTypes.Role,         user.RoleCode));
        identity.AddClaim(new Claim("DataBridgeDisplayName", user.DisplayName));
        identity.AddClaim(new Claim("DataBridgeRole",        user.RoleCode));
        identity.AddClaim(new Claim("DataBridgePSNO",        user.PSNO));
        identity.AddClaim(new Claim("DataBridgeAuthMethod",  "SSO"));
    }
}
