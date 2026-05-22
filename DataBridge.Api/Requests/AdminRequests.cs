namespace DataBridge.Api.Requests;

public sealed class AddUserHttpRequest
{
    public string Email       { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int    RoleId      { get; set; }
}

public sealed class SetActiveHttpRequest
{
    public bool IsActive { get; set; }
}
