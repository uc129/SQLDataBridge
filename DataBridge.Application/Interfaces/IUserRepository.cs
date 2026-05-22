using DataBridge.Domain.Entities;

namespace DataBridge.Application.Interfaces;

public interface IUserRepository
{
    Task<DataBridgeUser?> FindActiveUserAsync(string email, CancellationToken ct = default);
    Task<IReadOnlyList<DataBridgeUser>> GetAllUsersAsync(CancellationToken ct = default);
    Task<IReadOnlyList<DataBridgeUser>> GetActiveUsersAsync(CancellationToken ct = default);
    Task<DataBridgeUser?> GetUserByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<DataBridgeUserRole>> GetAllRolesAsync(CancellationToken ct = default);
    Task AddUserAsync(string email, string displayName, int roleId, CancellationToken ct = default);
    Task SetActiveAsync(int id, bool isActive, CancellationToken ct = default);
    Task DeleteUserAsync(int id, CancellationToken ct = default);
}
