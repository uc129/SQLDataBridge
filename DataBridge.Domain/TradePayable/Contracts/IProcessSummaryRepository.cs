using DataBridge.Domain.TradePayable.Models;

namespace DataBridge.Domain.TradePayable.Contracts;

public interface IProcessSummaryRepository
{
    Task UpsertAsync(Guid runId, DateTime quarterDate, ProcessResultSummary summary);
}
