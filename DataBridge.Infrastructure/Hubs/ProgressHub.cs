using Microsoft.AspNetCore.SignalR;

namespace DataBridge.Infrastructure.Hubs;

public class ProgressHub : Hub
{
    public async Task JoinJob(string jobId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, jobId);
    }
}
