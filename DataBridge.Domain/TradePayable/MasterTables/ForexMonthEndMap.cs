namespace DataBridge.Domain.TradePayable.MasterTables;

public class ForexMonthEndMap
{
    public Guid Id { get; set; }
    public string Currency { get; set; } = null!;
    public DateTime Date { get; set; }
    public decimal Conversion_Rate { get; set; }
}
