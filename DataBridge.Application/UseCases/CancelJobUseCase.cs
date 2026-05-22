using DataBridge.Application.Interfaces;

namespace DataBridge.Application.UseCases;

public class CancelJobUseCase(IJobRegistry jobRegistry)
{
    public void Execute(string jobId) => jobRegistry.Cancel(jobId);
}
