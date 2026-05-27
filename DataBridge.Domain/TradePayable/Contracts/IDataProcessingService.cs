using DataBridge.Domain.TradePayable.Models;

namespace DataBridge.Domain.TradePayable.Contracts;

public interface IDataProcessingService
{
    Task<ProcessState> RunStepsUpTo(int targetStepIndex, ProcessState state);
}
