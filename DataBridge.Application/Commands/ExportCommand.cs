namespace DataBridge.Application.Commands;

public sealed class ExportCommand
{
    public required string JobId            { get; init; }
    public required string ConnectionString { get; init; }
    public required string QueryOrView      { get; init; }
    public          bool   IsRawQuery       { get; init; }
    public required string OutputFolder     { get; init; }
    public          string FilePrefix       { get; init; } = "export";
    public          string SheetName        { get; init; } = "Data";
    public          int    MaxRowsPerFile   { get; init; } = 1_000_000;
}
