using System.Text.Json;
using fredapi.Database;
using fredapi.Hubs;
using fredapi.Routes;
using fredapi.SportRadarService.Transformers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace fredapi.SportRadarService.Background;

public class PredictionDataBackgroundService : BackgroundService
{
    private readonly ILogger<PredictionDataBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _updateInterval = TimeSpan.FromHours(3);
    private readonly TimeSpan _cacheDuration = TimeSpan.FromHours(3);

    public PredictionDataBackgroundService(
        ILogger<PredictionDataBackgroundService> logger,
        IServiceProvider serviceProvider,
        IMemoryCache cache)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _cache = cache;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Starting prediction data update at: {time}", DateTimeOffset.Now);

                using var scope = _serviceProvider.CreateScope();
                var mongoDbService = scope.ServiceProvider.GetRequiredService<MongoDbService>();
                var transformer = scope.ServiceProvider.GetRequiredService<SportMatchesPredictionTransformer>();
                var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<SportMatchHub>>();

                // Get all matches for the next 24 hours
                var startTime = DateTime.UtcNow.AddMinutes(-90);
                var endTime = DateTime.UtcNow.AddHours(24);

                var collection = mongoDbService.GetCollection<MongoEnrichedMatch>("EnrichedSportMatches");
                var filter = Builders<MongoEnrichedMatch>.Filter.And(
                    Builders<MongoEnrichedMatch>.Filter.Gte(m => m.MatchTime, startTime),
                    Builders<MongoEnrichedMatch>.Filter.Lte(m => m.MatchTime, endTime),
                    Builders<MongoEnrichedMatch>.Filter.Not(
                        Builders<MongoEnrichedMatch>.Filter.Or(
                            Builders<MongoEnrichedMatch>.Filter.Regex(m => m.OriginalMatch.Teams.Home.Name, new BsonRegularExpression("SRL", "i")),
                            Builders<MongoEnrichedMatch>.Filter.Regex(m => m.OriginalMatch.Teams.Away.Name, new BsonRegularExpression("SRL", "i"))
                        )
                    )
                );

                var matches = await collection
                    .Find(filter)
                    .SortBy(m => m.MatchTime)
                    .ToListAsync(stoppingToken);

                if (!matches.Any())
                {
                    _logger.LogInformation("No matches found in the specified time range");
                    return;
                }

                // Transform matches to enriched format
                var enrichedMatches = matches
                    .Select(m => m.ToEnrichedSportMatch())
                    .Where(m => m != null)
                    .ToList();

                // Process matches in batches
                const int batchSize = 20;
                var batches = enrichedMatches
                    .Select((match, index) => new { match, index })
                    .GroupBy(x => x.index / batchSize)
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
                        _logger.LogWarning($"Batch processing timed out for {batch.Count} matches");
                        return null;
                    }
                });

                var batchResults = (await Task.WhenAll(batchTasks)).Where(r => r != null).ToList();

                if (!batchResults.Any())
                {
                    _logger.LogError("No valid prediction data generated");
                    return;
                }

                // Combine all batch results
                var predictionData = new fredapi.SportRadarService.Transformers.PredictionDataResponse
                {
                    Data = new fredapi.SportRadarService.Transformers.PredictionData
                    {
                        UpcomingMatches = batchResults.SelectMany(r => r.Data.UpcomingMatches).ToList(),
                        Metadata = new fredapi.SportRadarService.Transformers.PredictionMetadata
                        {
                            Total = enrichedMatches.Count,
                            Date = DateTime.Now.ToString("yyyy-MM-dd"),
                            LeagueData = batchResults
                                .SelectMany(r => r.Data.Metadata.LeagueData)
                                .GroupBy(x => x.Key)
                                .ToDictionary(
                                    g => g.Key,
                                    g => g.First().Value
                                )
                        }
                    },
                    Pagination = new fredapi.SportRadarService.Transformers.PaginationInfo
                    {
                        CurrentPage = 1,
                        TotalPages = 1,
                        PageSize = enrichedMatches.Count,
                        TotalItems = enrichedMatches.Count,
                        HasNext = false,
                        HasPrevious = false
                    }
                };

                // Cache the result
                _cache.Set("prediction_data", predictionData, _cacheDuration);

                // Broadcast to all connected clients
                await hubContext.Clients.All.SendAsync("ReceivePredictionData", predictionData, stoppingToken);

                _logger.LogInformation("Prediction data update completed at: {time}", DateTimeOffset.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating prediction data");
            }

            // Wait for the next update interval
            await Task.Delay(_updateInterval, stoppingToken);
        }
    }
}