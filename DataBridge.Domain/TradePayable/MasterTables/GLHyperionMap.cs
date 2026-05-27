namespace DataBridge.Domain.TradePayable.MasterTables;

public class GLHyperionMap
{
    public Guid Id { get; set; }
    public string GLCode { get; set; } = null!;
    public string? GL_Description { get; set; }
    public string Hyperion_Code { get; set; } = null!;
    public string? Hyperion_Description { get; set; }
    public string? Billed_Status { get; set; }
}
