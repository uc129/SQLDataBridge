namespace DataBridge.Api.Requests;

public sealed class CancelBody
{
    public string JobId { get; set; } = string.Empty;
}

public sealed class TestConnectionHttpRequest
{
    public string ConnectionString { get; set; } = string.Empty;
}
