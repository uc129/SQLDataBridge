namespace DataBridge.Application.Commands;

public sealed class ImportCommand
{
    public required string JobId            { get; init; }
    public required string ConnectionString { get; init; }
    public required string TableName        { get; init; }
    public          string SchemaName       { get; init; } = "dbo";
    public          bool   ReplaceTable     { get; init; }
    public required IReadOnlyList<(string FileName, Stream Stream)> Files { get; init; }
}
