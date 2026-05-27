namespace DataBridge.Domain.TradePayable.Models;

public enum PipelineRunStatus
{
    Uploaded,
    Running,
    StepComplete,
    Completed,
    Failed,
    Cancelled
}
