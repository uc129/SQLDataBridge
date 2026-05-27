using System.Data;

namespace DataBridge.Domain.TradePayable.Contracts;

public interface IBackupTablesRepository
{
    Task SaveAndAppendStepResultAsync(DataTable data, Guid runId, int stepIndex);
}
