namespace DataBridge.Application.Interfaces;

public interface IJobRegistry
{
    CancellationToken Register(string jobId);
    void Cancel(string jobId);
    void Remove(string jobId);
}
