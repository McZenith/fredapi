using fredapi.Model;
using Microsoft.AspNetCore.SignalR;
using fredapi.SportRadarService.Background.ArbitrageLiveMatchBackgroundService;

namespace fredapi.SignalR;

public class LiveMatchHub(ILogger<LiveMatchHub> logger) : Hub
{
    public override async Task OnConnectedAsync()
    {
        logger.LogInformation($"Client connected: {Context.ConnectionId}");

        // Send the last cached arbitrage matches to the newly connected client
        var cachedArbitrageMatches = ArbitrageLiveMatchBackgroundService.GetLastSentArbitrageMatches();
        if (cachedArbitrageMatches.Any())
        {
            logger.LogInformation($"Sending cached {cachedArbitrageMatches.Count} arbitrage matches to client: {Context.ConnectionId}");
            await Clients.Caller.SendAsync("ReceiveArbitrageLiveMatches", cachedArbitrageMatches);
        }

        // Send the last cached all matches to the newly connected client
        var cachedAllMatches = ArbitrageLiveMatchBackgroundService.GetLastSentAllMatches();
        if (cachedAllMatches.Any())
        {
            logger.LogInformation($"Sending cached {cachedAllMatches.Count} all matches to client: {Context.ConnectionId}");
            await Clients.Caller.SendAsync("ReceiveAllLiveMatches", cachedAllMatches);
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