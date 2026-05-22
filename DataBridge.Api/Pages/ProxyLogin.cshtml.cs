using DataBridge.Application.Interfaces;
using DataBridge.Application.UseCases;
using DataBridge.Domain.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DataBridge.Api.Pages;

[AllowAnonymous]
public class ProxyLoginModel(
    IUserRepository userRepository,
    ProxySignInUseCase signInUseCase) : PageModel
{
    public IReadOnlyList<DataBridgeUser> Users { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        if (User.Identity?.IsAuthenticated == true)
            return Redirect("/");
        Users = await userRepository.GetActiveUsersAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int selectedUserId, string otp)
    {
        try
        {
            var principal = await signInUseCase.ExecuteAsync(selectedUserId, otp);
            var props = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc   = DateTimeOffset.UtcNow.AddHours(8),
            };
            await HttpContext.SignInAsync("DataBridgeAuth", principal, props);
            return Redirect("/");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            Users = await userRepository.GetActiveUsersAsync();
            return Page();
        }
    }
}
