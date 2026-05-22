using DataBridge.Application.Interfaces;

namespace DataBridge.Application.UseCases;

public class AddUserUseCase(IUserRepository userRepository)
{
    public async Task ExecuteAsync(string email, string displayName, int roleId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException("Email is required.");
        if (string.IsNullOrWhiteSpace(displayName))
            throw new InvalidOperationException("Display name is required.");

        var roles = await userRepository.GetAllRolesAsync(ct);
        if (!roles.Any(r => r.Id == roleId))
            throw new InvalidOperationException("Invalid role.");

        await userRepository.AddUserAsync(
            email.Trim().ToLowerInvariant(),
            displayName.Trim(),
            roleId,
            ct);
    }
}
