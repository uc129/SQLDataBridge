using ClosedXML.Excel;
using DataBridge.Application.Interfaces;

namespace DataBridge.Infrastructure.Excel;

internal sealed class ClosedXmlExcelWriter : IExcelWriter
{
    public string WritePartFile(
        IReadOnlyList<string> columns,
        IReadOnlyList<object?[]> rows,
        string filePrefix,
        string sheetName,
        string outputFolder,
        int partNumber)
    {
        var filename = $"{filePrefix}_part{partNumber:D2}.xlsx";
        var filepath = Path.Combine(outputFolder, filename);

        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet(sheetName);

        for (int c = 0; c < columns.Count; c++)
        {
            var cell = ws.Cell(1, c + 1);
            cell.Value = columns[c];
            cell.Style.Font.Bold            = true;
            cell.Style.Font.FontColor       = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E79");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        for (int r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            for (int c = 0; c < row.Length; c++)
            {
                var val = row[c];
                if (val != null)
                    ws.Cell(r + 2, c + 1).SetValue(val.ToString());
            }

            if (r % 2 == 0)
                ws.Row(r + 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#EBF3FB");
        }

        ws.Columns().AdjustToContents(1, Math.Min(201, rows.Count + 1));
        ws.SheetView.FreezeRows(1);
        ws.RangeUsed()!.SetAutoFilter();

        wb.SaveAs(filepath);
        return filepath;
    }
}
