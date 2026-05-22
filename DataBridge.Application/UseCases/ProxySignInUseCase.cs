using DataBridge.Application.Interfaces;
using System.Security.Claims;

namespace DataBridge.Application.UseCases;

public class ProxySignInUseCase(IProxyAuthService proxyAuthService, IUserRepository userRepository)
{
    /// <exception cref="InvalidOperationException">Thrown for invalid OTP or inactive user.</exception>
    public async Task<ClaimsPrincipal> ExecuteAsync(int userId, string otp, CancellationToken ct = default)
    {
        if (!proxyAuthService.ValidateOtp(otp))
            throw new InvalidOperationException("Invalid OTP.");

        var user = await userRepository.GetUserByIdAsync(userId, ct);
        if (user is null || !user.IsActive)
            throw new InvalidOperationException("Selected user is not active.");

        return await proxyAuthService.BuildPrincipalAsync(userId, ct)
            ?? throw new InvalidOperationException("Failed to build authentication principal.");
    }
}
