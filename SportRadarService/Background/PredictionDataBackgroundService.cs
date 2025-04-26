using System.Collections.Concurrent;
using fredapi.Database;
using fredapi.Routes;
using fredapi.SignalR;
using fredapi.SportRadarService.TokenService;
using fredapi.SportRadarService.Transformers;
using fredapi.Utils;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Driver.Linq;

namespace fredapi.SportRadarService.Background;

public class PredictionDataBackgroundService : BackgroundService
{
    private readonly ILogger<PredictionDataBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMemoryCache _cache;
    private readonly PredictionResultsService _predictionResultsService;


    // Constants for better management
    private const string CACHE_KEY_PREDICTION_DATA = "prediction_data";
    private const string CACHE_KEY_MATCH_PREFIX = "prediction_match_";
    private const string CACHE_KEY_UPDATE_LOCK = "prediction_data_update_lock";
    private const int CONCURRENT_TRANSFORMATIONS = 4;

    // Configurable intervals
    private readonly TimeSpan _updateInterval = TimeSpan.FromHours(1);
    private readonly TimeSpan _resultProcessingInterval = TimeSpan.FromMinutes(15);

    // Semaphore to control concurrent transformations
    private static readonly SemaphoreSlim _transformSemaphore = new(CONCURRENT_TRANSFORMATIONS);

    // Semaphore to prevent concurrent prediction data updates
    private static readonly SemaphoreSlim _updateLockSemaphore = new(1, 1);

    public PredictionDataBackgroundService(
        ILogger<PredictionDataBackgroundService> logger,
        IServiceProvider serviceProvider,
        IMemoryCache cache,
        PredictionResultsService predictionResultsService)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _cache = cache;
        _predictionResultsService = predictionResultsService;
    }

// Modified UpdatePredictionDataAsync method in PredictionDataBackgroundService.cs
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Track when we last processed prediction results
        DateTime lastResultsProcessingTime = DateTime.MinValue;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Update prediction data (existing functionality)
                await UpdatePredictionDataAsync(stoppingToken);

                // Determine if we need to process prediction results
                var timeElapsedSinceLastProcessing = DateTime.UtcNow - lastResultsProcessingTime;
                if (timeElapsedSinceLastProcessing >= _resultProcessingInterval)
                {
                    // Process prediction results for completed matches
                    await ProcessPredictionResultsAsync(stoppingToken);
                    lastResultsProcessingTime = DateTime.UtcNow;
                }
                else
                {
                    _logger.LogInformation(
                        $"Skipping prediction results processing. Next run in {(_resultProcessingInterval - timeElapsedSinceLastProcessing).TotalMinutes:F1} minutes");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating prediction data");
            }

            // Wait for the next update interval with cancellation support
            try
            {
                await Task.Delay(_updateInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Prediction data background service is shutting down");
                break;
            }
        }
    }

// Modified UpdatePredictionDataAsync method in PredictionDataBackgroundService.cs
private async Task UpdatePredictionDataAsync(CancellationToken stoppingToken)
{
    // Use a lock to prevent multiple concurrent updates
    if (await _updateLockSemaphore.WaitAsync(0, stoppingToken) == false)
    {
        _logger.LogInformation("Another prediction data update is already in progress. Skipping this run.");
        return;
    }
    
    try
    {
        using var scope = _serviceProvider.CreateScope();
        var mongoDbService = scope.ServiceProvider.GetRequiredService<MongoDbService>();
        var transformer = scope.ServiceProvider.GetRequiredService<SportMatchesPredictionTransformer>();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<LiveMatchHub>>();
        
        // Get token first to avoid unnecessary DB queries if token fails
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        try
        {
            await tokenService.GetSportRadarToken();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get SportRadar token, skipping prediction data update");
            return;
        }

        // Look for matches up to 5 hours in the past and 1 day in the future
        var startTime = DateTime.UtcNow.AddHours(-5);  
        var endTime = DateTime.UtcNow.AddDays(1);

        // Create time windows to process matches in smaller chunks rather than all at once
        var timeWindows = new List<(DateTime start, DateTime end)>
        {
            (startTime, startTime.AddHours(6)),                  // Past matches + next 1 hour
            (startTime.AddHours(6), startTime.AddHours(12)),     // Next 1-7 hours
            (startTime.AddHours(12), endTime)                    // Remaining future matches
        };
        
        var collection = mongoDbService.GetCollection<MongoEnrichedMatch>("EnrichedSportMatches");
        
        // Prepare to collect all transformed matches
        var allTransformedMatches = new ConcurrentBag<UpcomingMatch>();
        var metadata = new PredictionMetadata
        {
            Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            LeagueData = new Dictionary<string, Transformers.LeagueMetadata>()
        };
        
        // Process each time window separately and send data incrementally
        int windowIndex = 0;
        int totalMatchCount = 0;
        int totalProcessedMatches = 0;
        
        foreach (var (windowStart, windowEnd) in timeWindows)
        {
            windowIndex++;
            _logger.LogInformation($"Processing time window {windowIndex}: {windowStart:yyyy-MM-dd HH:mm} to {windowEnd:yyyy-MM-dd HH:mm}");
            
            // Create filter for this time window
            var timeFilter = Builders<MongoEnrichedMatch>.Filter.And(
                Builders<MongoEnrichedMatch>.Filter.Gte(m => m.MatchTime, windowStart),
                Builders<MongoEnrichedMatch>.Filter.Lt(m => m.MatchTime, windowEnd)
            );
            
            // Use projection to only fetch needed fields
            var projection = Builders<MongoEnrichedMatch>.Projection
                .Include(m => m.MatchTime)
                .Include(m => m.MatchId)
                .Include(m => m.OriginalMatch)
                .Include(m => m.SeasonId)
                .Include(m => m.Team1LastX)
                .Include(m => m.TeamVersusRecent)
                .Include(m => m.TeamTableSlice)
                .Include(m => m.LastXStatsTeam1)
                .Include(m => m.LastXStatsTeam2)
                .Include(m => m.Team2LastX);
            
            // Find matches for this time window
            var matches = await collection.Find(timeFilter)
                .Project<MongoEnrichedMatch>(projection)
                .Sort(Builders<MongoEnrichedMatch>.Sort.Ascending(m => m.MatchTime))
                .ToListAsync(stoppingToken);
            
            _logger.LogInformation($"Found {matches.Count} matches in time window {windowIndex}");
            
            if (matches.Count == 0)
            {
                continue; // Skip empty windows
            }
            
            totalMatchCount += matches.Count;
            
            // Transform matches to enriched format with cache utilization
            var enrichedMatches = new List<EnrichedSportMatch>();
            foreach (var match in matches)
            {
                // Try to get from cache first
                var cacheKey = $"{CACHE_KEY_MATCH_PREFIX}{match.MatchId}";
                if (!_cache.TryGetValue(cacheKey, out EnrichedSportMatch? enrichedMatch))
                {
                    enrichedMatch = match.ToEnrichedSportMatch();
                    if (enrichedMatch != null)
                    {
                        // Cache for 2 hours to reduce transformation overhead
                        _cache.Set(cacheKey, enrichedMatch, TimeSpan.FromHours(2));
                    }
                }
                
                if (enrichedMatch != null)
                {
                    enrichedMatches.Add(enrichedMatch);
                }
            }
            
            _logger.LogInformation($"Transformed {enrichedMatches.Count} matches for time window {windowIndex}");
            
            if (enrichedMatches.Count == 0)
            {
                continue; // Skip if no valid enriched matches
            }
            
            // Transform matches in smaller batches and send immediately when ready
            const int BATCH_SIZE = 20; // Smaller batches for more responsiveness
            var batches = enrichedMatches
                .Chunk(BATCH_SIZE)
                .ToList();
            
            _logger.LogInformation($"Processing {batches.Count} batches for time window {windowIndex}");
            
            // Process each batch sequentially to avoid overwhelming the system
            foreach (var batch in batches)
            {
                try
                {
                    // Use a shorter timeout for transformation (30 seconds instead of 90)
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, stoppingToken);
                    
                    var batchResult = transformer.TransformToPredictionData(batch.ToList());
                    
                    if (batchResult?.Data?.UpcomingMatches == null || !batchResult.Data.UpcomingMatches.Any())
                    {
                        _logger.LogWarning($"No prediction data generated for batch of {batch.Length} matches");
                        continue;
                    }
                    
                    // Add matches to our overall collection
                    foreach (var match in batchResult.Data.UpcomingMatches)
                    {
                        allTransformedMatches.Add(match);
                    }
                    
                    // Get current batch matches
                    var batchMatches = batchResult.Data.UpcomingMatches.ToList();
                    totalProcessedMatches += batchMatches.Count;
                    
                    // IMPORTANT: Send this batch immediately to clients
                    var batchData = new PredictionDataResponse
                    {
                        Data = new PredictionData
                        {
                            UpcomingMatches = batchMatches,
                            Metadata = new PredictionMetadata
                            {
                                Total = totalMatchCount, // Total known so far
                                Date = metadata.Date,
                                LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                                LeagueData = metadata.LeagueData,
                            }
                        },
                        Pagination = new Transformers.PaginationInfo
                        {
                            CurrentPage = windowIndex,
                            TotalPages = timeWindows.Count,
                            PageSize = BATCH_SIZE,
                            TotalItems = totalMatchCount,
                            HasNext = true, // More data is coming
                        }
                    };
                    
                    // Small delay to avoid flooding clients
                    await Task.Delay(100, stoppingToken);
                    
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning($"Transformation timed out for batch of {batch.Length} matches");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error transforming batch of {batch.Length} matches");
                }
            }
        }
        
        // Cache the full list of transformed matches
        _cache.Set(CACHE_KEY_PREDICTION_DATA, allTransformedMatches.ToList(), TimeSpan.FromHours(2));
        
        // Send a final complete message
        var finalData = new PredictionDataResponse
        {
            Data = new PredictionData
            {
                UpcomingMatches = new List<UpcomingMatch>(), // Empty list, just sending metadata
                Metadata = new PredictionMetadata
                {
                    Total = totalMatchCount,
                    Date = metadata.Date,
                    LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    LeagueData = metadata.LeagueData,
                }
            },
            Pagination = new Transformers.PaginationInfo
            {
                CurrentPage = timeWindows.Count,
                TotalPages = timeWindows.Count,
                TotalItems = totalMatchCount,
                HasNext = false, // No more data
            }
        };
        
        // Send final completion message
        await hubContext.Clients.All.SendAsync("ReceivePredictionData", finalData, stoppingToken);
        
        _logger.LogInformation($"Successfully processed and sent {totalProcessedMatches} matches in {timeWindows.Count} time windows");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error updating prediction data");
    }
    finally
    {
        _updateLockSemaphore.Release();
    }
}
private FilterDefinition<MongoEnrichedMatch> CreateFilter(DateTime rangeStart, DateTime rangeEnd)
    {
        // CHANGE: Modified filter to be less restrictive - remove SRL filtering
        // since it appears to be filtering too many matches
        return Builders<MongoEnrichedMatch>.Filter.And(
            Builders<MongoEnrichedMatch>.Filter.Gte(m => m.MatchTime, rangeStart),
            Builders<MongoEnrichedMatch>.Filter.Lt(m => m.MatchTime, rangeEnd)
            // Removed the SRL filter to include more matches
        );
    }

    private IEnumerable<(DateTime start, DateTime end)> GetTimeRanges(DateTime start, DateTime end, int chunks)
    {
        var totalHours = (end - start).TotalHours;
        var chunkSize = totalHours / chunks;

        for (int i = 0; i < chunks; i++)
        {
            var chunkStart = start.AddHours(i * chunkSize);
            var chunkEnd = (i == chunks - 1) ? end : start.AddHours((i + 1) * chunkSize);
            yield return (chunkStart, chunkEnd);
        }
    }

    private async Task ProcessPredictionResultsAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Processing prediction results for completed matches");

            // Use a lock to prevent multiple concurrent processing
            if (_cache.TryGetValue(CACHE_KEY_UPDATE_LOCK, out _))
            {
                _logger.LogInformation("Another prediction data update process is already running. Skipping this run.");
                return;
            }

            try
            {
                // Set a lock in the cache to prevent concurrent updates
                _cache.Set(CACHE_KEY_UPDATE_LOCK, true, TimeSpan.FromMinutes(5));

                // Process completed matches from the last 24 hours
                await _predictionResultsService.ProcessCompletedMatchesAsync(stoppingToken);
            }
            finally
            {
                // Remove the lock when done
                _cache.Remove(CACHE_KEY_UPDATE_LOCK);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing prediction results");
        }
    }
}