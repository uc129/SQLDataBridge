namespace DataBridge.Application.TradePayable.UseCases.Commands;

public class RunPipelineStepCommand
{
    public required Guid   RunId           { get; set; }
    public required int    TargetStepIndex { get; set; }
    public required string JobId           { get; set; }
}

public class RunFullPipelineCommand
{
    public required Guid   RunId { get; set; }
    public required string JobId { get; set; }
}
