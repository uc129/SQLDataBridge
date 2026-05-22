namespace DataBridge.Application.Commands;

public sealed class ImportAndCleanCommand
{
    public required string JobId            { get; init; }
    public required string ConnectionString { get; init; }
    public required string TableName        { get; init; }
    public          string SchemaName       { get; init; } = "dbo";
    public          bool   ReplaceTable     { get; init; }
    public Dictionary<string, string?>?     ColumnMap       { get; init; }
    public int[]                            PoLeadingDigits  { get; init; } = [7, 8, 3];
    public required IReadOnlyList<(string FileName, Stream Stream)> Files { get; init; }
}
