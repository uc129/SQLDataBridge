namespace DataBridge.Application.TradePayable.UseCases.Commands;

public class UploadFAGLL03Command
{
    public required string   JobId          { get; set; }
    public required DateTime QuarterDate    { get; set; }
    public required string   RevisionNumber { get; set; }
    public required List<(string FileName, Stream FileStream)> Files { get; set; }
    public string?           UserName       { get; set; }
}
