namespace DataBridge.Api.Requests;

public sealed class ImportHttpRequest
{
    public string          JobId            { get; set; } = string.Empty;
    public string          TableName        { get; set; } = string.Empty;
    public string          SchemaName       { get; set; } = "dbo";
    public bool            ReplaceTable     { get; set; }
    public string?         ConnectionString { get; set; }
    public List<IFormFile> Files            { get; set; } = [];
}
