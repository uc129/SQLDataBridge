using DataBridge.Domain.TradePayable.Aggregates;

namespace DataBridge.Domain.TradePayable.Contracts;

public interface IFAGLL03StagingRepository
{
    Task BulkInsertAsync(IEnumerable<FAGLL03RAWEntity> rows, Guid runId);
    Task<IEnumerable<FAGLL03RAWEntity>> GetByRunIdAsync(Guid runId);
}
