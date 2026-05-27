using DataBridge.Domain.TradePayable.Models;

namespace DataBridge.Domain.TradePayable.Contracts;

public interface IProcessStep
{
    int StepIndex { get; }
    Task<ProcessState> ExecuteAsync(ProcessState state);
}
