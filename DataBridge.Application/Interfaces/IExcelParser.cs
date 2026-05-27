using System.Data;

namespace DataBridge.Application.Interfaces;

public interface IExcelParser
{
    Task<(IReadOnlyList<string> Columns, DataTable Data)> BuildMergedDataTableAsync(
        IReadOnlyList<(string FileName, Stream Stream)> files,
        CancellationToken ct = default,
        bool sanitizeColumnNames = true);
}
