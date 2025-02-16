using System.Text.Json;
using System.Text.Json.Serialization;
using fredapi.Database;
using MongoDB.Bson;
using MongoDB.Driver;

namespace fredapi.SportRadarService.Background.UpcomingArbitrageBackgroundService;

public partial class UpcomingArbitrageBackgroundService : BackgroundService
{
    private readonly ILogger<UpcomingArbitrageBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly HttpClient _httpClient;
    private const int MaxRetries = 3;
    private const int PageSize = 100;
    private const int PageLimit = 9;
    private const decimal MaxAcceptableMargin = 10.0m;
    
    public UpcomingArbitrageBackgroundService(
        ILogger<UpcomingArbitrageBackgroundService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
        _httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var mongoDbService = scope.ServiceProvider.GetRequiredService<MongoDbService>();
                var arbitrageCollection = mongoDbService.GetCollection<ArbitrageMatch>("UpcomingArbitrageMatches");

                // Create TTL index for automatic document expiration
                var indexKeysDefinition = Builders<ArbitrageMatch>.IndexKeys.Ascending(x => x.CreatedAt);
                var indexOptions = new CreateIndexOptions { ExpireAfter = TimeSpan.FromHours(24) };
                await arbitrageCollection.Indexes.CreateOneAsync(
                    new CreateIndexModel<ArbitrageMatch>(indexKeysDefinition, indexOptions),
                    cancellationToken: stoppingToken);

                var matches = await FetchAllUpcomingMatchesAsync(stoppingToken);
                _logger.LogInformation($"Fetched {matches.Count} upcoming matches");

                var arbitrageMatches = ProcessArbitrageOpportunities(matches);
                _logger.LogInformation($"Found {arbitrageMatches.Count} matches with arbitrage opportunities");

                if (arbitrageMatches.Any())
                {
                    await StoreArbitrageMatchesAsync(arbitrageCollection, arbitrageMatches, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in arbitrage processing");
            }
            finally
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Arbitrage service is shutting down");
                }
            }
        }
    }

    private async Task<List<MatchData>> FetchAllUpcomingMatchesAsync(CancellationToken stoppingToken)
    {
        var allMatches = new List<MatchData>();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var pageNumbers = Enumerable.Range(1, PageLimit);
        var chunks = pageNumbers.Chunk(3); // Process 3 pages concurrently

        foreach (var chunk in chunks)
        {
            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                var tasks = chunk.Select(pageNum => FetchPageWithRetryAsync(pageNum, timestamp, stoppingToken));
                var results = await Task.WhenAll(tasks);

                foreach (var result in results)
                {
                    if (result?.Data?.Tournaments != null)
                    {
                        var matches = result.Data.Tournaments
                            .SelectMany(t => t.Events)
                            .ToList();
                        allMatches.AddRange(matches);
                    }
                }

                // Add human-like delay between chunks
                await Task.Delay(Random.Shared.Next(1000, 2000), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chunk of pages");
            }
        }

        return allMatches;
    }

    private async Task<ApiResponse?> FetchPageWithRetryAsync(int pageNum, long timestamp, CancellationToken stoppingToken)
    {
        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                if (attempt > 0)
                {
                    var jitter = Random.Shared.Next(0, 1000);
                    var delay = Math.Min(1000 * Math.Pow(2, attempt) + jitter, 10000);
                    await Task.Delay((int)delay, stoppingToken);
                }

                var url = $"https://www.sportybet.com/api/ng/factsCenter/pcUpcomingEvents?" +
                         $"sportId=sr%3Asport%3A1&marketId=1%2C18%2C19%2C20%2C10%2C29%2C11%2C26%2C36%2C14%2C60100" +
                         $"&pageSize={PageSize}&pageNum={pageNum}&option=1&timeline=24&_t={timestamp}";

                var response = await _httpClient.GetAsync(url, stoppingToken);
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound ||
                    response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    return null;
                }

                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync(stoppingToken);
                return JsonSerializer.Deserialize<ApiResponse>(content);
            }
            catch (Exception ex) when (attempt < MaxRetries - 1)
            {
                _logger.LogWarning(ex, $"Retry attempt {attempt + 1} failed for page {pageNum}");
            }
        }

        return null;
    }
}
public partial class UpcomingArbitrageBackgroundService
{
    private List<ArbitrageMatch> ProcessArbitrageOpportunities(List<MatchData> matches)
    {
        var arbitrageMatches = new List<ArbitrageMatch>();
        
        if (matches == null)
        {
            _logger.LogWarning("No matches provided to process");
            return arbitrageMatches;
        }

        foreach (var match in matches)
        {
            try
            {
                if (match == null || 
                    string.IsNullOrEmpty(match.EventId) || 
                    string.IsNullOrEmpty(match.HomeTeamId) || 
                    string.IsNullOrEmpty(match.AwayTeamId) ||
                    string.IsNullOrEmpty(match.HomeTeamName) ||
                    string.IsNullOrEmpty(match.AwayTeamName) ||
                    match.Markets == null)
                {
                    continue;
                }

                var arbitrageMarkets = match.Markets
                    .Where(m => m != null)
                    .Where(m => m.Status != 2 && m.Status != 3) // Exclude suspended and settled markets
                    .Where(m => m.Outcomes != null && m.Outcomes.Count is 2 or 3)
                    .Select(m =>
                    {
                        try
                        {
                            var validOutcomes = m.Outcomes
                                .Where(o => o != null && o.IsActive == 1 && !string.IsNullOrEmpty(o.Odds))
                                .Select(o =>
                                {
                                    try
                                    {
                                        return new Outcome
                                        {
                                            Id = o.Id ?? "",
                                            Description = o.Desc ?? "",
                                            Odds = decimal.TryParse(o.Odds, out var odds) ? odds : 0m
                                        };
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, "Error processing outcome in market {MarketId}", m.Id);
                                        return null;
                                    }
                                })
                                .Where(o => o != null && o.Odds > 0)
                                .ToList();

                            if (!validOutcomes.Any())
                            {
                                return null;
                            }

                            var market = new Market
                            {
                                Id = m.Id ?? "",
                                Description = m.Desc ?? "",
                                Specifier = m.Specifier ?? "",
                                Outcomes = validOutcomes
                            };

                            var (hasArbitrage, stakePercentages, profitPercentage) = CalculateArbitrageOpportunity(market);
                            if (hasArbitrage)
                            {
                                for (int i = 0; i < market.Outcomes.Count; i++)
                                {
                                    market.Outcomes[i].StakePercentage = stakePercentages[i];
                                }
                                market.Margin = CalculateMarginPercentage(market.Outcomes);
                                market.ProfitPercentage = profitPercentage;

                                _logger.LogInformation(
                                    "Found arbitrage opportunity in match {MatchId}, market {MarketId} with {ProfitPercentage}% profit",
                                    match.EventId,
                                    market.Id,
                                    market.ProfitPercentage);

                                return market;
                            }

                            return null;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error processing market {MarketId}", m.Id);
                            return null;
                        }
                    })
                    .Where(m => m != null)
                    .ToList();

                if (arbitrageMarkets.Any())
                {
                    try
                    {
                        var arbitrageMatch = new ArbitrageMatch
                        {
                            Id = ObjectId.GenerateNewId(),
                            MatchId = match.EventId,
                            Teams = new Teams
                            {
                                Home = new Team
                                {
                                    Id = match.HomeTeamId,
                                    Name = match.HomeTeamName
                                },
                                Away = new Team
                                {
                                    Id = match.AwayTeamId,
                                    Name = match.AwayTeamName
                                }
                            },
                            TournamentName = match.Tournament?.Name ?? "Unknown Tournament",
                            Markets = arbitrageMarkets,
                            CreatedAt = DateTime.UtcNow,
                            MatchTime = DateTime.TryParse(match.StartTime, out var matchTime) 
                                ? matchTime 
                                : DateTime.UtcNow
                        };

                        arbitrageMatches.Add(arbitrageMatch);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error creating arbitrage match for event {EventId}", match.EventId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error processing match {EventId}", match?.EventId ?? "unknown");
            }
        }

        _logger.LogInformation("Processed {TotalMatches} matches, found {ArbitrageMatches} with arbitrage opportunities",
            matches.Count,
            arbitrageMatches.Count);

        return arbitrageMatches;
    }

    private (bool hasArbitrage, List<decimal> stakePercentages, decimal profitPercentage) CalculateArbitrageOpportunity(Market market)
    {
        if (!market.Outcomes.Any()) return (false, new List<decimal>(), 0m);

        // Calculate margin but don't filter on it, just store it
        var margin = CalculateMarginPercentage(market.Outcomes);
        market.Margin = margin;  // Store the margin for reference

        // Calculate total inverse of odds
        var totalInverse = market.Outcomes.Sum(o => 1m / o.Odds);

        // Calculate profit percentage
        var profitPercentage = ((1 / totalInverse) - 1) * 100;

        // Only check for positive profit - this is our main arbitrage indicator
        if (profitPercentage <= 0) return (false, new List<decimal>(), 0m);

        // Calculate optimal stake percentages for guaranteed profit
        var stakePercentages = market.Outcomes
            .Select(o => (1m / o.Odds / totalInverse) * 100m)
            .Select(p => Math.Round(p, 2))
            .ToList();

        return (true, stakePercentages, Math.Round(profitPercentage, 2));
    }
    private decimal CalculateMarginPercentage(List<Outcome> outcomes)
    {
        var impliedProbabilities = outcomes.Select(o => 1m / o.Odds);
        var margin = (impliedProbabilities.Sum() - 1m) * 100;
        return margin;
    }

    private async Task StoreArbitrageMatchesAsync(
        IMongoCollection<ArbitrageMatch> collection,
        List<ArbitrageMatch> matches,
        CancellationToken stoppingToken)
    {
        try
        {
            var upsertModels = matches.Select(match =>
            {
                var filter = Builders<ArbitrageMatch>.Filter.Eq(x => x.MatchId, match.MatchId);
                // Create an update definition that excludes the _id field
                var update = Builders<ArbitrageMatch>.Update
                    .Set(x => x.Teams, match.Teams)
                    .Set(x => x.TournamentName, match.TournamentName)
                    .Set(x => x.Markets, match.Markets)
                    .Set(x => x.CreatedAt, match.CreatedAt)
                    .Set(x => x.MatchTime, match.MatchTime);

                return new UpdateOneModel<ArbitrageMatch>(filter, update)
                {
                    IsUpsert = true
                };
            }).ToList();

            var result = await collection.BulkWriteAsync(
                upsertModels,
                new BulkWriteOptions { IsOrdered = false },
                cancellationToken: stoppingToken
            );

            _logger.LogInformation(
                "Arbitrage matches stored. Matched: {0}, Modified: {1}, Upserts: {2}",
                result.MatchedCount,
                result.ModifiedCount,
                result.Upserts.Count
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing arbitrage matches");
            throw;
        }
    }
}

public class ArbitrageMatch
{
    public ObjectId Id { get; set; }
    public string MatchId { get; set; }
    public Teams Teams { get; set; }
    public string TournamentName { get; set; }
    public List<Market> Markets { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime MatchTime { get; set; }
}

public class Teams
{
    public Team Home { get; set; }
    public Team Away { get; set; }
}

public class Team
{
    public string Id { get; set; }
    public string Name { get; set; }
}

public class Market
{
    public string Id { get; set; }
    public string Description { get; set; }
    public string Specifier { get; set; }
    public decimal Margin { get; set; }
    public decimal ProfitPercentage { get; set; }
    public List<Outcome> Outcomes { get; set; } = new();
}

public class Outcome
{
    public string Id { get; set; }
    public string Description { get; set; }
    public decimal Odds { get; set; }
    public decimal StakePercentage { get; set; }
}

// API Response Models
public class ApiResponse
{
    [JsonPropertyName("bizCode")]
    public int BizCode { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; }

    [JsonPropertyName("data")]
    public ApiData Data { get; set; }
}

public class ApiData
{
    [JsonPropertyName("totalNum")]
    public int TotalNum { get; set; }

    [JsonPropertyName("tournaments")]
    public List<Tournament> Tournaments { get; set; }
}

public class Tournament
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("events")]
    public List<MatchData> Events { get; set; }
}

public class MatchData
{
    [JsonPropertyName("eventId")]
    public string EventId { get; set; }

    [JsonPropertyName("homeTeamId")]
    public string HomeTeamId { get; set; }

    [JsonPropertyName("awayTeamId")]
    public string AwayTeamId { get; set; }

    [JsonPropertyName("homeTeamName")]
    public string HomeTeamName { get; set; }

    [JsonPropertyName("awayTeamName")]
    public string AwayTeamName { get; set; }

    [JsonPropertyName("startTime")]
    public string StartTime { get; set; }

    [JsonPropertyName("tournament")]
    public Tournament Tournament { get; set; }

    [JsonPropertyName("markets")]
    public List<MarketData> Markets { get; set; }
}

public class MarketData
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("desc")]
    public string Desc { get; set; }

    [JsonPropertyName("specifier")]
    public string Specifier { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("outcomes")]
    public List<OutcomeData> Outcomes { get; set; }
}

public class OutcomeData
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("desc")]
    public string Desc { get; set; }

    [JsonPropertyName("odds")]
    public string Odds { get; set; }

    [JsonPropertyName("isActive")]
    public int IsActive { get; set; }
}


