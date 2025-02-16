using fredapi.Model;
using Microsoft.AspNetCore.SignalR;

namespace fredapi.SignalR;

public class LiveMatchHub(ILogger<LiveMatchHub> logger) : Hub
{
    public override async Task OnConnectedAsync()
    {
        logger.LogInformation($"Client connected: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        logger.LogInformation($"Client disconnected: {Context.ConnectionId}");
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMatches(List<EnrichedMatch> matches)
    {
        await Clients.All.SendAsync("ReceiveLiveMatches", matches);
    }
    
}