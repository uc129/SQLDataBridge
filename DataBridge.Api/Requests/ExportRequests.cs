namespace DataBridge.Api.Requests;

public sealed class ExportRunHttpRequest
{
    public string  JobId            { get; set; } = string.Empty;
    public string? ConnectionString { get; set; }
    public string  QueryOrView      { get; set; } = string.Empty;
    public bool    IsRawQuery       { get; set; }
    public string  FilePrefix       { get; set; } = "export";
    public string  SheetName        { get; set; } = "Data";
    public int     MaxRowsPerFile   { get; set; } = 1_000_000;
}
