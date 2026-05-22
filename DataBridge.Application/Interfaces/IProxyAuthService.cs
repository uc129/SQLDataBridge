using System.Security.Claims;

namespace DataBridge.Application.Interfaces;

public interface IProxyAuthService
{
    bool ValidateOtp(string otp);
    Task<ClaimsPrincipal?> BuildPrincipalAsync(int userId, CancellationToken ct = default);
}
