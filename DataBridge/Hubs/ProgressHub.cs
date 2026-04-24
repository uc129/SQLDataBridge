using Microsoft.AspNetCore.SignalR;

namespace DataBridge.Hubs;

public class ProgressHub : Hub
{
    // Clients call this to join a job-specific group
    public async Task JoinJob(string jobId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, jobId);
    }
}
