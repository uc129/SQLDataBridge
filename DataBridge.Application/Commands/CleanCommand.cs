namespace DataBridge.Application.Commands;

public sealed class CleanCommand
{
    public required string JobId { get; init; }
    public required string TableName { get; init; }
    public Dictionary<string, string?>? ColumnMap { get; init; }
    public int[] PoLeadingDigits { get; init; } = [7, 8, 3];
}
