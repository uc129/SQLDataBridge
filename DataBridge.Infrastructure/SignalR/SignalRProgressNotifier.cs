using DataBridge.Application.Interfaces;
using DataBridge.Domain.Models;
using DataBridge.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace DataBridge.Infrastructure.SignalR;

internal sealed class SignalRProgressNotifier(IHubContext<ProgressHub> hub) : IProgressNotifier
{
    public Task NotifyAsync(string jobId, ProgressMessage message) =>
        hub.Clients.Group(jobId).SendAsync("progress", message);
}
