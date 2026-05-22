using DataBridge.Application.Interfaces;
using System.Collections.Concurrent;

namespace DataBridge.Infrastructure.Jobs;

internal sealed class InMemoryJobRegistry : IJobRegistry
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _jobs = new();

    public CancellationToken Register(string jobId)
    {
        var cts = new CancellationTokenSource();
        _jobs[jobId] = cts;
        return cts.Token;
    }

    public void Cancel(string jobId)
    {
        if (_jobs.TryGetValue(jobId, out var cts))
            cts.Cancel();
    }

    public void Remove(string jobId)
    {
        if (_jobs.TryRemove(jobId, out var cts))
            cts.Dispose();
    }
}
