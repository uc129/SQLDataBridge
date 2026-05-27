namespace DataBridge.Domain.TradePayable.Models;

public class PipelineRun
{
    public Guid RunId { get; set; }
    public DateTime QuarterDate { get; set; }
    public string RevisionNumber { get; set; } = null!;
    public int CurrentStepIndex { get; set; } = 0;
    public PipelineRunStatus Status { get; set; } = PipelineRunStatus.Uploaded;
    public string? StartedBy { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
