using Microsoft.AspNetCore.SignalR;

namespace SoundWords.Hubs;

public class RebuildHub : Hub
{
    public Task JoinJob(string jobId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, jobId);
    }
}

public record RebuildStatus(string Code, string Text);

public record RebuildProgress(int Progress);
