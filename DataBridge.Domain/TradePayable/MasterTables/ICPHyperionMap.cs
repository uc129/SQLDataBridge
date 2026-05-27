namespace DataBridge.Domain.TradePayable.MasterTables;

public class ICPHyperionMap
{
    public Guid Id { get; set; }
    public string ICP_Name { get; set; } = null!;
    public string? Hyperion_Credit { get; set; }
    public string? Hyperion_Debit { get; set; }
}
