using DataBridge.Api.Requests;
using DataBridge.Application.Interfaces;
using DataBridge.Application.UseCases;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DataBridge.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(
    ProxySignInUseCase signInUseCase,
    IUserRepository userRepository) : ControllerBase
{
    private const string AuthCookie = "DataBridgeAuth";

    [AllowAnonymous]
    [HttpGet("proxy-users")]
    public async Task<IActionResult> ProxyUsers()
    {
        var users = await userRepository.GetActiveUsersAsync();
        return Ok(users.Select(u => new { u.Id, u.DisplayName, u.RoleCode }));
    }

    [AllowAnonymous]
    [HttpPost("proxy-login")]
    public async Task<IActionResult> ProxyLogin([FromBody] ProxyLoginHttpRequest req)
    {
        try
        {
            var principal = await signInUseCase.ExecuteAsync(req.UserId, req.Otp);
            var props = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc   = DateTimeOffset.UtcNow.AddHours(8),
            };
            await HttpContext.SignInAsync(AuthCookie, principal, props);
            return Ok(new { message = "Signed in." });
        }
        catch (InvalidOperationException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpGet("sso-login")]
    public IActionResult SsoLogin([FromQuery] string? returnUrl = null)
    {
        return Challenge(
            new AuthenticationProperties { RedirectUri = returnUrl ?? "/" },
            "OpenIdConnect");
    }

    [Authorize]
    [HttpPost("sign-out")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(AuthCookie);
        return Ok(new { message = "Signed out." });
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        return Ok(new
        {
            name       = User.FindFirstValue("DataBridgeDisplayName"),
            email      = User.FindFirstValue(ClaimTypes.Email),
            role       = User.FindFirstValue("DataBridgeRole"),
            psno       = User.FindFirstValue("DataBridgePSNO"),
            authMethod = User.FindFirstValue("DataBridgeAuthMethod"),
        });
    }
}
