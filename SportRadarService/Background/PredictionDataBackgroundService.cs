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
    private const int BATCH_SIZE = 50;
    private const int TRANSFORM_BATCH_SIZE = 20;
    private const int CONCURRENT_TRANSFORMATIONS = 4;
    
    // Configurable intervals
    private readonly TimeSpan _updateInterval = TimeSpan.FromHours(1);
    private readonly TimeSpan _dbQueryTimeout = TimeSpan.FromSeconds(30);
    private readonly TimeSpan _transformTimeout = TimeSpan.FromSeconds(20);
    
    // Semaphore to control concurrent transformations
    private static readonly SemaphoreSlim _transformSemaphore = new(CONCURRENT_TRANSFORMATIONS);

    public PredictionDataBackgroundService(
        ILogger<PredictionDataBackgroundService> logger,
        IServiceProvider serviceProvider,
        IMemoryCache cache,
        PredictionResultsService predictionResultsService) // Add this parameter
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _cache = cache;
        _predictionResultsService = predictionResultsService; // Initialize the field
    }

    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Update prediction data (existing functionality)
                await UpdatePredictionDataAsync(stoppingToken);
            
                // New: Process prediction results for completed matches
                await ProcessPredictionResultsAsync(stoppingToken);
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
    
    private async Task ProcessPredictionResultsAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Processing prediction results for completed matches");
        
            // Process completed matches from the last 24 hours
            await _predictionResultsService.ProcessCompletedMatchesAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing prediction results");
        }
    }

    
    private async Task UpdatePredictionDataAsync(CancellationToken stoppingToken)
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

        // Get matches for the next 24 hours and previous 5 hours (to include ongoing matches)
        var startTime = DateTime.UtcNow.AddMinutes(-300);
        var endTime = DateTime.UtcNow.AddHours(24);

        var collection = mongoDbService.GetCollection<MongoEnrichedMatch>("EnrichedSportMatches");
        
        // Cache the total count with a separate light query first
        var countFilter = CreateFilter(startTime, endTime);
        var totalMatchCount = 0;
        
        try
        {
            totalMatchCount = (int)await collection.CountDocumentsAsync(countFilter, 
                new CountOptions { MaxTime = TimeSpan.FromSeconds(10) }, 
                stoppingToken);
                
            _logger.LogInformation($"Found {totalMatchCount} matches in date range");
            
            if (totalMatchCount == 0)
            {
                _logger.LogInformation("No upcoming matches found");
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get match count, proceeding with main query");
        }
        
        // Use concurrent collection for thread safety when gathering results from multiple queries
        var allMatches = new ConcurrentBag<MongoEnrichedMatch>();
        
        // Split the query into smaller date ranges to process in parallel
        var timeRanges = GetTimeRanges(startTime, endTime, 6); // Increased to 6 chunks for better parallelism
        var queryTasks = new List<Task>();
        
        // Process time ranges in parallel
        foreach (var (rangeStart, rangeEnd) in timeRanges)
        {
            var task = Task.Run(async () =>
            {
                try
                {
                    var filter = CreateFilter(rangeStart, rangeEnd);
                    
                    // Use optimized find with index hint if available
                    var options = new FindOptions<MongoEnrichedMatch>
                    {
                        BatchSize = BATCH_SIZE,
                        MaxTime = _dbQueryTimeout,
                        AllowDiskUse = true,
                        NoCursorTimeout = false // Better for short-lived queries
                    };
                    
                    // Use projection to only fetch needed fields
                    var projection = Builders<MongoEnrichedMatch>.Projection
                        .Include(m => m.MatchTime)
                        .Include(m => m.MatchId)
                        .Include(m => m.OriginalMatch)
                        .Include(m => m.SeasonId)
                        .Include(m => m.MatchId)
                        .Include(m => m.Team1LastX)
                        .Include(m => m.TeamVersusRecent)
                        .Include(m => m.TeamTableSlice)
                        .Include(m => m.LastXStatsTeam1)
                        .Include(m => m.LastXStatsTeam2)
                        .Include(m => m.Team2LastX);
                    
                    // Use the MongoDB extension method we optimized earlier
                    using var cursor = await collection.FindWithDiskUse(filter, BATCH_SIZE)
                        .Project<MongoEnrichedMatch>(projection)
                        .ToCursorAsync(stoppingToken);
                        
                    // Stream results to reduce memory pressure
                    while (await cursor.MoveNextAsync(stoppingToken))
                    {
                        foreach (var match in cursor.Current)
                        {
                            allMatches.Add(match);
                        }
                    }
                    
                    _logger.LogInformation($"Retrieved matches between {rangeStart:yyyy-MM-dd HH:mm} and {rangeEnd:yyyy-MM-dd HH:mm}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error retrieving matches between {rangeStart} and {rangeEnd}");
                    // Continue with next range even if this one fails
                }
            }, stoppingToken);
            
            queryTasks.Add(task);
        }
        
        // Wait for all queries to finish
        await Task.WhenAll(queryTasks);

        // Check if we have matches to process
        if (allMatches.IsEmpty)
        {
            _logger.LogInformation("No upcoming matches found after filtering");
            return;
        }

        // Sort the matches after combining all chunks - use OrderBy for stable sorting
        var sortedMatches = allMatches.OrderBy(m => m.MatchTime).ToList();
        _logger.LogInformation($"Processing {sortedMatches.Count} matches for prediction data");

        // Transform matches to enriched format with null check and caching
        var enrichedMatches = new List<EnrichedSportMatch>();
        foreach (var match in sortedMatches)
        {
            // Try to get from cache first
            var cacheKey = $"{CACHE_KEY_MATCH_PREFIX}{match.MatchId}";
            if (!_cache.TryGetValue(cacheKey, out EnrichedSportMatch? enrichedMatch))
            {
                enrichedMatch = match.ToEnrichedSportMatch();
                if (enrichedMatch != null)
                {
                    // Cache for 30 minutes to reduce transformation overhead for frequent updates
                    _cache.Set(cacheKey, enrichedMatch, TimeSpan.FromMinutes(30));
                }
            }
            
            if (enrichedMatch != null)
            {
                enrichedMatches.Add(enrichedMatch);
            }
        }

        if (!enrichedMatches.Any())
        {
            _logger.LogWarning("No valid enriched matches after transformation");
            return;
        }

        // Process matches in smaller batches with a limited degree of parallelism
        var batches = enrichedMatches
            .Chunk(TRANSFORM_BATCH_SIZE)
            .ToList();

        // Use ConcurrentBag to safely collect results from parallel operations
        var batchResults = new ConcurrentBag<PredictionDataResponse>();
        var transformTasks = new List<Task>();
        
        // Create a progress tracker
        var completedBatches = 0;
        var totalBatches = batches.Count;

        foreach (var batch in batches)
        {
            // Use semaphore to limit concurrent transformations
            var task = Task.Run(async () =>
            {
                await _transformSemaphore.WaitAsync(stoppingToken);
                
                try
                {
                    using var cts = new CancellationTokenSource(_transformTimeout);
                    var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, stoppingToken);
                    
                    var batchResult = await Task.Run(() => transformer.TransformToPredictionData(batch.ToList()), linkedCts.Token);
                    if (batchResult != null && batchResult.Data?.UpcomingMatches?.Any() == true)
                    {
                        batchResults.Add(batchResult);
                    }
                    
                    // Update progress
                    Interlocked.Increment(ref completedBatches);
                    _logger.LogInformation($"Transformed batch {completedBatches}/{totalBatches} with {batch.Length} matches");
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning($"Batch transformation timed out for {batch.Length} matches");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error transforming batch of {batch.Length} matches");
                }
                finally
                {
                    _transformSemaphore.Release();
                }
            }, stoppingToken);
            
            transformTasks.Add(task);
        }

        // Wait for all transformations to complete
        await Task.WhenAll(transformTasks);

        if (!batchResults.Any())
        {
            _logger.LogError("No valid prediction data generated after transformation");
            return;
        }

        var currentTime = DateTime.UtcNow;

        // Combine all batch results efficiently
        // Since LeagueMetadata appears to be a concrete type, we'll use that type directly
        var leagueData = new ConcurrentDictionary<string, Transformers.LeagueMetadata>();

        foreach (var result in batchResults)
        {
            foreach (var kvp in result.Data.Metadata.LeagueData)
            {
                // Make sure we're using the right type
                leagueData[kvp.Key] = kvp.Value; // Using indexer instead of TryAdd
            }
        }

        // Create the final prediction data
        var upcomingMatches = batchResults.SelectMany(r => r.Data.UpcomingMatches).ToList();
        var predictionData = new PredictionDataResponse
        {
            Data = new PredictionData
            {
                UpcomingMatches = upcomingMatches,
                Metadata = new PredictionMetadata
                {
                    Total = upcomingMatches.Count,
                    Date = currentTime.ToString("yyyy-MM-dd"),
                    LastUpdated = currentTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    LeagueData = leagueData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                }
            },
            Pagination = new Transformers.PaginationInfo
            {
                CurrentPage = 1,
                TotalPages = 1,
                PageSize = upcomingMatches.Count,
                TotalItems = upcomingMatches.Count,
                HasNext = false,
                HasPrevious = false
            }
        };

        // Cache the prediction data before broadcasting
        _cache.Set(CACHE_KEY_PREDICTION_DATA, predictionData, _updateInterval);

        // Broadcast to all connected clients
        await hubContext.Clients.All.SendAsync("ReceivePredictionData", predictionData, stoppingToken);
        
        _logger.LogInformation($"Successfully updated prediction data for {upcomingMatches.Count} matches");
    }
    
    private FilterDefinition<MongoEnrichedMatch> CreateFilter(DateTime rangeStart, DateTime rangeEnd)
    {
        return Builders<MongoEnrichedMatch>.Filter.And(
            Builders<MongoEnrichedMatch>.Filter.Gte(m => m.MatchTime, rangeStart),
            Builders<MongoEnrichedMatch>.Filter.Lt(m => m.MatchTime, rangeEnd),
            Builders<MongoEnrichedMatch>.Filter.Not(
                Builders<MongoEnrichedMatch>.Filter.Or(
                    Builders<MongoEnrichedMatch>.Filter.Regex(m => m.OriginalMatch.Teams.Home.Name, new BsonRegularExpression("SRL", "i")),
                    Builders<MongoEnrichedMatch>.Filter.Regex(m => m.OriginalMatch.Teams.Away.Name, new BsonRegularExpression("SRL", "i"))
                )
            )
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
}