using fredapi.Model;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using fredapi.SportRadarService.Background.ArbitrageLiveMatchBackgroundService;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace fredapi.SignalR;

public class LiveMatchHub : Hub
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<LiveMatchHub> _logger;
    private static readonly string _arbitrageMatchesCacheKey = "arbitrage_matches_cache";
    private static readonly string _allMatchesCacheKey = "all_matches_cache";
    private static readonly string _predictionDataCacheKey = "prediction_data";
    
    // Keep track of connected clients for potential targeted updates
    private static readonly HashSet<string> _connectedClients = new HashSet<string>();

    public LiveMatchHub(IMemoryCache cache, ILogger<LiveMatchHub> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        try
        {
            string connectionId = Context.ConnectionId;
            _logger.LogDebug("Client connected: {ConnectionId}", connectionId);
            
            // Add to connected clients set with thread safety
            lock (_connectedClients)
            {
                _connectedClients.Add(connectionId);
            }

            // Group all tasks to execute them concurrently
            var tasks = new List<Task>();

            // Use cached data from memory cache instead of static service
            if (_cache.TryGetValue(_arbitrageMatchesCacheKey, out List<EnrichedMatch> arbitrageMatches) && 
                arbitrageMatches?.Any() == true)
            {
                tasks.Add(Clients.Caller.SendAsync("ReceiveArbitrageLiveMatches", arbitrageMatches));
            }
            else
            {
                // Fallback to static service data if not in cache
                var serviceArbitrageMatches = ArbitrageLiveMatchBackgroundService.GetLastSentArbitrageMatches();
                if (serviceArbitrageMatches?.Any() == true)
                {
                    // Update cache for future connections
                    _cache.Set(_arbitrageMatchesCacheKey, serviceArbitrageMatches, 
                        TimeSpan.FromMinutes(5));
                    tasks.Add(Clients.Caller.SendAsync("ReceiveArbitrageLiveMatches", serviceArbitrageMatches));
                }
            }

            // Same for all matches
            if (_cache.TryGetValue(_allMatchesCacheKey, out List<EnrichedMatch> allMatches) && 
                allMatches?.Any() == true)
            {
                tasks.Add(Clients.Caller.SendAsync("ReceiveAllLiveMatches", allMatches));
            }
            else
            {
                var serviceAllMatches = ArbitrageLiveMatchBackgroundService.GetLastSentAllMatches();
                if (serviceAllMatches?.Any() == true)
                {
                    _cache.Set(_allMatchesCacheKey, serviceAllMatches, 
                        TimeSpan.FromMinutes(5));
                    tasks.Add(Clients.Caller.SendAsync("ReceiveAllLiveMatches", serviceAllMatches));
                }
            }

            // Send prediction data if available
            if (_cache.TryGetValue(_predictionDataCacheKey, out var predictionData))
            {
                tasks.Add(Clients.Caller.SendAsync("ReceivePredictionData", predictionData));
            }

            // Wait for all data to be sent concurrently
            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
            }

            await base.OnConnectedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during client connection: {ConnectionId}", Context.ConnectionId);
            throw; // Rethrow to let SignalR handle it
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            string connectionId = Context.ConnectionId;
            _logger.LogDebug("Client disconnected: {ConnectionId}, Reason: {Exception}", 
                connectionId, exception?.Message ?? "Normal disconnect");
            
            // Remove from connected clients set with thread safety
            lock (_connectedClients)
            {
                _connectedClients.Remove(connectionId);
            }
            
            await base.OnDisconnectedAsync(exception);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during client disconnection: {ConnectionId}", Context.ConnectionId);
            throw; // Rethrow to let SignalR handle it
        }
    }

    public async Task SendMatches(List<EnrichedMatch> matches)
    {
        if (matches?.Any() != true)
        {
            _logger.LogWarning("Attempted to send empty matches list");
            return;
        }

        try
        {
            // Cache the data for new connections
            _cache.Set(_allMatchesCacheKey, matches, TimeSpan.FromMinutes(5));
            
            // Send to all clients
            await Clients.All.SendAsync("ReceiveLiveMatches", matches);
            
            _logger.LogDebug("Sent {Count} matches to {ClientCount} clients", 
                matches.Count, _connectedClients.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending matches to clients");
        }
    }

    public async Task RequestPredictionData()
    {
        try
        {
            if (_cache.TryGetValue(_predictionDataCacheKey, out var predictionData))
            {
                await Clients.Caller.SendAsync("ReceivePredictionData", predictionData);
                _logger.LogDebug("Sent prediction data to client {ConnectionId}", Context.ConnectionId);
            }
            else
            {
                await Clients.Caller.SendAsync("Error", "Prediction data not available");
                _logger.LogWarning("Prediction data requested but not available for client {ConnectionId}", Context.ConnectionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending prediction data to client {ConnectionId}", Context.ConnectionId);
            await Clients.Caller.SendAsync("Error", "Failed to send prediction data");
        }
    }

    // Utility method to get connected client count (for diagnostics)
    public static int GetConnectedClientCount()
    {
        lock (_connectedClients)
        {
            return _connectedClients.Count;
        }
    }
}