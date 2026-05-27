using System.Data;

namespace DataBridge.Domain.TradePayable.Models;

public class ProcessState
{
    public Guid RunId { get; set; }
    public DateTime CurrentQuarter { get; set; }
    public string RevisionNumber { get; set; } = null!;
    public int CurrentStepIndex { get; set; } = 0;
    public int NextStepIndex { get; set; } = 1;
    public DateTime ReportDate { get; set; }
    public string? UserName { get; set; }
    public DateTime ProcessStartTime { get; set; } = DateTime.UtcNow;
    public DateTime ProcessEndTime { get; set; }
    public TimeSpan ProcessDuration => ProcessEndTime - ProcessStartTime;

    // In-memory DataTable store: steps read input and write output here instead of round-tripping through DB.
    // Keyed by the logical slot index (e.g. 3,4,5,31 are all outputs of Step03).
    public Dictionary<int, DataTable> StepData { get; } = new();

    // Set by Step13 after the final aggregation; surfaced in the completion notification.
    public ProcessResultSummary? Summary { get; set; }
}
