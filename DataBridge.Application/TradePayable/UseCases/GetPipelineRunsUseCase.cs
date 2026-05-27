using DataBridge.Domain.TradePayable.Contracts;
using DataBridge.Domain.TradePayable.Models;

namespace DataBridge.Application.TradePayable.UseCases;

public class GetPipelineRunsUseCase(IPipelineRunRepository pipelineRunRepo)
{
    public Task<IEnumerable<PipelineRun>> ExecuteAsync() =>
        pipelineRunRepo.GetAllAsync();
}
