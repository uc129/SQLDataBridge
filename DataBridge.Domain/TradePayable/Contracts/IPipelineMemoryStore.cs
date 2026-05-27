using DataBridge.Domain.TradePayable.Aggregates;

namespace DataBridge.Domain.TradePayable.Contracts;

/// <summary>
/// Singleton store that bridges the gap between the upload HTTP request and the
/// subsequent run-pipeline request.  Raw entities are placed here after upload and
/// consumed (then freed) by Step01, so every in-between step avoids a DB round-trip.
/// Falls back gracefully when the store is cold (e.g. after a server restart).
/// </summary>
public interface IPipelineMemoryStore
{
    void Store(Guid runId, List<FAGLL03RAWEntity> entities);
    List<FAGLL03RAWEntity>? Get(Guid runId);
    void Remove(Guid runId);
}
