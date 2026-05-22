namespace DataBridge.Domain.Entities;

public class DataBridgeUserRole
{
    public int    Id       { get; set; }
    public string RoleCode { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
}
