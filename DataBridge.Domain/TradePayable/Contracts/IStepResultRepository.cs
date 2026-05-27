using System.Data;

namespace DataBridge.Domain.TradePayable.Contracts;

public interface IStepResultRepository
{
    Task SaveAndReplaceStepResultAsync(DataTable data, Guid runId, int stepIndex);
    Task SaveAndAppendStepResultAsync(DataTable data, Guid runId, int stepIndex);
    Task<DataTable> RetrieveStepResultAsync(Guid runId, int stepIndex);
    Task<IEnumerable<T>> RetrieveStepResultAsIEnumerableAsync<T>(Guid runId, int stepIndex) where T : new();
    Task<bool> StepHasResultsAsync(Guid runId, int stepIndex);

    /// <summary>
    /// Truncates <paramref name="tableName"/> and bulk-inserts <paramref name="data"/> without
    /// adding RunId/StepIndex meta columns. Used to populate the source table that feeds SQL views.
    /// </summary>
    Task TruncateAndInsertAsync(DataTable data, string tableName);

    /// <summary>
    /// Reads all rows from a named SQL view or table and returns them as a DataTable.
    /// </summary>
    Task<DataTable> ReadFromViewAsync(string viewName);
}
