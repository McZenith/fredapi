using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using fredapi.SportRadarService.Background.ArbitrageLiveMatchBackgroundService;
using fredapi.SportRadarService.Transformers;
using fredapi.Utils;
using Exception = System.Exception;


namespace fredapi.SignalR;

public class LiveMatchHub : Hub
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<LiveMatchHub> _logger;
    private static readonly string _arbitrageMatchesCacheKey = "arbitrage_matches_cache";
    private static readonly string _allMatchesCacheKey = "all_matches_cache";
    private static readonly string _predictionDataCacheKey = "prediction_data";
    private static readonly string _predictionResultsCacheKey = "prediction_results";
    
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
        if (_cache.TryGetValue(_arbitrageMatchesCacheKey, out List<ClientMatch> arbitrageMatches) && 
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
        if (_cache.TryGetValue(_allMatchesCacheKey, out List<ClientMatch> allMatches) && 
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

        // IMPROVED: Send prediction data if available - with type checking and logging
        if (_cache.TryGetValue(_predictionDataCacheKey, out object predictionDataObj))
        {
            _logger.LogDebug("Found prediction data in cache of type: {Type}", predictionDataObj?.GetType().Name ?? "null");
            
            // Check if it's the proper type
            if (predictionDataObj is PredictionDataResponse predictionData)
            {
                _logger.LogDebug("Sending cached prediction data with {Count} matches to new client", 
                    predictionData.Data?.UpcomingMatches?.Count ?? 0);
                tasks.Add(Clients.Caller.SendAsync("ReceivePredictionData", predictionData));
            }
            else
            {
                _logger.LogWarning("Cached prediction data is not of expected type PredictionDataResponse but {ActualType}. " + 
                                  "This will likely cause client-side errors.",
                    predictionDataObj?.GetType().Name ?? "null");
                
                // Try to send something for backward compatibility
                tasks.Add(Clients.Caller.SendAsync("ReceivePredictionData", predictionDataObj));
            }
        }
        else
        {
            _logger.LogDebug("No prediction data found in cache for new client");
        }
        
        // Send prediction results if available
        if (_cache.TryGetValue(_predictionResultsCacheKey, out PredictionResultsResponse predictionResults))
        {
            tasks.Add(Clients.Caller.SendAsync("ReceivePredictionResults", predictionResults));
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

    public async Task SendMatches(List<ClientMatch> matches)
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
        if (_cache.TryGetValue(_predictionDataCacheKey, out object predictionDataObj))
        {
            // Verify the correct type is cached
            if (predictionDataObj is PredictionDataResponse predictionData)
            {
                _logger.LogDebug("Sending cached prediction data with {Count} matches on demand", 
                    predictionData.Data?.UpcomingMatches?.Count ?? 0);
                
                // Send to the client that requested it
                await Clients.Caller.SendAsync("ReceivePredictionData", predictionData);
            }
            else
            {
                _logger.LogWarning("Cached prediction data has incorrect type: {ActualType}. Expected PredictionDataResponse.", 
                    predictionDataObj?.GetType().Name ?? "null");
                
                // Try to be backwards compatible
                if (predictionDataObj != null)
                {
                    // If it's a list of matches, try to format it properly
                    if (predictionDataObj is System.Collections.Generic.List<UpcomingMatch> matches && matches.Any())
                    {
                        _logger.LogInformation("Converting raw match list to proper PredictionDataResponse format");
                        
                        // Create a properly formatted response
                        var currentTime = DateTime.UtcNow;
                        var formattedResponse = new PredictionDataResponse
                        {
                            Data = new PredictionData
                            {
                                UpcomingMatches = matches,
                                Metadata = new PredictionMetadata
                                {
                                    Total = matches.Count,
                                    Date = currentTime.ToString("yyyy-MM-dd"),
                                    LastUpdated = currentTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                                    LeagueData = new System.Collections.Generic.Dictionary<string, LeagueMetadata>()
                                }
                            },
                            Pagination = new PaginationInfo
                            {
                                CurrentPage = 1,
                                TotalPages = 1,
                                PageSize = matches.Count,
                                TotalItems = matches.Count,
                                HasNext = false,
                                HasPrevious = false
                            }
                        };
                        
                        // Update the cache with the properly formatted response
                        _cache.Set(_predictionDataCacheKey, formattedResponse, TimeSpan.FromHours(2));
                        
                        // Send the formatted response
                        await Clients.Caller.SendAsync("ReceivePredictionData", formattedResponse);
                        return;
                    }
                
                    await Clients.Caller.SendAsync("ReceivePredictionData", predictionDataObj);
                    
                    // Log this situation as it indicates a problem in the system
                    _logger.LogWarning("Sent incorrectly formatted prediction data. This may cause client-side errors.");
                }
                else
                {
                    await Clients.Caller.SendAsync("Error", "Prediction data format incorrect");
                }
            }
        }
        else
        {
            // Nothing in cache
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
    // NEW: Add method to request prediction results
    public async Task RequestPredictionResults()
    {
        try
        {
            if (_cache.TryGetValue(_predictionResultsCacheKey, out PredictionResultsResponse predictionResults))
            {
                await Clients.Caller.SendAsync("ReceivePredictionResults", predictionResults);
                _logger.LogDebug("Sent prediction results to client {ConnectionId}", Context.ConnectionId);
            }
            else
            {
                await Clients.Caller.SendAsync("Error", "Prediction results not available");
                _logger.LogWarning("Prediction results requested but not available for client {ConnectionId}", Context.ConnectionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending prediction results to client {ConnectionId}", Context.ConnectionId);
            await Clients.Caller.SendAsync("Error", "Failed to send prediction results");
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