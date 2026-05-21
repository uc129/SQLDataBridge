using Dapper;
using Microsoft.Data.SqlClient;

namespace DataBridge.Services;

public class DataBridgeUserRole
{
    public int    Id       { get; set; }
    public string RoleCode { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
}

public class DataBridgeUser
{
    public int      Id          { get; set; }
    public string   Email       { get; set; } = string.Empty;
    public string   DisplayName { get; set; } = string.Empty;
    public int      RoleId      { get; set; }
    public string   RoleCode    { get; set; } = string.Empty;
    public string   RoleName    { get; set; } = string.Empty;
    public string   PSNO        { get; set; } = string.Empty;
    public bool     IsActive    { get; set; }
    public DateTime CreatedAt   { get; set; }
    public DateTime ModifiedAt  { get; set; }
}

public class UserService(IConfiguration config)
{
    private string Conn => config.GetConnectionString("Default")!;

    private const string SelectUsers =
        """
        SELECT u.Id, u.Email, u.DisplayName, u.RoleId, u.PSNO, u.IsActive, u.CreatedAt, u.ModifiedAt,
               r.RoleCode, r.RoleName
        FROM   [dbo].[DataBridgeUsers] u
        INNER JOIN [dbo].[DataBridge_UserRoles] r ON r.Id = u.RoleId
        """;

    public async Task<DataBridgeUser?> FindActiveUserAsync(string email)
    {
        await using var conn = new SqlConnection(Conn);
        return await conn.QuerySingleOrDefaultAsync<DataBridgeUser>(
            SelectUsers + " WHERE u.Email = @email AND u.IsActive = 1",
            new { email });
    }

    public async Task<IEnumerable<DataBridgeUser>> GetAllUsersAsync()
    {
        await using var conn = new SqlConnection(Conn);
        return await conn.QueryAsync<DataBridgeUser>(
            SelectUsers + " ORDER BY u.DisplayName");
    }

    public async Task<IEnumerable<DataBridgeUser>> GetActiveUsersAsync()
    {
        await using var conn = new SqlConnection(Conn);
        return await conn.QueryAsync<DataBridgeUser>(
            SelectUsers + " WHERE u.IsActive = 1 ORDER BY u.DisplayName");
    }

    public async Task<DataBridgeUser?> GetUserByIdAsync(int id)
    {
        await using var conn = new SqlConnection(Conn);
        return await conn.QuerySingleOrDefaultAsync<DataBridgeUser>(
            SelectUsers + " WHERE u.Id = @id",
            new { id });
    }

    public async Task<IEnumerable<DataBridgeUserRole>> GetAllRolesAsync()
    {
        await using var conn = new SqlConnection(Conn);
        return await conn.QueryAsync<DataBridgeUserRole>(
            "SELECT Id, RoleCode, RoleName FROM [dbo].[DataBridge_UserRoles] ORDER BY Id");
    }

    public async Task AddUserAsync(string email, string displayName, int roleId)
    {
        await using var conn = new SqlConnection(Conn);
        await conn.ExecuteAsync(
            """
            INSERT INTO [dbo].[DataBridgeUsers] (Email, DisplayName, RoleId, IsActive)
            VALUES (@email, @displayName, @roleId, 1)
            """, new { email, displayName, roleId });
    }

    public async Task SetActiveAsync(int id, bool isActive)
    {
        await using var conn = new SqlConnection(Conn);
        await conn.ExecuteAsync(
            """
            UPDATE [dbo].[DataBridgeUsers]
            SET IsActive = @isActive, ModifiedAt = SYSUTCDATETIME()
            WHERE Id = @id
            """, new { id, isActive });
    }

    public async Task DeleteUserAsync(int id)
    {
        await using var conn = new SqlConnection(Conn);
        await conn.ExecuteAsync(
            "DELETE FROM [dbo].[DataBridgeUsers] WHERE Id = @id", new { id });
    }
}
