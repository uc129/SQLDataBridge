using DataBridge.Domain.TradePayable.MasterTables;

namespace DataBridge.Domain.TradePayable.Contracts;

public interface IGLHyperionMapRepository
{
    Task<IEnumerable<GLHyperionMap>> GetAllAsync();
}
