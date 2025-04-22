using fredapi.Database;
using fredapi.Routes;
using fredapi.SignalR;
using fredapi.SportRadarService.Transformers;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Extensions.Caching.Memory;

namespace fredapi.SportRadarService.Background;

public class PredictionDataBackgroundService(
    ILogger<PredictionDataBackgroundService> logger,
    IServiceProvider serviceProvider,
    IMemoryCache cache)
    : BackgroundService
{
    private readonly TimeSpan _updateInterval = TimeSpan.FromHours(1);
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var mongoDbService = scope.ServiceProvider.GetRequiredService<MongoDbService>();
                var transformer = scope.ServiceProvider.GetRequiredService<SportMatchesPredictionTransformer>();
                var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<LiveMatchHub>>();

                // Get all matches for the next 24 hours
                var startTime = DateTime.UtcNow.AddMinutes(-300);
                var endTime = DateTime.UtcNow.AddHours(24);

                var collection = mongoDbService.GetCollection<MongoEnrichedMatch>("EnrichedSportMatches");
                
                // Split the query into smaller date ranges to process in separate queries
                var timeRanges = GetTimeRanges(startTime, endTime, 4); // Split into 4 chunks
                var allMatches = new List<MongoEnrichedMatch>();
                
                // Process each time range separately
                foreach (var (rangeStart, rangeEnd) in timeRanges)
                {
                    try
                    {
                        var filter = CreateFilter(rangeStart, rangeEnd);
                        
                        // Use Find but with explicit timeout and lower batch size
                        var options = new FindOptions<MongoEnrichedMatch>
                        {
                            BatchSize = 50,
                            MaxTime = TimeSpan.FromSeconds(30)
                        };
                        
                        using var cursor = await collection.FindAsync(filter, options, stoppingToken);
                        var rangeMatches = await cursor.ToListAsync(stoppingToken);
                        allMatches.AddRange(rangeMatches);
                        
                        logger.LogInformation($"Retrieved {rangeMatches.Count} matches between {rangeStart} and {rangeEnd}");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Error retrieving matches between {rangeStart} and {rangeEnd}");
                        // Continue with next range even if this one fails
                    }
                }

                if (!allMatches.Any())
                {
                    logger.LogInformation("No upcoming matches found");
                    await Task.Delay(_updateInterval, stoppingToken);
                    continue;
                }

                // Sort the matches after combining all chunks
                allMatches = allMatches.OrderBy(m => m.MatchTime).ToList();

                // Transform matches to enriched format
                var enrichedMatches = allMatches
                    .Select(m => m.ToEnrichedSportMatch())
                    .Where(m => m != null)
                    .ToList();

                // Process matches in batches
                const int transformBatchSize = 20;
                var batches = enrichedMatches
                    .Select((match, index) => new { match, index })
                    .GroupBy(x => x.index / transformBatchSize)
                    .Select(g => g.Select(x => x.match).ToList())
                    .ToList();

                // Process batches concurrently
                var batchTasks = batches.Select(async batch =>
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    try
                    {
                        return await Task.Run(() => transformer.TransformToPredictionData(batch), cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        logger.LogWarning($"Batch processing timed out for {batch.Count} matches");
                        return null;
                    }
                });

                var batchResults = (await Task.WhenAll(batchTasks)).Where(r => r != null).ToList();

                if (!batchResults.Any())
                {
                    logger.LogError("No valid prediction data generated");
                    await Task.Delay(_updateInterval, stoppingToken);
                    continue;
                }

                var currentTime = DateTime.UtcNow;

                // Combine all batch results
                var predictionData = new PredictionDataResponse
                {
                    Data = new PredictionData
                    {
                        UpcomingMatches = batchResults.SelectMany(r => r.Data.UpcomingMatches).ToList(),
                        Metadata = new PredictionMetadata
                        {
                            Total = enrichedMatches.Count,
                            Date = currentTime.ToString("yyyy-MM-dd"),
                            LastUpdated = currentTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                            LeagueData = batchResults
                                .SelectMany(r => r.Data.Metadata.LeagueData)
                                .GroupBy(x => x.Key)
                                .ToDictionary(
                                    g => g.Key,
                                    g => g.First().Value
                                )
                        }
                    },
                    Pagination = new Transformers.PaginationInfo
                    {
                        CurrentPage = 1,
                        TotalPages = 1,
                        PageSize = enrichedMatches.Count,
                        TotalItems = enrichedMatches.Count,
                        HasNext = false,
                        HasPrevious = false
                    }
                };

                // Cache the prediction data before broadcasting
                cache.Set("prediction_data", predictionData, TimeSpan.FromHours(1));

                // Broadcast to all connected clients
                await hubContext.Clients.All.SendAsync("ReceivePredictionData", predictionData, stoppingToken);
                
                logger.LogInformation($"Successfully updated prediction data for {enrichedMatches.Count} matches");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while updating prediction data");
            }

            // Wait for the next update interval
            await Task.Delay(_updateInterval, stoppingToken);
        }
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