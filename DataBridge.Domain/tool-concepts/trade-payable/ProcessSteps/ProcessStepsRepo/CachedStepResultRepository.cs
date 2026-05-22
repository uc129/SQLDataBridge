using Application.ProcessSteps.PipelineCache;
using Domain.Shared;
using Shared.Extensions;
using System.Data;

namespace Application.ProcessSteps.ProcessStepsRepo
{
    public class CachedStepResultRepository(StepResultRepository inner, PipelineDataCache cache) : IStepResultsRepository
    {
        private readonly StepResultRepository _inner = inner;
        private readonly PipelineDataCache _cache = cache;

        public async Task<DataTable> RetrieveStepResultAsync(Guid processId, int stepIndex)
        {
            var cached = _cache.GetAndEvict(stepIndex);
            if (cached != null) return cached;

            var result = await _inner.RetrieveStepResultAsync(processId, stepIndex);
            if (result.Rows.Count > 0) _cache.Set(stepIndex, result);
            return result;
        }

        public async Task<IEnumerable<T>> RetrieveStepResultAsIEnumerableAsync<T>(Guid processId, int stepIndex) where T : new()
        {
            var cached = _cache.GetAndEvict(stepIndex);
            if (cached != null) return cached.ToEntities<T>();

            var result = await _inner.RetrieveStepResultAsIEnumerableAsync<T>(processId, stepIndex);
            return result;
        }

        public async Task<Message> SaveAndReplaceStepResultAsync(DataTable data, Guid processId, int stepIndex)
        {
            var result = await _inner.SaveAndReplaceStepResultAsync(data, processId, stepIndex);
            if (result.Success) _cache.Set(stepIndex, data);
            return result;
        }

        public Task<Message> SaveAndAppendStepResultAsync(DataTable data, Guid processId, int stepIndex)
            => _inner.SaveAndAppendStepResultAsync(data, processId, stepIndex);

        public Task<IEnumerable<T>> RetrievePaginatedStepResultAsIEnumerableAsync<T>(Guid processId, int stepIndex, int pageSize, int skip) where T : new()
            => _inner.RetrievePaginatedStepResultAsIEnumerableAsync<T>(processId, stepIndex, pageSize, skip);
    }
}
