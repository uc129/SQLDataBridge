using Domain.Models.ProcessRun;

namespace Domain.Contracts
{
    public interface IDataProcessingService
    {
        Task<ProcessState> RunStepsUpTo(int targetStepIndex, ProcessState state);
    }
}