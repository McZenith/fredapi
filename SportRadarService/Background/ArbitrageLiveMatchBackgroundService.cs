using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using fredapi.SignalR;
using fredapi.Utils;
using fredapi.Model.MatchSituationStats;
using fredapi.SportRadarService.TokenService;
using fredapi.SportRadarService.Transformers;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;

namespace fredapi.SportRadarService.Background.ArbitrageLiveMatchBackgroundService;

public partial class ArbitrageLiveMatchBackgroundService : BackgroundService
{
    private readonly ILogger<ArbitrageLiveMatchBackgroundService> _logger;
    private readonly IHubContext<LiveMatchHub> _hubContext;
    private readonly HttpClient _httpClient;
    private readonly MarketValidator _marketValidator;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMemoryCache _cache;
    private readonly PredictionEnrichedMatchService _predictionEnrichedMatchService;


    // Thread-safe collections for concurrent access
    private static readonly ConcurrentBag<ClientMatch> _lastSentArbitrageMatches = new();
    private static readonly ConcurrentBag<ClientMatch> _lastSentAllMatches = new();

    // Semaphore to limit concurrent API calls
    private static readonly SemaphoreSlim _apiSemaphore = new(3); // Max 3 concurrent API calls

    // Cache keys for consistent caching
    private const string CACHE_KEY_ARBITRAGE_MATCHES = "arbitrage_matches_cache";
    private const string CACHE_KEY_ALL_MATCHES = "all_matches_cache";
    private const string CACHE_KEY_MATCH_PREFIX = "match_data_";
    private const string CACHE_KEY_EVENT_PREFIX = "event_";

    private const int DelaySeconds = 60; // Changed from minutes to seconds for clarity
    private const decimal MaxAcceptableMargin = 10.0m;
    private const decimal MinProfitThreshold = 0.05m;

    // Static HttpClientHandler for connection pooling
    private static readonly HttpClientHandler _httpClientHandler = new()
    {
        MaxConnectionsPerServer = 20,
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
    };

    // JSON options for faster deserialization
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public ArbitrageLiveMatchBackgroundService(
        ILogger<ArbitrageLiveMatchBackgroundService> logger,
        IHubContext<LiveMatchHub> hubContext,
        IServiceProvider serviceProvider,
        IMemoryCache cache)
    {
        _logger = logger;
        _hubContext = hubContext;
        _marketValidator = new MarketValidator(logger);
        _serviceProvider = serviceProvider;
        _cache = cache;

        _predictionEnrichedMatchService = serviceProvider.GetRequiredService<PredictionEnrichedMatchService>();
        // Configure HttpClient with optimized settings
        _httpClient = new HttpClient(_httpClientHandler)
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        _httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        _httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SportApp", "1.0"));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var isInitialFetch = string.IsNullOrWhiteSpace(TokenService.TokenService.ApiToken);
                using var scope = _serviceProvider.CreateScope();

                if (isInitialFetch)
                {
                    var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
                    await tokenService.GetSportRadarToken();
                }

                await ProcessMatchesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in match processing");
            }
            finally
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(DelaySeconds), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Live match service is shutting down");
                }
            }
        }
    }

    private async Task ProcessMatchesAsync(CancellationToken stoppingToken)
    {
        try
        {
            var apiResponse = await FetchLiveMatchesWithRetryAsync(stoppingToken);
            if (apiResponse?.Data == null)
            {
                _logger.LogWarning("No data received from API");
                return;
            }

            var flattenedEvents = FlattenEvents(apiResponse);
            _logger.LogInformation($"Processing {flattenedEvents.Count} matches");

            // Process in parallel for better performance 
            var arbitrageMatches = await Task.Run(() => ProcessEvents(flattenedEvents), stoppingToken);
            var allMatches = await Task.Run(() => ProcessAllEvents(flattenedEvents), stoppingToken);

            LogMatchStatistics(arbitrageMatches, flattenedEvents.Count);

            await StreamMatchesToClientsAsync(arbitrageMatches, flattenedEvents, allMatches, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching and processing matches");
        }
    }

    // 1. Fix: Properly call ProcessMarkets instead of ProcessMarketsForArbitrage in ProcessSingleEvent
    private Match ProcessSingleEvent(Event eventData)
    {
        // Check cache first
        var cacheKey = $"{CACHE_KEY_EVENT_PREFIX}{eventData.EventId}";
        if (_cache.TryGetValue(cacheKey, out Match cachedMatch))
        {
            return cachedMatch;
        }

        _logger.LogDebug($"Processing event {eventData.EventId}: {eventData.HomeTeamName} vs {eventData.AwayTeamName}");

        // *** FIX: Use ProcessMarkets which doesn't filter by market type ***
        var potentialMarkets = ProcessMarkets(eventData);
    
        var marketTypes = potentialMarkets
            .Select(m => m.market.Description)
            .GroupBy(d => d)
            .Select(g => $"{g.Key}: {g.Count()}")
            .ToList();
    
        _logger.LogDebug($"Processing market types: {string.Join(", ", marketTypes)}");

        var arbitrageMarkets = potentialMarkets
            .Where(m => m.hasArbitrage && m.market.ProfitPercentage > MinProfitThreshold)
            .Select(m => m.market)
            .ToList();

        _logger.LogDebug($"Found {arbitrageMarkets.Count} arbitrage markets for event {eventData.EventId}");

        // Only create a match if there are actual arbitrage opportunities
        if (!arbitrageMarkets.Any())
        {
            _logger.LogDebug($"No arbitrage opportunities found for event {eventData.EventId}");
            return null;
        }

        var match = CreateMatch(eventData, arbitrageMarkets);

        // Cache result with expiration
        _cache.Set(cacheKey, match, TimeSpan.FromSeconds(30));

        return match;
    }



// This is the critical issue - your process is only looking for NextGoal markets for arbitrage
// We need to remove this restriction to find all arbitrage opportunities
    private List<(Market market, bool hasArbitrage)> ProcessMarketsForArbitrage(Event eventData)
    {
        _logger.LogDebug($"Processing ALL markets for arbitrage in match {eventData.EventId}");

        // Use parallel processing for markets, WITHOUT filtering for NextGoal markets only
        var markets = eventData.Markets
            .AsParallel()
            .Where(IsValidMarketStatus)
            .Where(m => _marketValidator.ValidateMarket(m))
            // REMOVED: Filter for markets that are NextGoal markets
            // This was restricting you to only finding arbitrage in NextGoal markets
            .Select(m => ProcessMarket(m, eventData.EventId))
            .Where(m => m.market.Outcomes.Any())
            .ToList();

        // Log all market types for debugging
        var marketTypes = markets
            .Select(m => m.market.Description)
            .GroupBy(d => d)
            .Select(g => $"{g.Key}: {g.Count()}")
            .ToList();

        _logger.LogDebug($"Market types considered for arbitrage: {string.Join(", ", marketTypes)}");
        _logger.LogDebug($"Found {markets.Count} valid markets to check for arbitrage in match {eventData.EventId}");

        return markets;
    }


// This will be used by other parts of your code
    private List<(Market market, bool hasArbitrage)> ProcessMarkets(Event eventData)
    {
        _logger.LogDebug($"Processing markets for match {eventData.EventId}");

        // Use parallel processing for markets
        var markets = eventData.Markets
            .AsParallel()
            .Where(IsValidMarketStatus)
            .Where(m => _marketValidator.ValidateMarket(m))
            .Select(m => ProcessMarket(m, eventData.EventId))
            .Where(m => m.market.Outcomes.Any())
            .ToList();

        _logger.LogDebug($"Found {markets.Count} valid markets for match {eventData.EventId}");

        return markets;
    }

// 2. Fix: Remove NextGoal filter from ProcessEvents
    private List<Match> ProcessEvents(List<Event> events)
    {
        // Use parallelism with controlled degree
        var matches = events
            .AsParallel()
            .WithDegreeOfParallelism(Math.Min(Environment.ProcessorCount, 4))
            .Select(ProcessSingleEvent)
            .Where(m => m != null && m.Markets.Any()) // Only include matches with actual arbitrage opportunities
            .Where(m => !m.Teams.Away.Name.ToUpper().Contains("SRL") && !m.Teams.Home.Name.ToUpper().Contains("SRL"))
            // *** FIX: Remove the filter that only keeps matches with NextGoal markets ***
            .ToList();

        // Log the types of arbitrage markets found
        var arbitrageTypes = matches
            .SelectMany(m => m.Markets)
            .GroupBy(m => m.Description)
            .Select(g => $"{g.Key}: {g.Count()}")
            .ToList();

        _logger.LogInformation($"Found arbitrage in market types: {string.Join(", ", arbitrageTypes)}");

        return matches;
    }

// Update the CreateMatchFromEvent method to ensure we consistently use 1X2 markets
    private Match CreateMatchFromEvent(Event eventData)
    {
        // Try cache first
        var cacheKey = $"{CACHE_KEY_EVENT_PREFIX}base_{eventData.EventId}";
        if (_cache.TryGetValue(cacheKey, out Match cachedMatch))
        {
            return cachedMatch;
        }

        // Explicitly filter for 1X2 markets only - make this clearer and more consistent
        var markets = eventData.Markets
            .Where(m => IsValidMarketStatus(m))
            .Where(m =>
                // Match on 1X2 markets explicitly
                m.Desc?.ToLower() == "match result" ||
                m.Desc?.ToLower() == "1x2" ||
                (bool)m.Desc?.ToLower().Contains("1x2")
            )
            .Select(m => new Market
            {
                Id = m.Id,
                Description = m.Desc,
                Specifier = m.Specifier,
                Outcomes = ProcessOutcomes(m.Outcomes),
                Favourite = m.Favourite,
            })
            .Where(m => m.Outcomes.Any() && m.Outcomes.Count == 3) // Ensure we have exactly 3 outcomes (1X2)
            .ToList();

        var match = CreateMatch(eventData, markets);

        // Cache result
        _cache.Set(cacheKey, match, TimeSpan.FromSeconds(30));

        return match;
    }

// 5. Fix: Simplify StreamMatchesToClientsAsync to avoid excessive filtering
// Fix the StreamMatchesToClientsAsync method to handle caching correctly
private async Task StreamMatchesToClientsAsync(List<Match> arbitrageMatches, List<Event> allEvents,
    List<Match> allMatchesFromEvents, CancellationToken stoppingToken = default)
{
    if (!arbitrageMatches.Any() && !allMatchesFromEvents.Any()) return;

    try
    {
        _logger.LogInformation($"Processing {allMatchesFromEvents.Count} matches for enrichment");

        // Log arbitrage match details for debugging
        foreach (var match in arbitrageMatches)
        {
            _logger.LogInformation($"Arbitrage match: {match.Id} - {match.Teams.Home.Name} vs {match.Teams.Away.Name}");
            foreach (var market in match.Markets)
            {
                _logger.LogInformation($"  Market: {market.Description}, Profit: {market.ProfitPercentage}%, " +
                                       $"Outcomes: {string.Join(", ", market.Outcomes.Select(o => $"{o.Description}: {o.Odds}"))}");
            }
        }

        // Create a separate list of matches to process for details/situation data
        // This ensures we process both arbitrage and regular matches for enrichment
        var allMatchesToProcess = new List<Match>();
        
        // Start with all regular matches
        allMatchesToProcess.AddRange(allMatchesFromEvents);
        
        // Add arbitrage matches only if they're not already in the list
        foreach (var match in arbitrageMatches)
        {
            if (!allMatchesToProcess.Any(m => m.Id == match.Id))
            {
                allMatchesToProcess.Add(match);
            }
        }
        
        _logger.LogInformation($"Processing a total of {allMatchesToProcess.Count} matches for details/situation data");

        // Process match data in batches
        var batches = allMatchesToProcess.Chunk(5).ToList();
        var batchTasks = new List<Task>();

        foreach (var batch in batches)
        {
            var batchTask = Task.Run(async () =>
            {
                foreach (var match in batch)
                {
                    if (stoppingToken.IsCancellationRequested) break;

                    try
                    {
                        await FetchMatchDataAsync(match);
                        await AddHumanLikeDelay(100, 200);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error fetching data for match {MatchId}", match.Id);
                        await AddHumanLikeDelay(300, 500);
                    }
                }
            }, stoppingToken);

            batchTasks.Add(batchTask);
            await Task.Delay(150, stoppingToken);
        }

        // Wait for all batches to complete
        await Task.WhenAll(batchTasks);

        // Enrich matches with prediction data
        // IMPORTANT: Keep these separate and don't mix the lists
        var (enrichedArbitrageMatches, enrichedAllMatches) = await _predictionEnrichedMatchService
            .EnrichMatchesWithPredictionDataAsync(arbitrageMatches, allMatchesFromEvents);

        // Keep all arbitrage matches without filtering
        var arbitrageMatchesForClient = enrichedArbitrageMatches.ToList();

        // For all matches, filter to only include 1X2 markets
        var allMatchesForClient = enrichedAllMatches
            .Select(match => {
                // Create a new match with only one 1X2 market
                var matchWith1X2Only = new ClientMatch {
                    Id = match.Id,
                    SeasonId = match.SeasonId,
                    Teams = match.Teams,
                    TournamentName = match.TournamentName,
                    Score = match.Score,
                    Period = match.Period,
                    MatchStatus = match.MatchStatus,
                    PlayedTime = match.PlayedTime,
                    LastUpdated = match.LastUpdated,
                    MatchSituation = match.MatchSituation,
                    MatchDetails = match.MatchDetails,
                    PredictionData = match.PredictionData,
                    // Only include one 1X2 market with more comprehensive matching
                    Markets = match.Markets
                        .Take(1) // Take just one market
                        .ToList()
                };
                
                return matchWith1X2Only;
            })
            .ToList();

        // Log sizes before sending
        _logger.LogInformation($"Sending {arbitrageMatchesForClient.Count} arbitrage matches and {allMatchesForClient.Count} regular matches");

        // Send messages to clients
        await _hubContext.Clients.All.SendAsync("ReceiveArbitrageLiveMatches", arbitrageMatchesForClient,
            cancellationToken: stoppingToken);
        await _hubContext.Clients.All.SendAsync("ReceiveAllLiveMatches", allMatchesForClient,
            cancellationToken: stoppingToken);

        // IMPORTANT: Keep these lists separate in static collections and cache
        UpdateStaticCollection(_lastSentArbitrageMatches, arbitrageMatchesForClient);
        UpdateStaticCollection(_lastSentAllMatches, allMatchesForClient);

        // Cache separately with their own keys
        _cache.Set(CACHE_KEY_ARBITRAGE_MATCHES, arbitrageMatchesForClient, TimeSpan.FromMinutes(5));
        _cache.Set(CACHE_KEY_ALL_MATCHES, allMatchesForClient, TimeSpan.FromMinutes(5));

        // Log stats for debugging
        LogDetailedMatchStats(arbitrageMatchesForClient, allMatchesForClient);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error streaming matches to clients");
    }
}


// Helper method to combine match lists without duplicates
private List<Match> CombineMatchLists(List<Match> arbitrageMatches, List<Match> allMatchesFromEvents)
{
    var allMatchesToProcess = new List<Match>(allMatchesFromEvents);
    
    // Add arbitrage matches that aren't already in the list
    foreach (var match in arbitrageMatches)
    {
        if (!allMatchesToProcess.Any(m => m.Id == match.Id))
        {
            allMatchesToProcess.Add(match);
        }
    }
    
    return allMatchesToProcess;
}

// Process batches of matches with parallel execution
private async Task ProcessMatchBatchesAsync(List<Match> matches, CancellationToken stoppingToken)
{
    // Create batches for parallel processing
    var batches = matches.Chunk(5).ToList();
    var batchTasks = new List<Task>();

    foreach (var batch in batches)
    {
        var batchTask = Task.Run(async () =>
        {
            foreach (var match in batch)
            {
                if (stoppingToken.IsCancellationRequested) break;

                try
                {
                    await FetchMatchDataAsync(match);
                    await AddHumanLikeDelay(100, 200);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error fetching data for match {MatchId}", match.Id);
                    await AddHumanLikeDelay(300, 500);
                }
            }
        }, stoppingToken);

        batchTasks.Add(batchTask);
        await Task.Delay(150, stoppingToken);
    }

    // Wait for all batches to complete
    await Task.WhenAll(batchTasks);
}

// Log match stats in a consistent way
private void LogMatchStats(List<ClientMatch> arbitrageMatches, List<ClientMatch> allMatches)
{
    _logger.LogInformation(
        "Streamed {ArbitrageMatchCount} arbitrage matches and {AllMatchCount} total 1X2 matches with enriched data",
        arbitrageMatches.Count,
        allMatches.Count);

    // Log match detail status for debugging
    var arbitrageDetailsCount = arbitrageMatches.Count(m => m.MatchDetails != null);
    var arbitrageSituationCount = arbitrageMatches.Count(m => m.MatchSituation != null);
    var allDetailsCount = allMatches.Count(m => m.MatchDetails != null);
    var allSituationCount = allMatches.Count(m => m.MatchSituation != null);

    _logger.LogInformation(
        "Arbitrage matches with details: {DetailsCount}/{TotalCount}, with situation: {SituationCount}/{TotalCount}",
        arbitrageDetailsCount, arbitrageMatches.Count, arbitrageSituationCount, arbitrageMatches.Count);

    _logger.LogInformation(
        "All matches with details: {DetailsCount}/{TotalCount}, with situation: {SituationCount}/{TotalCount}",
        allDetailsCount, allMatches.Count, allSituationCount, allMatches.Count);
}
    // Add this method for updating static collections
    private void UpdateStaticCollection<T>(ConcurrentBag<T> bag, List<T> newItems)
    {
        // Empty the bag first
        while (bag.TryTake(out _))
        {
        }

        // Add new items
        foreach (var item in newItems)
        {
            bag.Add(item);
        }
    }

    // Enhanced logging to troubleshoot caching and enrichment issues
private void LogDetailedMatchStats(List<ClientMatch> arbitrageMatches, List<ClientMatch> allMatches)
{
    // Log overall counts
    _logger.LogInformation(
        "Streamed {ArbitrageMatchCount} arbitrage matches and {AllMatchCount} regular matches with enriched data",
        arbitrageMatches.Count,
        allMatches.Count);

    // Log details about enrichment
    var arbitrageDetailsCount = arbitrageMatches.Count(m => m.MatchDetails != null);
    var arbitrageSituationCount = arbitrageMatches.Count(m => m.MatchSituation != null);
    var arbitragePredictionCount = arbitrageMatches.Count(m => m.PredictionData != null);
    
    var allDetailsCount = allMatches.Count(m => m.MatchDetails != null);
    var allSituationCount = allMatches.Count(m => m.MatchSituation != null);
    var allPredictionCount = allMatches.Count(m => m.PredictionData != null);

    _logger.LogInformation(
        "Arbitrage matches - with details: {DetailsCount}/{TotalCount}, with situation: {SituationCount}/{TotalCount}, with prediction: {PredictionCount}/{TotalCount}",
        arbitrageDetailsCount, arbitrageMatches.Count, arbitrageSituationCount, arbitrageMatches.Count, arbitragePredictionCount, arbitrageMatches.Count);

    _logger.LogInformation(
        "Regular matches - with details: {DetailsCount}/{TotalCount}, with situation: {SituationCount}/{TotalCount}, with prediction: {PredictionCount}/{TotalCount}",
        allDetailsCount, allMatches.Count, allSituationCount, allMatches.Count, allPredictionCount, allMatches.Count);
        
    // Check for duplicate match IDs between the two lists
    var arbitrageIds = arbitrageMatches.Select(m => m.Id).ToHashSet();
    var allMatchIds = allMatches.Select(m => m.Id).ToHashSet();
    var duplicateIds = arbitrageIds.Intersect(allMatchIds).ToList();
    
    if (duplicateIds.Any())
    {
        _logger.LogWarning("Found {DuplicateCount} matches that appear in both arbitrage and regular lists", duplicateIds.Count);
        foreach (var id in duplicateIds)
        {
            var arbMatch = arbitrageMatches.First(m => m.Id == id);
            var regMatch = allMatches.First(m => m.Id == id);
            _logger.LogWarning("Duplicate match: {MatchId} - {HomeTeam} vs {AwayTeam}", 
                id, arbMatch.Teams.Home.Name, arbMatch.Teams.Away.Name);
        }
    }
}

    private void ProcessMatchSituation(Match match, JsonDocument document)
    {
        try
        {
            var rawJson = document.RootElement.GetRawText();

            if (rawJson.Contains("\"event\":\"exception\""))
            {
                _logger.LogWarning("Received exception response for match {MatchId} in situation data", match.Id);
                return;
            }

            var response =
                JsonSerializer.Deserialize<fredapi.Model.Live.StatsMatchSituationResponse>(rawJson, _jsonOptions);

            if (response?.Doc?.FirstOrDefault()?.Data != null)
            {
                var data = response.Doc[0].Data;
                if (data.Data == null || !data.Data.Any())
                {
                    _logger.LogWarning("No match situation data found for match {MatchId}", data.MatchId);
                    return;
                }

                var validData = data.Data.Where(d => d != null && d.Home != null && d.Away != null).ToList();
                if (!validData.Any())
                {
                    _logger.LogWarning("No valid match situation data entries found for match {MatchId}", data.MatchId);
                    return;
                }

                var situationData = new MatchSituationStats
                {
                    MatchId = data.MatchId,
                    Data = validData.Select(d => new TimeSliceStats
                    {
                        Time = d.Time,
                        InjuryTime = d.InjuryTime,
                        Home = new fredapi.Model.MatchSituationStats.TeamStats
                        {
                            Attack = (int)Math.Round((decimal)d.Home.Attack, 0),
                            Dangerous = (int)Math.Round((decimal)d.Home.Dangerous, 0),
                            Safe = (int)Math.Round((decimal)d.Home.Safe, 0),
                            AttackCount = d.Home.AttackCount,
                            DangerousCount = d.Home.DangerousCount,
                            SafeCount = d.Home.SafeCount
                        },
                        Away = new fredapi.Model.MatchSituationStats.TeamStats
                        {
                            Attack = (int)Math.Round((decimal)d.Away.Attack, 0),
                            Dangerous = (int)Math.Round((decimal)d.Away.Dangerous, 0),
                            Safe = (int)Math.Round((decimal)d.Away.Safe, 0),
                            AttackCount = d.Away.AttackCount,
                            DangerousCount = d.Away.DangerousCount,
                            SafeCount = d.Away.SafeCount
                        }
                    }).ToList()
                };

                var analyzedStats = MatchSituationAnalyzer.AnalyzeMatchSituation(situationData);

                match.MatchSituation = new ClientMatchSituation
                {
                    TotalTime = analyzedStats.TotalTime,
                    DominantTeam = analyzedStats.DominantTeam,
                    MatchMomentum = analyzedStats.MatchMomentum,
                    Home = new ClientTeamSituation
                    {
                        TotalAttacks = analyzedStats.Home.TotalAttacks,
                        TotalDangerousAttacks = analyzedStats.Home.TotalDangerousAttacks,
                        TotalSafeAttacks = analyzedStats.Home.TotalSafeAttacks,
                        TotalAttackCount = analyzedStats.Home.TotalAttackCount,
                        TotalDangerousCount = analyzedStats.Home.TotalDangerousCount,
                        TotalSafeCount = analyzedStats.Home.TotalSafeCount,
                        AttackPercentage = Math.Round(analyzedStats.Home.AttackPercentage),
                        DangerousAttackPercentage = Math.Round(analyzedStats.Home.DangerousAttackPercentage),
                        SafeAttackPercentage = Math.Round(analyzedStats.Home.SafeAttackPercentage)
                    },
                    Away = new ClientTeamSituation
                    {
                        TotalAttacks = analyzedStats.Away.TotalAttacks,
                        TotalDangerousAttacks = analyzedStats.Away.TotalDangerousAttacks,
                        TotalSafeAttacks = analyzedStats.Away.TotalSafeAttacks,
                        TotalAttackCount = analyzedStats.Away.TotalAttackCount,
                        TotalDangerousCount = analyzedStats.Away.TotalDangerousCount,
                        TotalSafeCount = analyzedStats.Away.TotalSafeCount,
                        AttackPercentage = Math.Round(analyzedStats.Away.AttackPercentage),
                        DangerousAttackPercentage = Math.Round(analyzedStats.Away.DangerousAttackPercentage),
                        SafeAttackPercentage = Math.Round(analyzedStats.Away.SafeAttackPercentage)
                    }
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing match situation for match {MatchId}", match.Id);
        }
    }

    private void ProcessMatchDetails(Match match, JsonDocument document)
    {
        try
        {
            var rawJson = document.RootElement.GetRawText();

            if (rawJson.Contains("\"event\":\"exception\""))
            {
                _logger.LogWarning("Received exception response for match {MatchId} in details data", match.Id);
                return;
            }

            var options = new JsonSerializerOptions(_jsonOptions)
            {
                Converters =
                {
                    new JsonDictionaryStringNumberConverter(),
                    new MatchDetailsDataConverter()
                }
            };

            var detailsData = JsonSerializer.Deserialize<MatchDetailsExtendedResponse>(rawJson, options);

            if (detailsData?.Doc?.FirstOrDefault()?.Data != null)
            {
                var data = detailsData.Doc[0].Data;
                match.MatchDetails = new ClientMatchDetailsExtended
                {
                    Types = data.Types,
                    Home = new ClientTeamStats
                    {
                        YellowCards = ExtractIntValue(data.Values, "40", "home"),
                        RedCards = ExtractIntValue(data.Values, "50", "home"),
                        FreeKicks = ExtractIntValue(data.Values, "120", "home"),
                        GoalKicks = ExtractIntValue(data.Values, "121", "home"),
                        ThrowIns = ExtractIntValue(data.Values, "122", "home"),
                        Offsides = ExtractIntValue(data.Values, "123", "home"),
                        CornerKicks = ExtractIntValue(data.Values, "124", "home"),
                        ShotsOnTarget = ExtractIntValue(data.Values, "125", "home"),
                        ShotsOffTarget = ExtractIntValue(data.Values, "126", "home"),
                        Saves = ExtractIntValue(data.Values, "127", "home"),
                        Fouls = ExtractIntValue(data.Values, "129", "home"),
                        Injuries = ExtractIntValue(data.Values, "158", "home"),
                        DangerousAttacks = ExtractIntValue(data.Values, "1029", "home"),
                        BallSafe = ExtractIntValue(data.Values, "1030", "home"),
                        TotalAttacks = ExtractIntValue(data.Values, "1126", "home"),
                        GoalAttempts = ExtractIntValue(data.Values, "goalattempts", "home"),
                        BallSafePercentage = ExtractDoubleValue(data.Values, "ballsafepercentage", "home"),
                        AttackPercentage = ExtractDoubleValue(data.Values, "attackpercentage", "home"),
                        DangerousAttackPercentage = ExtractDoubleValue(data.Values, "dangerousattackpercentage", "home")
                    },
                    Away = new ClientTeamStats
                    {
                        YellowCards = ExtractIntValue(data.Values, "40", "away"),
                        RedCards = ExtractIntValue(data.Values, "50", "away"),
                        FreeKicks = ExtractIntValue(data.Values, "120", "away"),
                        GoalKicks = ExtractIntValue(data.Values, "121", "away"),
                        ThrowIns = ExtractIntValue(data.Values, "122", "away"),
                        Offsides = ExtractIntValue(data.Values, "123", "away"),
                        CornerKicks = ExtractIntValue(data.Values, "124", "away"),
                        ShotsOnTarget = ExtractIntValue(data.Values, "125", "away"),
                        ShotsOffTarget = ExtractIntValue(data.Values, "126", "away"),
                        Saves = ExtractIntValue(data.Values, "127", "away"),
                        Fouls = ExtractIntValue(data.Values, "129", "away"),
                        Injuries = ExtractIntValue(data.Values, "158", "away"),
                        DangerousAttacks = ExtractIntValue(data.Values, "1029", "away"),
                        BallSafe = ExtractIntValue(data.Values, "1030", "away"),
                        TotalAttacks = ExtractIntValue(data.Values, "1126", "away"),
                        GoalAttempts = ExtractIntValue(data.Values, "goalattempts", "away"),
                        BallSafePercentage = ExtractDoubleValue(data.Values, "ballsafepercentage", "away"),
                        AttackPercentage = ExtractDoubleValue(data.Values, "attackpercentage", "away"),
                        DangerousAttackPercentage = ExtractDoubleValue(data.Values, "dangerousattackpercentage", "away")
                    }
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing match details for match {MatchId}", match.Id);
        }
    }

// Add this method for fetching match data
    private async Task FetchMatchDataAsync(Match match)
    {
        // Check cache first
        var cacheKey = $"{CACHE_KEY_MATCH_PREFIX}{match.Id}";
        if (_cache.TryGetValue(cacheKey, out var cachedData))
        {
            var (situation, details) = ((ClientMatchSituation, ClientMatchDetailsExtended))cachedData;
            match.MatchSituation = situation;
            match.MatchDetails = details;
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var matchService = scope.ServiceProvider.GetService<SportRadarService>();
        if (matchService == null)
        {
            _logger.LogWarning("SportRadarService not found in service provider");
            return;
        }

        // Use semaphore to limit API calls
        var acquiredSemaphore = await _apiSemaphore.WaitAsync(3000);
        await AddHumanLikeDelay(700, 1100);

        if (!acquiredSemaphore)
        {
            _logger.LogWarning("Timeout waiting for API semaphore for match {MatchId}", match.Id);
            return;
        }

        try
        {
            // Execute both API calls in parallel
            var situationTask = matchService.GetMatchSituationAsync(match.Id.ToString());
            var detailsTask = matchService.GetMatchDetailsExtendedAsync(match.Id.ToString());

            await Task.WhenAll(situationTask, detailsTask);

            // Process situation data if available
            if (situationTask.Result is Ok<JsonDocument> situationResult && situationResult.Value != null)
            {
                ProcessMatchSituation(match, situationResult.Value);
            }

            // Process details data if available
            if (detailsTask.Result is Ok<JsonDocument> detailsResult && detailsResult.Value != null)
            {
                ProcessMatchDetails(match, detailsResult.Value);
            }

            // Cache the results for future usage
            if (match.MatchSituation != null || match.MatchDetails != null)
            {
                _cache.Set(cacheKey, (match.MatchSituation, match.MatchDetails), TimeSpan.FromMinutes(2));
            }
        }
        finally
        {
            _apiSemaphore.Release();
        }
    }

    private async Task<ApiResponse> FetchLiveMatchesWithRetryAsync(CancellationToken stoppingToken)
    {
        // Implement retry pattern with exponential backoff
        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                await _apiSemaphore.WaitAsync(stoppingToken);

                try
                {
                    // Add timestamp to prevent caching
                    var url =
                        $"https://www.sportybet.com/api/ng/factsCenter/liveOrPrematchEvents?sportId=sr%3Asport%3A1&_t={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

                    var response = await _httpClient.GetAsync(url, stoppingToken);
                    response.EnsureSuccessStatusCode();

                    // Use stream for efficient memory usage
                    await using var contentStream = await response.Content.ReadAsStreamAsync(stoppingToken);
                    return await JsonSerializer.DeserializeAsync<ApiResponse>(contentStream, _jsonOptions,
                        cancellationToken: stoppingToken);
                }
                finally
                {
                    _apiSemaphore.Release();
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                if (attempt == 2) throw; // Rethrow on final attempt

                _logger.LogWarning(ex, "API request failed, retrying after delay (attempt {Attempt}/3)", attempt + 1);
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), stoppingToken); // Exponential backoff
            }
        }

        throw new InvalidOperationException("Failed to fetch live matches after multiple attempts");
    }

    private List<Event> FlattenEvents(ApiResponse apiResponse)
    {
        return apiResponse.Data
            .SelectMany(t => t.Events.Select(e =>
            {
                e.Sport.Category.Tournament.Name = t.Name;
                return e;
            }))
            .ToList();
    }

    private List<Match> ProcessAllEvents(List<Event> events)
    {
        // Use parallelism with controlled degree
        return events
            .AsParallel()
            .WithDegreeOfParallelism(Math.Min(Environment.ProcessorCount, 4))
            .Select(CreateMatchFromEvent)
            .Where(m => m.Markets.Any())
            .Where(m => !m.Teams.Away.Name.ToUpper().Contains("SRL") && !m.Teams.Home.Name.ToUpper().Contains("SRL"))
            .ToList();
    }


    private bool IsValidMarketStatus(MarketData market)
    {
        return market.Status != 2 && market.Status != 3; // Exclude suspended and settled markets
    }

// 4. Fix: Improve ProcessMarket to better handle different market types
    private (Market market, bool hasArbitrage) ProcessMarket(MarketData marketData, string matchId)
    {
        try
        {
            _logger.LogDebug($"Processing market {marketData.Id} ({marketData.Desc}) for match {matchId}");
            var market = new Market
            {
                Id = marketData.Id,
                Description = marketData.Desc,
                Specifier = marketData.Specifier,
                Outcomes = ProcessOutcomes(marketData.Outcomes),
                Favourite = marketData.Favourite,
            };

            if (!market.Outcomes.Any())
            {
                _logger.LogDebug($"Market {marketData.Id} has no valid outcomes");
                return (market, false);
            }

            // We need at least 2 outcomes for arbitrage
            if (market.Outcomes.Count < 2)
            {
                _logger.LogDebug($"Market {marketData.Id} has fewer than 2 outcomes, can't have arbitrage");
                return (market, false);
            }

            // Try validating, but don't exit early if validation fails
            bool isValidMarket = _marketValidator.ValidateMarket(marketData, market.Outcomes.Count);
            if (!isValidMarket)
            {
                _logger.LogDebug($"Market {marketData.Id} failed validation: {marketData.Desc}, outcomes: {market.Outcomes.Count}");
                // Continue anyway for arbitrage checking since it's mathematically possible
            }

            var (hasArbitrage, stakePercentages, profitPercentage) =
                CalculateArbitrageOpportunity(market);

            if (hasArbitrage)
            {
                UpdateMarketWithArbitrageData(market, stakePercentages, profitPercentage);
                LogArbitrageOpportunity(matchId, market);
            }

            return (market, hasArbitrage);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error processing market {MarketId}", marketData.Id);
            return (new Market(), false);
        }
    }
    
    private List<Outcome> ProcessOutcomes(List<OutcomeData> outcomes)
    {
        var validOutcomes = outcomes
            .Where(o => o != null && o.IsActive == 1 && !string.IsNullOrEmpty(o.Odds))
            .Select(CreateOutcome)
            .Where(o => o != null)
            .ToList();

        // Fix #9: Log outcomes for debugging
        _logger.LogDebug($"Processed {outcomes.Count} outcomes, found {validOutcomes.Count} valid outcomes: " +
                         $"{string.Join(", ", validOutcomes.Select(o => $"{o.Description}: {o.Odds}"))}");

        return validOutcomes;
    }

    private Outcome CreateOutcome(OutcomeData outcomeData)
    {
        if (!decimal.TryParse(outcomeData.Odds, out var odds) || odds <= 0)
            return null;

        return new Outcome
        {
            Id = outcomeData.Id ?? "",
            Description = outcomeData.Desc ?? "",
            Odds = odds
        };
    }

    private void UpdateMarketWithArbitrageData(Market market, List<decimal> stakePercentages, decimal profitPercentage)
    {
        for (int i = 0; i < market.Outcomes.Count; i++)
        {
            market.Outcomes[i].StakePercentage = stakePercentages[i];
        }

        market.ProfitPercentage = profitPercentage;
    }

    private void LogArbitrageOpportunity(string matchId, Market market)
    {
        _logger.LogInformation(
            "Found arbitrage opportunity in match {MatchId}, market {MarketId} with {ProfitPercentage}% profit",
            matchId,
            market.Id,
            market.ProfitPercentage);
    }

    private Match CreateMatch(Event eventData, List<Market> arbitrageMarkets)
    {
        return new Match
        {
            Id = (int)(long.Parse(eventData.EventId.Split(':')[2]) % int.MaxValue),
            Teams = new Teams
            {
                Home = new Team
                {
                    Id = (int)(long.Parse(eventData.HomeTeamId.Split(':')[2]) % int.MaxValue),
                    Name = eventData.HomeTeamName,
                },
                Away = new Team
                {
                    Id = (int)(long.Parse(eventData.AwayTeamId.Split(':')[2]) % int.MaxValue),
                    Name = eventData.AwayTeamName,
                }
            },
            SeasonId = (int)(long.Parse(eventData.Sport.Category.Tournament.Id.Split(':')[2]) % int.MaxValue),
            Result = null,
            TournamentName = eventData.Sport.Category.Tournament.Name,
            Score = eventData.SetScore,
            Period = eventData.Period,
            MatchStatus = eventData.MatchStatus,
            PlayedTime = eventData.PlayedSeconds,
            Markets = arbitrageMarkets,
            LastUpdated = DateTime.UtcNow
        };
    }
    
// 3. Fix: Enhance the arbitrage calculation logic
    private (bool hasArbitrage, List<decimal> stakePercentages, decimal profitPercentage)
        CalculateArbitrageOpportunity(Market market)
    {
        if (!market.Outcomes.Any())
        {
            return (false, new List<decimal>(), 0m);
        }

        // Require at least 2 outcomes for arbitrage
        if (market.Outcomes.Count < 2)
        {
            return (false, new List<decimal>(), 0m);
        }

        var margin = CalculateMarginPercentage(market.Outcomes);
        market.Margin = margin;

        _logger.LogDebug($"Market {market.Id} ({market.Description}) has margin {margin}%");

        if (margin > MaxAcceptableMargin)
        {
            return (false, new List<decimal>(), 0m);
        }

        var totalInverse = market.Outcomes.Sum(o => 1m / o.Odds);
        var profitPercentage = ((1 / totalInverse) - 1) * 100;

        _logger.LogDebug($"Market {market.Id} ({market.Description}) has profit {profitPercentage:F2}%");

        if (profitPercentage <= 0)
        {
            return (false, new List<decimal>(), 0m);
        }

        // For actual arbitrage opportunities, log more prominently
        if (profitPercentage > MinProfitThreshold)
        {
            _logger.LogInformation($"ARBITRAGE FOUND: Market {market.Id} ({market.Description}) " +
                                   $"with {profitPercentage:F2}% profit, " +
                                   $"Outcomes: {string.Join(", ", market.Outcomes.Select(o => $"{o.Description}: {o.Odds}"))}");
        }

        var stakePercentages = CalculateStakePercentages(market.Outcomes, totalInverse);
        return (true, stakePercentages, Math.Round(profitPercentage, 2));
    }

    private List<decimal> CalculateStakePercentages(List<Outcome> outcomes, decimal totalInverse)
    {
        return outcomes
            .Select(o => (1m / o.Odds / totalInverse) * 100m)
            .Select(p => Math.Round(p, 2))
            .ToList();
    }

    private decimal CalculateMarginPercentage(List<Outcome> outcomes)
    {
        var impliedProbabilities = outcomes.Select(o => 1m / o.Odds);
        var totalImpliedProbability = impliedProbabilities.Sum();
        var margin = (totalImpliedProbability - 1m) * 100;

        // Log the calculation details for troubleshooting
        _logger.LogDebug(
            $"Margin calculation: total implied probability = {totalImpliedProbability}, margin = {margin}%");

        return Math.Round(margin, 2);
    }

    private void LogMatchStatistics(List<Match> matches, int totalEvents)
    {
        var totalArbitrageOpportunities = matches.Sum(m => m.Markets.Count);
        _logger.LogInformation($"Found {matches.Count} matches with arbitrage opportunities");
        _logger.LogInformation($"Total arbitrage opportunities: {totalArbitrageOpportunities}");
        _logger.LogInformation($"Arbitrage opportunity rate: {(decimal)matches.Count / totalEvents:P2}");
    }

    private ClientTeamStats CreateTeamStats(Dictionary<string, StatValue> values, string team)
    {
        return new ClientTeamStats
        {
            YellowCards = ExtractIntValue(values, "40", team),
            RedCards = ExtractIntValue(values, "50", team),
            FreeKicks = ExtractIntValue(values, "120", team),
            GoalKicks = ExtractIntValue(values, "121", team),
            ThrowIns = ExtractIntValue(values, "122", team),
            Offsides = ExtractIntValue(values, "123", team),
            CornerKicks = ExtractIntValue(values, "124", team),
            ShotsOnTarget = ExtractIntValue(values, "125", team),
            ShotsOffTarget = ExtractIntValue(values, "126", team),
            Saves = ExtractIntValue(values, "127", team),
            Fouls = ExtractIntValue(values, "129", team),
            Injuries = ExtractIntValue(values, "158", team),
            DangerousAttacks = ExtractIntValue(values, "1029", team),
            BallSafe = ExtractIntValue(values, "1030", team),
            TotalAttacks = ExtractIntValue(values, "1126", team),
            GoalAttempts = ExtractIntValue(values, "goalattempts", team),
            BallSafePercentage = ExtractDoubleValue(values, "ballsafepercentage", team),
            AttackPercentage = ExtractDoubleValue(values, "attackpercentage", team),
            DangerousAttackPercentage = ExtractDoubleValue(values, "dangerousattackpercentage", team)
        };
    }

    private static async Task AddHumanLikeDelay(int minMs, int maxMs)
    {
        var delay = Random.Shared.Next(minMs, maxMs);
        await Task.Delay(delay);
    }

    private ClientMatch CreateClientMatch(Match match)
    {
        return new ClientMatch
        {
            Id = match.Id,
            SeasonId = match.SeasonId,
            Teams = new ClientTeams
            {
                Home = new ClientTeam { Id = match.Teams.Home.Id, Name = match.Teams.Home.Name },
                Away = new ClientTeam { Id = match.Teams.Away.Id, Name = match.Teams.Away.Name }
            },
            TournamentName = match.TournamentName,
            Score = match.Score,
            Period = match.Period,
            MatchStatus = match.MatchStatus,
            PlayedTime = match.PlayedTime,
            Markets = match.Markets.Select(m => new ClientMarket
            {
                Id = m.Id,
                Description = m.Description,
                Specifier = m.Specifier,
                Margin = m.Margin,
                Favourite = m.Favourite,
                ProfitPercentage = m.ProfitPercentage,
                Outcomes = m.Outcomes.Select(o => new ClientOutcome
                {
                    Id = o.Id,
                    Description = o.Description,
                    Odds = o.Odds,
                    StakePercentage = o.StakePercentage
                }).ToList()
            }).ToList(),
            LastUpdated = DateTime.UtcNow,
            MatchSituation = match.MatchSituation,
            MatchDetails = match.MatchDetails
        };
    }

    // Optimized methods to get the last sent matches (using threadsafe collection)
    public static List<ClientMatch> GetLastSentArbitrageMatches() => _lastSentArbitrageMatches.ToList();
    public static List<ClientMatch> GetLastSentAllMatches() => _lastSentAllMatches.ToList();

    private int ExtractIntValue(Dictionary<string, StatValue> values, string key, string team)
    {
        if (values == null || !values.TryGetValue(key, out var statValue) ||
            statValue?.Value == null || !statValue.Value.TryGetValue(team, out var valueObj))
        {
            return 0;
        }

        if (valueObj == null)
            return 0;

        if (valueObj is string strValue)
            return int.TryParse(strValue, out var value) ? value : 0;
        if (valueObj is int intValue)
            return intValue;
        if (valueObj is long longValue)
            return (int)longValue;
        if (valueObj is double doubleValue)
            return (int)doubleValue;
        return 0;
    }

    private double ExtractDoubleValue(Dictionary<string, StatValue> values, string key, string team)
    {
        if (values == null || !values.TryGetValue(key, out var statValue) ||
            statValue?.Value == null || !statValue.Value.TryGetValue(team, out var valueObj))
        {
            return 0;
        }

        if (valueObj == null)
            return 0;

        if (valueObj is string strValue)
            return double.TryParse(strValue, out var value) ? value : 0;
        if (valueObj is int intValue)
            return intValue;
        if (valueObj is long longValue)
            return longValue;
        if (valueObj is double doubleValue)
            return doubleValue;
        return 0;
    }

    // Add this custom comparer for matches to properly deduplicate in the Distinct call
    private class MatchEqualityComparer : IEqualityComparer<Match>
    {
        public bool Equals(Match x, Match y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;
            return x.Id == y.Id;
        }

        public int GetHashCode(Match obj)
        {
            return obj.Id.GetHashCode();
        }
    }
}

public class ApiResponse
{
    [JsonPropertyName("bizCode")] public int BizCode { get; set; }

    [JsonPropertyName("message")] public string Message { get; set; }

    [JsonPropertyName("data")] public List<TournamentData> Data { get; set; }
}

public class TournamentData
{
    [JsonPropertyName("id")] public string Id { get; set; }

    [JsonPropertyName("name")] public string Name { get; set; }

    [JsonPropertyName("events")] public List<Event> Events { get; set; }
}

public class Event
{
    [JsonPropertyName("eventId")] public string EventId { get; set; }

    [JsonPropertyName("gameId")] public string GameId { get; set; }

    [JsonPropertyName("homeTeamId")] public string HomeTeamId { get; set; }

    [JsonPropertyName("homeTeamName")] public string HomeTeamName { get; set; }

    [JsonPropertyName("awayTeamId")] public string AwayTeamId { get; set; }

    [JsonPropertyName("awayTeamName")] public string AwayTeamName { get; set; }

    [JsonPropertyName("startTime")] public string StartTime { get; set; }

    [JsonPropertyName("status")] public int Status { get; set; }

    [JsonPropertyName("setScore")] public string SetScore { get; set; }

    [JsonPropertyName("period")] public string Period { get; set; }

    [JsonPropertyName("matchStatus")] public string MatchStatus { get; set; }

    [JsonPropertyName("playedSeconds")] public string PlayedSeconds { get; set; }

    [JsonPropertyName("markets")] public List<MarketData> Markets { get; set; }

    [JsonPropertyName("sport")] public Sport Sport { get; set; }
}

public class Sport
{
    [JsonPropertyName("id")] public string Id { get; set; }

    [JsonPropertyName("name")] public string Name { get; set; }

    [JsonPropertyName("category")] public Category Category { get; set; }
}

public class Category
{
    [JsonPropertyName("id")] public string Id { get; set; }

    [JsonPropertyName("name")] public string Name { get; set; }

    [JsonPropertyName("tournament")] public Tournament Tournament { get; set; }
}

public class Tournament
{
    [JsonPropertyName("id")] public string Id { get; set; }

    [JsonPropertyName("name")] public string Name { get; set; }
}

public class MarketData
{
    [JsonPropertyName("id")] public string Id { get; set; }

    [JsonPropertyName("desc")] public string Desc { get; set; }

    [JsonPropertyName("specifier")] public string Specifier { get; set; }

    [JsonPropertyName("status")] public int Status { get; set; }

    [JsonPropertyName("group")] public string Group { get; set; }

    [JsonPropertyName("groupId")] public string GroupId { get; set; }

    [JsonPropertyName("marketGuide")] public string MarketGuide { get; set; }

    [JsonPropertyName("title")] public string Title { get; set; }

    [JsonPropertyName("name")] public string Name { get; set; }

    [JsonPropertyName("favourite")] public int Favourite { get; set; }

    [JsonPropertyName("outcomes")] public List<OutcomeData> Outcomes { get; set; }

    [JsonPropertyName("farNearOdds")] public int FarNearOdds { get; set; }

    [JsonPropertyName("sourceType")] public string SourceType { get; set; }

    [JsonPropertyName("availableScore")] public string AvailableScore { get; set; }

    [JsonPropertyName("banned")] public bool Banned { get; set; }
}

public class OutcomeData
{
    [JsonPropertyName("id")] public string Id { get; set; }

    [JsonPropertyName("desc")] public string Desc { get; set; }

    [JsonPropertyName("odds")] public string Odds { get; set; }

    [JsonPropertyName("probability")] public string Probability { get; set; }

    [JsonPropertyName("isActive")] public int IsActive { get; set; }
}

public class Match
{
    public int Id { get; set; }
    public Teams Teams { get; set; }
    public int SeasonId { get; set; }
    public object Result { get; set; }
    public string TournamentName { get; set; }
    public string Score { get; set; }
    public string Period { get; set; }
    public string MatchStatus { get; set; }
    public string PlayedTime { get; set; }
    public List<Market> Markets { get; set; } = new();
    public DateTime LastUpdated { get; set; }
    public ClientMatchSituation MatchSituation { get; set; }
    public ClientMatchDetailsExtended MatchDetails { get; set; }
}

public class Teams
{
    public Team Home { get; set; }
    public Team Away { get; set; }
}

public class Team
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class Market
{
    public string Id { get; set; }
    public string Description { get; set; }
    public string Specifier { get; set; }
    public decimal Margin { get; set; }
    public decimal ProfitPercentage { get; set; }
    public int Favourite { get; set; }
    public List<Outcome> Outcomes { get; set; } = new();
}

public class Outcome
{
    public string Id { get; set; }
    public string Description { get; set; }
    public decimal Odds { get; set; }
    public decimal StakePercentage { get; set; }
}

// Additional Models for SignalR
public class ClientMatch
{
    public int Id { get; set; }
    public int SeasonId { get; set; }
    public ClientTeams Teams { get; set; }
    public string TournamentName { get; set; }
    public string Score { get; set; }
    public string Period { get; set; }
    public string MatchStatus { get; set; }
    public string PlayedTime { get; set; }
    public List<ClientMarket> Markets { get; set; }
    public DateTime LastUpdated { get; set; }
    public ClientMatchSituation MatchSituation { get; set; }
    public ClientMatchDetailsExtended MatchDetails { get; set; }
    public ClientMatchPredictionData PredictionData { get; set; }
}

public class ClientTeams
{
    public ClientTeam Home { get; set; }
    public ClientTeam Away { get; set; }
}

public class ClientTeam
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class ClientMarket
{
    public string Id { get; set; }
    public string Description { get; set; }
    public string Specifier { get; set; }
    public decimal Margin { get; set; }
    public int Favourite { get; set; }
    public decimal ProfitPercentage { get; set; }
    public List<ClientOutcome> Outcomes { get; set; }
}

public class ClientOutcome
{
    public string Id { get; set; }
    public string Description { get; set; }
    public decimal Odds { get; set; }
    public decimal StakePercentage { get; set; }
}

public class ClientMatchSituation
{
    public int TotalTime { get; set; }
    public string DominantTeam { get; set; }
    public string MatchMomentum { get; set; }
    public ClientTeamSituation Home { get; set; }
    public ClientTeamSituation Away { get; set; }
}

public class ClientTeamSituation
{
    public int TotalAttacks { get; set; }
    public int TotalDangerousAttacks { get; set; }
    public int TotalSafeAttacks { get; set; }
    public int TotalAttackCount { get; set; }
    public int TotalDangerousCount { get; set; }
    public int TotalSafeCount { get; set; }
    public double AttackPercentage { get; set; }
    public double DangerousAttackPercentage { get; set; }
    public double SafeAttackPercentage { get; set; }
}

public class ClientMatchDetailsExtended
{
    public ClientTeamStats Home { get; set; }
    public ClientTeamStats Away { get; set; }
    public Dictionary<string, string> Types { get; set; }
}

public class ClientTeamStats
{
    public int YellowCards { get; set; }
    public int RedCards { get; set; }
    public int FreeKicks { get; set; }
    public int GoalKicks { get; set; }
    public int ThrowIns { get; set; }
    public int Offsides { get; set; }
    public int CornerKicks { get; set; }
    public int ShotsOnTarget { get; set; }
    public int ShotsOffTarget { get; set; }
    public int Saves { get; set; }
    public int Fouls { get; set; }
    public int Injuries { get; set; }
    public int DangerousAttacks { get; set; }
    public int BallSafe { get; set; }
    public int TotalAttacks { get; set; }
    public int GoalAttempts { get; set; }
    public double BallSafePercentage { get; set; }
    public double AttackPercentage { get; set; }
    public double DangerousAttackPercentage { get; set; }
}

public class MatchDetailsExtendedResponse
{
    [JsonPropertyName("queryUrl")] public string QueryUrl { get; set; }

    [JsonPropertyName("doc")] public List<MatchDetailsDoc> Doc { get; set; }
}

public class MatchDetailsDoc
{
    [JsonPropertyName("event")] public string Event { get; set; }

    [JsonPropertyName("_dob")] public long Dob { get; set; }

    [JsonPropertyName("_maxage")] public int MaxAge { get; set; }

    [JsonPropertyName("data")] public MatchDetailsData Data { get; set; }
}

public class MatchDetailsData
{
    [JsonPropertyName("_doc")] public string Doc { get; set; }

    [JsonPropertyName("_matchid")] public long MatchId { get; set; }

    [JsonPropertyName("teams")] public Teams Teams { get; set; }

    [JsonPropertyName("index")] public List<object> Index { get; set; }

    [JsonPropertyName("values")] public Dictionary<string, StatValue> Values { get; set; }

    [JsonPropertyName("types")] public Dictionary<string, string> Types { get; set; }
}

public class TeamsConverter : JsonConverter<Teams>
{
    public override Teams Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var teams = new Teams();

        if (reader.TokenType == JsonTokenType.Null)
        {
            return teams;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            reader.Skip();
            return teams;
        }

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return teams;

            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            var propertyName = reader.GetString()?.ToLower();
            reader.Read();

            try
            {
                switch (propertyName)
                {
                    case "home":
                        if (reader.TokenType == JsonTokenType.StartObject)
                        {
                            teams.Home = JsonSerializer.Deserialize<Team>(ref reader, options);
                        }
                        else
                        {
                            reader.Skip();
                        }

                        break;
                    case "away":
                        if (reader.TokenType == JsonTokenType.StartObject)
                        {
                            teams.Away = JsonSerializer.Deserialize<Team>(ref reader, options);
                        }
                        else
                        {
                            reader.Skip();
                        }

                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }
            catch (JsonException)
            {
                reader.Skip();
            }
        }

        return teams;
    }

    public override void Write(Utf8JsonWriter writer, Teams value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("home");
        JsonSerializer.Serialize(writer, value.Home, options);
        writer.WritePropertyName("away");
        JsonSerializer.Serialize(writer, value.Away, options);
        writer.WriteEndObject();
    }
}

public class TeamConverter : JsonConverter<Team>
{
    public override Team Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var team = new Team();

        if (reader.TokenType == JsonTokenType.Null)
        {
            return team;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            reader.Skip();
            return team;
        }

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return team;

            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            var propertyName = reader.GetString()?.ToLower();
            reader.Read();

            try
            {
                switch (propertyName)
                {
                    case "id":
                        if (reader.TokenType == JsonTokenType.Number)
                        {
                            if (reader.TryGetInt32(out int intValue))
                                team.Id = intValue;
                            else if (reader.TryGetInt64(out long longValue))
                                team.Id = (int)longValue;
                            else if (reader.TryGetDouble(out double doubleValue))
                                team.Id = (int)doubleValue;
                        }
                        else if (reader.TokenType == JsonTokenType.String)
                        {
                            var strValue = reader.GetString();
                            if (int.TryParse(strValue, out int parsedValue))
                                team.Id = parsedValue;
                        }
                        else
                        {
                            reader.Skip();
                        }

                        break;
                    case "name":
                        if (reader.TokenType == JsonTokenType.String)
                        {
                            team.Name = reader.GetString();
                        }
                        else
                        {
                            reader.Skip();
                        }

                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }
            catch (JsonException)
            {
                reader.Skip();
            }
        }

        return team;
    }

    public override void Write(Utf8JsonWriter writer, Team value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("id", value.Id);
        writer.WriteString("name", value.Name);
        writer.WriteEndObject();
    }
}

public class MatchDetailsDataConverter : JsonConverter<MatchDetailsData>
{
    private readonly TeamsConverter _teamsConverter = new TeamsConverter();
    private readonly TeamConverter _teamConverter = new TeamConverter();

    public override MatchDetailsData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var data = new MatchDetailsData();

        if (reader.TokenType == JsonTokenType.Null)
        {
            return data;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            reader.Skip();
            return data;
        }

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return data;

            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            var propertyName = reader.GetString();
            reader.Read();

            try
            {
                switch (propertyName)
                {
                    case "_doc":
                        if (reader.TokenType == JsonTokenType.String)
                        {
                            data.Doc = reader.GetString();
                        }
                        else
                        {
                            reader.Skip();
                        }

                        break;
                    case "_matchid":
                        if (reader.TokenType == JsonTokenType.Number)
                        {
                            if (reader.TryGetInt64(out long longValue))
                                data.MatchId = longValue;
                            else if (reader.TryGetInt32(out int intValue))
                                data.MatchId = intValue;
                            else if (reader.TryGetDouble(out double doubleValue))
                                data.MatchId = (long)doubleValue;
                        }
                        else if (reader.TokenType == JsonTokenType.String)
                        {
                            var strValue = reader.GetString();
                            if (long.TryParse(strValue, out long parsedValue))
                                data.MatchId = parsedValue;
                        }
                        else
                        {
                            reader.Skip();
                        }

                        break;
                    case "teams":
                        data.Teams = _teamsConverter.Read(ref reader, typeof(Teams), options);
                        break;
                    case "index":
                        if (reader.TokenType == JsonTokenType.StartArray)
                        {
                            data.Index = JsonSerializer.Deserialize<List<object>>(ref reader, options);
                        }
                        else
                        {
                            reader.Skip();
                        }

                        break;
                    case "values":
                        if (reader.TokenType == JsonTokenType.StartObject)
                        {
                            data.Values =
                                JsonSerializer.Deserialize<Dictionary<string, StatValue>>(ref reader, options);
                        }
                        else
                        {
                            reader.Skip();
                        }

                        break;
                    case "types":
                        if (reader.TokenType == JsonTokenType.StartObject)
                        {
                            data.Types = JsonSerializer.Deserialize<Dictionary<string, string>>(ref reader, options);
                        }
                        else
                        {
                            reader.Skip();
                        }

                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }
            catch (JsonException)
            {
                reader.Skip();
            }
        }

        return data;
    }

    public override void Write(Utf8JsonWriter writer, MatchDetailsData value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("_doc", value.Doc);
        writer.WriteNumber("_matchid", value.MatchId);
        writer.WritePropertyName("teams");
        _teamsConverter.Write(writer, value.Teams, options);
        writer.WritePropertyName("index");
        JsonSerializer.Serialize(writer, value.Index, options);
        writer.WritePropertyName("values");
        JsonSerializer.Serialize(writer, value.Values, options);
        writer.WritePropertyName("types");
        JsonSerializer.Serialize(writer, value.Types, options);
        writer.WriteEndObject();
    }
}

public class JsonDictionaryStringNumberConverter : JsonConverter<Dictionary<string, object>>
{
    public override Dictionary<string, object> Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var result = new Dictionary<string, object>();

        if (reader.TokenType != JsonTokenType.StartObject)
            return result;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return result;

            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            var key = reader.GetString();
            reader.Read();

            object value = null;
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    value = reader.GetString();
                    break;
                case JsonTokenType.Number:
                    if (reader.TryGetInt32(out int intValue))
                        value = intValue;
                    else if (reader.TryGetInt64(out long longValue))
                        value = longValue;
                    else if (reader.TryGetDouble(out double doubleValue))
                        value = doubleValue;
                    break;
                case JsonTokenType.True:
                    value = true;
                    break;
                case JsonTokenType.False:
                    value = false;
                    break;
                case JsonTokenType.Null:
                    value = null;
                    break;
                case JsonTokenType.StartObject:
                    value = JsonSerializer.Deserialize<Dictionary<string, object>>(ref reader, options);
                    break;
                case JsonTokenType.StartArray:
                    value = JsonSerializer.Deserialize<List<object>>(ref reader, options);
                    break;
            }

            if (key != null)
                result[key] = value;
        }

        return result;
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<string, object> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var kvp in value)
        {
            writer.WritePropertyName(kvp.Key);
            if (kvp.Value == null)
                writer.WriteNullValue();
            else if (kvp.Value is string strValue)
                writer.WriteStringValue(strValue);
            else if (kvp.Value is int intValue)
                writer.WriteNumberValue(intValue);
            else if (kvp.Value is long longValue)
                writer.WriteNumberValue(longValue);
            else if (kvp.Value is double doubleValue)
                writer.WriteNumberValue(doubleValue);
            else if (kvp.Value is bool boolValue)
                writer.WriteBooleanValue(boolValue);
            else
                JsonSerializer.Serialize(writer, kvp.Value, options);
        }

        writer.WriteEndObject();
    }
}

public class StatValue
{
    [JsonPropertyName("name")] public string Name { get; set; }

    [JsonPropertyName("value")] public Dictionary<string, object> Value { get; set; }
}