namespace DataBridge.Domain.TradePayable.MasterTables;

public class ICPVendorMap
{
    public Guid Id { get; set; }
    public string ICP_Name { get; set; } = null!;
    public string? Vendor_Code { get; set; }
    public string? Vendor_Name { get; set; }
    public string? Entity_Type { get; set; }
    public string? Entity_Relation { get; set; }
}
