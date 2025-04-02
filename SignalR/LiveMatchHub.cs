using fredapi.Model;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using fredapi.SportRadarService.Background.ArbitrageLiveMatchBackgroundService;

namespace fredapi.SignalR;

public class LiveMatchHub : Hub
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<LiveMatchHub> _logger;

    public LiveMatchHub(IMemoryCache cache, ILogger<LiveMatchHub> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        // Send the last cached arbitrage matches to the newly connected client
        var cachedArbitrageMatches = ArbitrageLiveMatchBackgroundService.GetLastSentArbitrageMatches();
        if (cachedArbitrageMatches.Any())
        {
            await Clients.Caller.SendAsync("ReceiveArbitrageLiveMatches", cachedArbitrageMatches);
        }

        // Send the last cached all matches to the newly connected client
        var cachedAllMatches = ArbitrageLiveMatchBackgroundService.GetLastSentAllMatches();
        if (cachedAllMatches.Any())
        {
            await Clients.Caller.SendAsync("ReceiveAllLiveMatches", cachedAllMatches);
        }

        // Send cached prediction data if available
        if (_cache.TryGetValue("prediction_data", out var predictionData))
        {
            await Clients.Caller.SendAsync("ReceivePredictionData", predictionData);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMatches(List<EnrichedMatch> matches)
    {
        await Clients.All.SendAsync("ReceiveLiveMatches", matches);
    }

    public async Task RequestPredictionData()
    {
        try
        {
            if (_cache.TryGetValue("prediction_data", out var predictionData))
            {
                await Clients.Caller.SendAsync("ReceivePredictionData", predictionData);
            }
            else
            {
                await Clients.Caller.SendAsync("Error", "Prediction data not available");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending prediction data to client {ConnectionId}", Context.ConnectionId);
            await Clients.Caller.SendAsync("Error", "Failed to send prediction data");
        }
    }
}