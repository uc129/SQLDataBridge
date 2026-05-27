using DataBridge.Domain.TradePayable.Aggregates;
using DataBridge.Domain.TradePayable.Contracts;
using System.Collections.Concurrent;

namespace DataBridge.Infrastructure.TradePayable;

internal sealed class PipelineMemoryStore : IPipelineMemoryStore
{
    private readonly ConcurrentDictionary<Guid, List<FAGLL03RAWEntity>> _store = new();

    public void Store(Guid runId, List<FAGLL03RAWEntity> entities) => _store[runId] = entities;

    public List<FAGLL03RAWEntity>? Get(Guid runId) =>
        _store.TryGetValue(runId, out var entities) ? entities : null;

    public void Remove(Guid runId) => _store.TryRemove(runId, out _);
}
