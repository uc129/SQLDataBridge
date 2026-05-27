using System.Data;

namespace DataBridge.Domain.TradePayable.Contracts;

public interface IMergedDataService
{
    Task<DataTable> ComputeAsync(CancellationToken ct = default);
}
