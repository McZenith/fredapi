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

        // CHANGE: Expanded date range significantly to ensure we get ALL matches
        // Look for matches up to 7 days in the past and 30 days in the future
        var startTime = DateTime.UtcNow.AddHours(-5);  
        var endTime = DateTime.UtcNow.AddDays(1);    

        var collection = mongoDbService.GetCollection<MongoEnrichedMatch>("EnrichedSportMatches");
        
        // Get a count of all matches in this date range (with no other filters)
        var dateRangeFilter = Builders<MongoEnrichedMatch>.Filter.And(
            Builders<MongoEnrichedMatch>.Filter.Gte(m => m.MatchTime, startTime),
            Builders<MongoEnrichedMatch>.Filter.Lt(m => m.MatchTime, endTime)
        );
        
        var totalMatchCount = 0;
        try
        {
            totalMatchCount = (int)await collection.CountDocumentsAsync(dateRangeFilter, 
                new CountOptions { MaxTime = TimeSpan.FromSeconds(20) }, 
                stoppingToken);
                
            _logger.LogInformation($"Found {totalMatchCount} total matches in date range");
            
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
        
        // CRITICAL CHANGE: Use direct pagination instead of time ranges
        // This ensures we get ALL matches without missing any between chunks
        var allMatches = new List<MongoEnrichedMatch>();
        const int PAGE_SIZE = 200; // Fetch 200 at a time
        
        _logger.LogInformation($"Fetching all {totalMatchCount} matches with pagination (page size: {PAGE_SIZE})");
        
        // Use explicit pagination with skip/limit
        int pageCount = (int)Math.Ceiling(totalMatchCount / (double)PAGE_SIZE);
        for (int page = 0; page < pageCount; page++)
        {
            try
            {
                var skipCount = page * PAGE_SIZE;
                
                _logger.LogInformation($"Fetching page {page+1}/{pageCount} (skip: {skipCount}, limit: {PAGE_SIZE})");
                
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
                
                // Execute the paginated query
                var pageMatches = await collection.Find(dateRangeFilter)
                    .Project<MongoEnrichedMatch>(projection)
                    .Sort(Builders<MongoEnrichedMatch>.Sort.Ascending(m => m.MatchTime))
                    .Skip(skipCount)
                    .Limit(PAGE_SIZE)
                    .ToListAsync(stoppingToken);
                
                _logger.LogInformation($"Retrieved {pageMatches.Count} matches for page {page+1}");
                allMatches.AddRange(pageMatches);
                
                // Break if we received fewer than expected (last page)
                if (pageMatches.Count < PAGE_SIZE)
                    break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving matches for page {page+1}");
                // Continue with next page even if this one fails
            }
        }

        // Check if we have matches to process
        if (allMatches.Count == 0)
        {
            _logger.LogInformation("No upcoming matches found after fetching all pages");
            return;
        }

        _logger.LogInformation($"Successfully retrieved {allMatches.Count} matches out of {totalMatchCount} total matches");

        // Transform matches to enriched format with null check and caching
        var enrichedMatches = new List<EnrichedSportMatch>();
        foreach (var match in allMatches)
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

        _logger.LogInformation($"Successfully transformed {enrichedMatches.Count} out of {allMatches.Count} matches");

        if (!enrichedMatches.Any())
        {
            _logger.LogWarning("No valid enriched matches after transformation");
            return;
        }

        // CHANGE: Implement a paged approach to sending prediction data
        // This ensures we can handle large numbers of matches without timeout issues
        const int MAX_MATCHES_PER_RESPONSE = 200;
        
        // Calculate total number of pages needed
        int totalPages = (int)Math.Ceiling(enrichedMatches.Count / (double)MAX_MATCHES_PER_RESPONSE);
        _logger.LogInformation($"Will send prediction data in {totalPages} pages of max {MAX_MATCHES_PER_RESPONSE} matches each");
        
        // Process all matches, but send them in pages
        // Process matches in batches to avoid memory pressure
        const int TRANSFORM_BATCH_SIZE = 100;
        var transformBatches = enrichedMatches
            .Chunk(TRANSFORM_BATCH_SIZE)
            .ToList();

        // Use ConcurrentBag to safely collect results from parallel operations
        var allTransformedMatches = new ConcurrentBag<UpcomingMatch>();
        var transformTasks = new List<Task>();
        
        // Create a progress tracker
        var completedBatches = 0;
        var totalBatches = transformBatches.Count;

        // Process transform batches in parallel
        foreach (var batch in transformBatches)
        {
            // Use semaphore to limit concurrent transformations
            var task = Task.Run(async () =>
            {
                await _transformSemaphore.WaitAsync(stoppingToken);
                
                try
                {
                    // Increased timeout to 90 seconds
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));
                    var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, stoppingToken);
                    
                    var batchResult = await Task.Run(() => transformer.TransformToPredictionData(batch.ToList()), linkedCts.Token);
                    if (batchResult != null && batchResult.Data?.UpcomingMatches?.Any() == true)
                    {
                        // Add each match to our collection
                        foreach (var match in batchResult.Data.UpcomingMatches)
                        {
                            allTransformedMatches.Add(match);
                        }
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

        // Convert to list and sort by match time
        var allMatchesList = allTransformedMatches.ToList();
        
        _logger.LogInformation($"Successfully transformed {allMatchesList.Count} matches out of {enrichedMatches.Count} enriched matches");

        if (!allMatchesList.Any())
        {
            _logger.LogError("No valid prediction data generated after transformation");
            return;
        }

        // Get metadata from all matches
        var currentTime = DateTime.UtcNow;
        var leagueData = new ConcurrentDictionary<string, Transformers.LeagueMetadata>();
        
        // Create metadata once for all pages
        var metadata = new PredictionMetadata
        {
            Total = allMatchesList.Count,
            Date = currentTime.ToString("yyyy-MM-dd"),
            LastUpdated = currentTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            LeagueData = leagueData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        };

        // Cache the FULL list of transformed matches
        _cache.Set(CACHE_KEY_PREDICTION_DATA, allMatchesList, TimeSpan.FromHours(2));
        
        // If we have more pages, send them with a small delay between
        if (totalPages > 1)
        {
            // Small delay to allow clients to process first page
            await Task.Delay(500, stoppingToken);
            
            // Send remaining pages
            for (int page = 1; page < totalPages; page++)
            {
                var pageMatches = allMatchesList
                    .ToList();
                
                var pageData = new PredictionDataResponse
                {
                    Data = new PredictionData
                    {
                        UpcomingMatches = pageMatches,
                        Metadata = metadata 
                    },
                    Pagination = new Transformers.PaginationInfo
                    {
                        CurrentPage = totalPages,
                        TotalPages = totalPages,
                        PageSize = MAX_MATCHES_PER_RESPONSE,
                        TotalItems = allMatchesList.Count,
                        HasNext = page < totalPages - 1,
                        HasPrevious = true
                    }
                };
                
                // Send the next page
                await hubContext.Clients.All.SendAsync("ReceivePredictionDataPage", pageData, stoppingToken);
                
                _logger.LogInformation($"Sent page {page+1}/{totalPages} with {pageMatches.Count} matches to clients");
                
                // Small delay to allow clients to process each page
                if (page < totalPages - 1)
                {
                    await Task.Delay(500, stoppingToken);
                }
            }
        }
        
        _logger.LogInformation($"Successfully updated prediction data for {allMatchesList.Count} matches in {totalPages} pages");
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