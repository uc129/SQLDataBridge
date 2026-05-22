namespace DataBridge.Domain.Entities;

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

    public static DataBridgeUser Create(string email, string displayName, int roleId)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name is required.", nameof(displayName));

        return new DataBridgeUser
        {
            Email       = email.Trim().ToLowerInvariant(),
            DisplayName = displayName.Trim(),
            RoleId      = roleId,
            IsActive    = true,
        };
    }

    public void Activate()   => IsActive = true;
    public void Deactivate() => IsActive = false;
}
