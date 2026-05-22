namespace DataBridge.Application.Interfaces;

public interface IExcelWriter
{
    string WritePartFile(
        IReadOnlyList<string> columns,
        IReadOnlyList<object?[]> rows,
        string filePrefix,
        string sheetName,
        string outputFolder,
        int partNumber,
        int totalParts);
}
