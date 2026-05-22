using DataBridge.Application.Interfaces;
using DataBridge.Application.UseCases;
using DataBridge.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DataBridge.Api.Pages.Admin;

[Authorize(Policy = "Admin")]
public class UsersModel(
    IUserRepository userRepository,
    AddUserUseCase addUserUseCase) : PageModel
{
    public IReadOnlyList<DataBridgeUser> Users { get; private set; } = [];
    public IReadOnlyList<DataBridgeUserRole> Roles { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Users = await userRepository.GetAllUsersAsync();
        Roles = await userRepository.GetAllRolesAsync();
    }

    public async Task<IActionResult> OnPostAddAsync(string email, string displayName, int roleId)
    {
        try
        {
            await addUserUseCase.ExecuteAsync(email, displayName, roleId);
            return RedirectToPage();
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            Users = await userRepository.GetAllUsersAsync();
            Roles = await userRepository.GetAllRolesAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostToggleAsync(int id, bool isActive)
    {
        await userRepository.SetActiveAsync(id, isActive);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        await userRepository.DeleteUserAsync(id);
        return RedirectToPage();
    }
}
