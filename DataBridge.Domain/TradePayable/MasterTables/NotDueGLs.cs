namespace DataBridge.Domain.TradePayable.MasterTables;

public class NotDueGLs
{
    public Guid Id { get; set; }
    public string Gl_Code { get; set; } = null!;
    public string? Gl_Description { get; set; }
}
