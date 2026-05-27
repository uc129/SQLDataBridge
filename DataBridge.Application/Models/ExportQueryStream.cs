namespace DataBridge.Application.Models;

public sealed record ExportQueryStream(
    IReadOnlyList<string> Columns,
    IAsyncEnumerable<object?[]> Rows);
