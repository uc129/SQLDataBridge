using DataBridge.Domain.Models;

namespace DataBridge.Application.Interfaces;

public interface IProgressNotifier
{
    Task NotifyAsync(string jobId, ProgressMessage message);
}
