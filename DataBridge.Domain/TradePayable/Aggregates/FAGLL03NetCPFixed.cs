namespace DataBridge.Domain.TradePayable.Aggregates;

public class FAGLL03NetCPFixed : FAGLL03NetLiability
{
    public FAGLL03NetCPFixed() { }

    public FAGLL03NetCPFixed(FAGLL03NetLiability liability) : base(liability) { }

    public bool CP_Fixed { get; set; } = false;
}
