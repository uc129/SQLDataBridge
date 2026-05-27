namespace DataBridge.Domain.TradePayable.MasterTables;

public class LiabilityGLs
{
    public Guid Id { get; set; }
    public string GL_Code { get; set; } = null!;
    public string? GL_Description { get; set; }
}
