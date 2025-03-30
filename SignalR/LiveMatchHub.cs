using fredapi.Model;
using Microsoft.AspNetCore.SignalR;
using fredapi.SportRadarService.Background.ArbitrageLiveMatchBackgroundService;

namespace fredapi.SignalR;

public class LiveMatchHub(ILogger<LiveMatchHub> logger) : Hub
{
    public override async Task OnConnectedAsync()
    {
        logger.LogInformation($"Client connected: {Context.ConnectionId}");

        // Send the last cached matches to the newly connected client
        var cachedMatches = ArbitrageLiveMatchBackgroundService.GetLastSentMatches();
        if (cachedMatches.Any())
        {
            logger.LogInformation($"Sending cached {cachedMatches.Count} matches to client: {Context.ConnectionId}");
            await Clients.Caller.SendAsync("ReceiveArbitrageLiveMatches", cachedMatches);
        }

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