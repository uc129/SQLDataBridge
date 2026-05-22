using DataBridge.Api.Requests;
using DataBridge.Application.Interfaces;
using DataBridge.Application.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataBridge.Api.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Policy = "Admin")]
public class AdminController(
    IUserRepository userRepository,
    AddUserUseCase addUserUseCase) : ControllerBase
{
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await userRepository.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await userRepository.GetAllRolesAsync();
        return Ok(roles);
    }

    [HttpPost("users")]
    public async Task<IActionResult> AddUser([FromBody] AddUserHttpRequest req)
    {
        try
        {
            await addUserUseCase.ExecuteAsync(req.Email, req.DisplayName, req.RoleId);
            return StatusCode(StatusCodes.Status201Created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPatch("users/{id}/active")]
    public async Task<IActionResult> SetActive(int id, [FromBody] SetActiveHttpRequest req)
    {
        await userRepository.SetActiveAsync(id, req.IsActive);
        return NoContent();
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        await userRepository.DeleteUserAsync(id);
        return NoContent();
    }
}
