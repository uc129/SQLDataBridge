using DataBridge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DataBridge.Pages.Admin;

[Authorize(Policy = "Admin")]
public class UsersModel(UserService userService) : PageModel
{
    public IEnumerable<DataBridgeUser>     Users { get; private set; } = [];
    public IEnumerable<DataBridgeUserRole> Roles { get; private set; } = [];

    [BindProperty] public string NewEmail       { get; set; } = string.Empty;
    [BindProperty] public string NewDisplayName { get; set; } = string.Empty;
    [BindProperty] public int    NewRoleId      { get; set; }

    public async Task OnGetAsync()
    {
        Users = await userService.GetAllUsersAsync();
        Roles = await userService.GetAllRolesAsync();
    }

    public async Task<IActionResult> OnPostAddAsync()
    {
        Roles = await userService.GetAllRolesAsync();

        if (string.IsNullOrWhiteSpace(NewEmail) || string.IsNullOrWhiteSpace(NewDisplayName))
        {
            ModelState.AddModelError(string.Empty, "Email and Display Name are required.");
            Users = await userService.GetAllUsersAsync();
            return Page();
        }

        if (!Roles.Any(r => r.Id == NewRoleId))
        {
            ModelState.AddModelError(string.Empty, "Invalid role.");
            Users = await userService.GetAllUsersAsync();
            return Page();
        }

        await userService.AddUserAsync(NewEmail.Trim().ToLowerInvariant(), NewDisplayName.Trim(), NewRoleId);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id, bool isActive)
    {
        await userService.SetActiveAsync(id, isActive);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        await userService.DeleteUserAsync(id);
        return RedirectToPage();
    }
}
