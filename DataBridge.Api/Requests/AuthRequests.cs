namespace DataBridge.Api.Requests;

public sealed class ProxyLoginHttpRequest
{
    public int    UserId { get; set; }
    public string Otp    { get; set; } = string.Empty;
}
