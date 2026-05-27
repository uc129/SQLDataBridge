namespace DataBridge.Domain.TradePayable.MasterTables;

public class AgeingGroup
{
    public Guid Id { get; set; }
    public int Group_Code { get; set; }
    public string Group_Name { get; set; } = null!;
}
