using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;

namespace fredapi.Hubs;

public class SportMatchHub : Hub
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<SportMatchHub> _logger;

    public SportMatchHub(IMemoryCache cache, ILogger<SportMatchHub> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);

        // Send cached prediction data if available
        if (_cache.TryGetValue("prediction_data", out var predictionData))
        {
            await Clients.Caller.SendAsync("ReceivePredictionData", predictionData);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
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