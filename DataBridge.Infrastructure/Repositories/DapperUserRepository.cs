using Dapper;
using DataBridge.Application.Interfaces;
using DataBridge.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace DataBridge.Infrastructure.Repositories;

internal sealed class DapperUserRepository(IConfiguration config) : IUserRepository
{
    private string Conn => config.GetConnectionString("Default") ?? string.Empty;

    private const string SelectUsers = """
        SELECT u.Id, u.Email, u.DisplayName, u.RoleId, u.PSNO, u.IsActive, u.CreatedAt, u.ModifiedAt,
               r.RoleCode, r.RoleName
        FROM   [dbo].[DataBridgeUsers] u
        INNER JOIN [dbo].[DataBridge_UserRoles] r ON r.Id = u.RoleId
        """;

    public async Task<DataBridgeUser?> FindActiveUserAsync(string email, CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(Conn);
        return await conn.QuerySingleOrDefaultAsync<DataBridgeUser>(
            SelectUsers + " WHERE u.Email = @email AND u.IsActive = 1", new { email });
    }

    public async Task<IReadOnlyList<DataBridgeUser>> GetAllUsersAsync(CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(Conn);
        return (await conn.QueryAsync<DataBridgeUser>(SelectUsers + " ORDER BY u.DisplayName")).AsList();
    }

    public async Task<IReadOnlyList<DataBridgeUser>> GetActiveUsersAsync(CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(Conn);
        return (await conn.QueryAsync<DataBridgeUser>(
            SelectUsers + " WHERE u.IsActive = 1 ORDER BY u.DisplayName")).AsList();
    }

    public async Task<DataBridgeUser?> GetUserByIdAsync(int id, CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(Conn);
        return await conn.QuerySingleOrDefaultAsync<DataBridgeUser>(
            SelectUsers + " WHERE u.Id = @id", new { id });
    }

    public async Task<IReadOnlyList<DataBridgeUserRole>> GetAllRolesAsync(CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(Conn);
        return (await conn.QueryAsync<DataBridgeUserRole>(
            "SELECT Id, RoleCode, RoleName FROM [dbo].[DataBridge_UserRoles] ORDER BY Id")).AsList();
    }

    public async Task AddUserAsync(string email, string displayName, int roleId, CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(Conn);
        await conn.ExecuteAsync(
            """
            INSERT INTO [dbo].[DataBridgeUsers] (Email, DisplayName, RoleId, IsActive)
            VALUES (@email, @displayName, @roleId, 1)
            """, new { email, displayName, roleId });
    }

    public async Task SetActiveAsync(int id, bool isActive, CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(Conn);
        await conn.ExecuteAsync(
            """
            UPDATE [dbo].[DataBridgeUsers]
            SET IsActive = @isActive, ModifiedAt = SYSUTCDATETIME()
            WHERE Id = @id
            """, new { id, isActive });
    }

    public async Task DeleteUserAsync(int id, CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(Conn);
        await conn.ExecuteAsync("DELETE FROM [dbo].[DataBridgeUsers] WHERE Id = @id", new { id });
    }
}
