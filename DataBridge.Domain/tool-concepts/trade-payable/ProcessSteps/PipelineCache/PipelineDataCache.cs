using System.Data;

namespace Application.ProcessSteps.PipelineCache
{
    public class PipelineDataCache
    {
        private readonly Dictionary<int, DataTable> _cache = new();

        public bool Has(int stepIndex) => _cache.ContainsKey(stepIndex);

        // Removes the entry while returning it so the DataTable can be GC'd once the caller is done with it.
        public DataTable? GetAndEvict(int stepIndex)
        {
            if (_cache.TryGetValue(stepIndex, out var dt))
            {
                _cache.Remove(stepIndex);
                return dt;
            }
            return null;
        }

        public void Set(int stepIndex, DataTable dt) => _cache[stepIndex] = dt;
    }
}
