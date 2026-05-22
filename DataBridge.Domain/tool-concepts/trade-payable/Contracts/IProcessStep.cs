using Domain.Models.ProcessRun;

namespace Domain.Contracts
{
    public interface IProcessStep
    {
        int StepIndex { get; }
        Task<ProcessState> ExecuteAsync(ProcessState state);
    }
}
