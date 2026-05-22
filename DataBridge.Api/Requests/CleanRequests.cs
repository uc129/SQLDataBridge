namespace DataBridge.Api.Requests;

public sealed class CleanRunHttpRequest
{
    public string                       JobId           { get; set; } = string.Empty;
    public string                       TableName       { get; set; } = string.Empty;
    public int[]                        PoLeadingDigits { get; set; } = [7, 8, 3];
    public Dictionary<string, string?>? ColumnMap       { get; set; }
}

public sealed class CleanImportAndRunHttpRequest
{
    public string          JobId            { get; set; } = string.Empty;
    public string          TableName        { get; set; } = string.Empty;
    public string          SchemaName       { get; set; } = "dbo";
    public bool            ReplaceTable     { get; set; }
    public string?         ConnectionString { get; set; }
    public string?         ColumnMapJson    { get; set; }
    public string?         PoDigitsJson     { get; set; }
    public List<IFormFile> Files            { get; set; } = [];
}
