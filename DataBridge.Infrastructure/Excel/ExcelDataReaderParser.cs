using DataBridge.Application.Interfaces;
using DataBridge.Domain.Services;
using ExcelDataReader;
using System.Data;
using System.Text;

namespace DataBridge.Infrastructure.Excel;

internal sealed class ExcelDataReaderParser : IExcelParser
{
    public async Task<(IReadOnlyList<string> Columns, DataTable Data)> BuildMergedDataTableAsync(
        IReadOnlyList<(string FileName, Stream Stream)> files, CancellationToken ct = default)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var allColumns = new List<string>();
        var seenCols   = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var snapshots  = new List<(string FileName, byte[] Data)>();

        // Pass 1: cache file bytes, collect unique column names from headers
        foreach (var (fileName, stream) in files)
        {
            var data = await ReadStreamAsync(stream);
            snapshots.Add((fileName, data));

            using var ms     = new MemoryStream(data);
            using var reader = ExcelReaderFactory.CreateReader(ms);
            var ds = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration
                {
                    UseHeaderRow  = true,
                    ReadHeaderRow = _ => { }
                },
                UseColumnDataType = false
            });
            foreach (DataTable t in ds.Tables)
            {
                foreach (DataColumn col in t.Columns)
                {
                    var safe = ColumnNameSanitizer.SafeColumnName(col.ColumnName);
                    if (!string.IsNullOrWhiteSpace(safe) && seenCols.Add(safe))
                        allColumns.Add(safe);
                }
                break;
            }
        }

        var merged = new DataTable();
        foreach (var col in allColumns)
            merged.Columns.Add(col, typeof(string));

        // Pass 2: load full data into merged DataTable
        foreach (var (_, data) in snapshots)
        {
            ct.ThrowIfCancellationRequested();

            using var ms     = new MemoryStream(data);
            using var reader = ExcelReaderFactory.CreateReader(ms);
            var ds = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration
                {
                    UseHeaderRow          = true,
                    EmptyColumnNamePrefix = "col_"
                },
                UseColumnDataType = false
            });

            var src    = ds.Tables[0];
            var colMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataColumn col in src.Columns)
                colMap[col.ColumnName] = ColumnNameSanitizer.SafeColumnName(col.ColumnName);

            foreach (DataRow srcRow in src.Rows)
            {
                var newRow = merged.NewRow();
                foreach (DataColumn srcCol in src.Columns)
                {
                    if (colMap.TryGetValue(srcCol.ColumnName, out var safeName) && merged.Columns.Contains(safeName))
                    {
                        var val = srcRow[srcCol];
                        newRow[safeName] = val == DBNull.Value || val == null ? (object)DBNull.Value : val.ToString()!;
                    }
                }
                merged.Rows.Add(newRow);
            }
        }

        return (allColumns, merged);
    }

    private static async Task<byte[]> ReadStreamAsync(Stream s)
    {
        using var ms = new MemoryStream();
        await s.CopyToAsync(ms);
        return ms.ToArray();
    }
}
