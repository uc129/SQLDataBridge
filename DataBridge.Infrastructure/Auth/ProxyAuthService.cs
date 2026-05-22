using DataBridge.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace DataBridge.Infrastructure.Auth;

internal sealed class ProxyAuthService(IUserRepository userRepository, IConfiguration config) : IProxyAuthService
{
    private const string AuthCookie = "DataBridgeAuth";

    public bool ValidateOtp(string otp)
    {
        var secret = config["DataBridge:ProxyOtp"] ?? string.Empty;
        return !string.IsNullOrWhiteSpace(secret) && otp == secret;
    }

    public async Task<ClaimsPrincipal?> BuildPrincipalAsync(int userId, CancellationToken ct = default)
    {
        var user = await userRepository.GetUserByIdAsync(userId, ct);
        if (user is null || !user.IsActive) return null;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Email),
            new(ClaimTypes.Name,           user.DisplayName),
            new(ClaimTypes.Email,          user.Email),
            new(ClaimTypes.Role,           user.RoleCode),
            new("DataBridgeRole",          user.RoleCode),
            new("DataBridgeDisplayName",   user.DisplayName),
            new("DataBridgePSNO",          user.PSNO),
            new("DataBridgeAuthMethod",    "Proxy"),
        };

        var identity = new ClaimsIdentity(claims, AuthCookie);
        return new ClaimsPrincipal(identity);
    }
}
