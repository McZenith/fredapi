using System.Text.Json;
using System.Text.Json.Serialization;
using fredapi.SignalR;
using fredapi.Utils;
using fredapi.Model;
using fredapi.Model.MatchSituationStats;
using fredapi.Model.Live;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.SignalR;

namespace fredapi.SportRadarService.Background.ArbitrageLiveMatchBackgroundService;

public partial class ArbitrageLiveMatchBackgroundService : BackgroundService
{
    private readonly ILogger<ArbitrageLiveMatchBackgroundService> _logger;
    private readonly IHubContext<LiveMatchHub> _hubContext;
    private readonly HttpClient _httpClient;
    private readonly MarketValidator _marketValidator;
    private readonly IServiceProvider _serviceProvider;

    // Static properties to store the last messages sent to clients
    private static List<ClientMatch> _lastSentArbitrageMatches = new List<ClientMatch>();
    private static List<ClientMatch> _lastSentAllMatches = new List<ClientMatch>();

    private const int DelayMinutes = 1;
    private const decimal MaxAcceptableMargin = 10.0m;

    public ArbitrageLiveMatchBackgroundService(
        ILogger<ArbitrageLiveMatchBackgroundService> logger,
        IHubContext<LiveMatchHub> hubContext,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _hubContext = hubContext;
        _marketValidator = new MarketValidator(logger);
        _serviceProvider = serviceProvider;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
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
                    await Task.Delay(TimeSpan.FromMinutes(DelayMinutes), stoppingToken);
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
            var apiResponse = await FetchLiveMatchesAsync(stoppingToken);
            if (apiResponse?.Data == null)
            {
                _logger.LogWarning("No data received from API");
                return;
            }

            var flattenedEvents = FlattenEvents(apiResponse);
            _logger.LogInformation($"Processing {flattenedEvents.Count} matches");

            var matchEvents = ProcessEvents(flattenedEvents);
            LogMatchStatistics(matchEvents, flattenedEvents.Count);

            await StreamMatchesToClientsAsync(matchEvents, flattenedEvents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching and processing matches");
        }
    }

    private async Task<ApiResponse> FetchLiveMatchesAsync(CancellationToken stoppingToken)
    {
        var response = await _httpClient.GetAsync(
            "https://www.sportybet.com/api/ng/factsCenter/liveOrPrematchEvents?sportId=sr%3Asport%3A1&_t=1736116770164",
            stoppingToken);
        response.EnsureSuccessStatusCode();

        var jsonString = await response.Content.ReadAsStringAsync(stoppingToken);
        return JsonSerializer.Deserialize<ApiResponse>(jsonString);
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

    private List<Match> ProcessEvents(List<Event> events)
    {
        return events
            .Select(ProcessSingleEvent)
            .Where(m => m.Markets.Any())
            .Where(m => !m.Teams.Away.Name.ToUpper().Contains("SRL") || !m.Teams.Home.Name.ToUpper().Contains("SRL"))
            .ToList();
    }

    private List<Match> ProcessAllEvents(List<Event> events)
    {
        return events
            .Select(CreateMatchFromEvent)
            .Where(m => m.Markets.Any())
            .Where(m => !m.Teams.Away.Name.ToUpper().Contains("SRL") || !m.Teams.Home.Name.ToUpper().Contains("SRL"))
            .ToList();
    }

    private Match ProcessSingleEvent(Event eventData)
    {
        var potentialMarkets = ProcessMarkets(eventData);
        var arbitrageMarkets = potentialMarkets
            .Where(m => m.hasArbitrage)
            .Select(m => m.market)
            .ToList();

        return CreateMatch(eventData, arbitrageMarkets);
    }

    private Match CreateMatchFromEvent(Event eventData)
    {
        var markets = eventData.Markets
            .Where(m => IsValidMarketStatus(m))
            .Where(m => m.Desc?.ToLower() == "match result" || m.Desc?.ToLower() == "1x2") // Only include 1X2 markets
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

        return CreateMatch(eventData, markets);
    }

    private List<(Market market, bool hasArbitrage)> ProcessMarkets(Event eventData)
    {
        return eventData.Markets
            .Where(m => IsValidMarketStatus(m))
            .Where(m => _marketValidator.ValidateMarket(m))
            .Select(m => ProcessMarket(m, eventData.EventId))
            .Where(m => m.market.Outcomes.Any())
            .ToList();
    }

    private bool IsValidMarketStatus(MarketData market)
    {
        return market.Status != 2 && market.Status != 3; // Exclude suspended and settled markets
    }

    private (Market market, bool hasArbitrage) ProcessMarket(MarketData marketData, string matchId)
    {
        try
        {
            var market = new Market
            {
                Id = marketData.Id,
                Description = marketData.Desc,
                Specifier = marketData.Specifier,
                Outcomes = ProcessOutcomes(marketData.Outcomes),
                Favourite = marketData.Favourite,
            };

            if (!market.Outcomes.Any() || 
                !_marketValidator.ValidateMarket(marketData, market.Outcomes.Count))
            {
                return (market, false);
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
        return outcomes
            .Where(o => o != null && o.IsActive == 1 && !string.IsNullOrEmpty(o.Odds))
            .Select(CreateOutcome)
            .Where(o => o != null)
            .ToList();
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
            Markets = arbitrageMarkets
        };
    }

    private (bool hasArbitrage, List<decimal> stakePercentages, decimal profitPercentage) 
        CalculateArbitrageOpportunity(Market market)
    {
        if (!market.Outcomes.Any()) 
            return (false, new List<decimal>(), 0m);

        var margin = CalculateMarginPercentage(market.Outcomes);
        market.Margin = margin;

        if (margin > MaxAcceptableMargin)
            return (false, new List<decimal>(), 0m);

        var totalInverse = market.Outcomes.Sum(o => 1m / o.Odds);
        var profitPercentage = ((1 / totalInverse) - 1) * 100;

        if (profitPercentage <= 0) 
            return (false, new List<decimal>(), 0m);

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
        var margin = (impliedProbabilities.Sum() - 1m) * 100;
        return Math.Round(margin, 2);
    }

    private void LogMatchStatistics(List<Match> matches, int totalEvents)
    {
        var totalArbitrageOpportunities = matches.Sum(m => m.Markets.Count);
        _logger.LogInformation($"Found {matches.Count} matches with arbitrage opportunities");
        _logger.LogInformation($"Total arbitrage opportunities: {totalArbitrageOpportunities}");
        _logger.LogInformation($"Arbitrage opportunity rate: {(decimal)matches.Count / totalEvents:P2}");
    }

    // SignalR Streaming Methods
    private async Task StreamMatchesToClientsAsync(List<Match> arbitrageMatches, List<Event> allEvents)
    {
        if (!arbitrageMatches.Any() && !allEvents.Any()) return;

        try
        {
            // Process all matches first to get enriched data
            var allMatches = ProcessAllEvents(allEvents);
            _logger.LogInformation($"Processing {allMatches.Count} matches for enrichment");

            // Process matches in smaller batches to avoid overwhelming the API
            foreach (var matchBatch in allMatches.Chunk(5))
            {
                // Add a random delay before processing each batch (1-2 seconds)
                await AddHumanLikeDelay(1000, 2000);

                foreach (var match in matchBatch)
                {
                    try
                    {
                        // Fetch match situation and details data
                        await FetchMatchSituationAndDetails(match);
                        _logger.LogDebug($"Enriched match {match.Id} with situation and details");
                        // Add a random delay between matches (800ms-1.2s)
                        await AddHumanLikeDelay(800, 1200);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error fetching match data for match {MatchId}", match.Id);
                        // Add a longer delay after an error (2-3 seconds)
                        await AddHumanLikeDelay(2000, 3000);
                    }
                }
                // Add delay between batches (2-3 seconds)
                await AddHumanLikeDelay(2000, 3000);
            }

            // Create client matches with enriched data for all matches
            var clientAllMatches = allMatches.Select(match =>
            {
                var clientMatch = CreateClientMatch(match);
                _logger.LogDebug($"Created client match {match.Id} with situation: {match.MatchSituation != null}, details: {match.MatchDetails != null}");
                return clientMatch;
            }).ToList();
            _lastSentAllMatches = clientAllMatches;

            // Create client matches with enriched data for arbitrage matches
            var clientArbitrageMatches = arbitrageMatches.Select(match =>
            {
                // Find the enriched version of this match from allMatches
                var enrichedMatch = allMatches.FirstOrDefault(m => m.Id == match.Id);
                var clientMatch = CreateClientMatch(enrichedMatch ?? match);
                _logger.LogDebug($"Created arbitrage client match {match.Id} with situation: {enrichedMatch?.MatchSituation != null}, details: {enrichedMatch?.MatchDetails != null}");
                return clientMatch;
            }).ToList();
            _lastSentArbitrageMatches = clientArbitrageMatches;

            // Send both messages with enriched data
            await _hubContext.Clients.All.SendAsync("ReceiveArbitrageLiveMatches", clientArbitrageMatches);
            await _hubContext.Clients.All.SendAsync("ReceiveAllLiveMatches", clientAllMatches);

            _logger.LogInformation(
                "Streamed {ArbitrageMatchCount} arbitrage matches and {AllMatchCount} total matches with enriched data",
                clientArbitrageMatches.Count,
                clientAllMatches.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming matches to clients");
        }
    }

    private async Task FetchMatchSituationAndDetails(Match match)
    {
        using var scope = _serviceProvider.CreateScope();
        var matchService = scope.ServiceProvider.GetService<SportRadarService>();

        var tasks = new Dictionary<string, Task<IResult>>
        {
            { "MatchSituation", matchService.GetMatchSituationAsync(match.Id.ToString()) },
            { "MatchDetailsExtended", matchService.GetMatchDetailsExtendedAsync(match.Id.ToString()) }
        };

        foreach (var task in tasks.OrderBy(_ => Random.Shared.Next()))
        {
            try
            {
                await AddHumanLikeDelay(300, 500);
                var result = await task.Value;
                await AddHumanLikeDelay(300, 500);
                if (result is Ok<JsonDocument> okResult && okResult.Value != null)
                {
                    var rawJson = okResult.Value.RootElement.GetRawText();

                    if (rawJson.Contains("\"event\":\"exception\""))
                    {
                        _logger.LogWarning("Received exception response for match {MatchId} in {ResponseType}", match.Id, task.Key);
                        continue;
                    }

                    var doc = okResult.Value.RootElement.GetProperty("doc");
                    if (doc.GetArrayLength() == 0)
                    {
                        _logger.LogWarning("Empty doc array for match {MatchId} in {ResponseType}", match.Id, task.Key);
                        continue;
                    }

                    var firstDoc = doc[0];
                    if (!firstDoc.TryGetProperty("data", out var dataElement))
                    {
                        _logger.LogWarning("No data property found for match {MatchId} in {ResponseType}", match.Id, task.Key);
                        continue;
                    }

                    if (task.Key == "MatchSituation")
                    {
                        try
                        {
                            var response = JsonSerializer.Deserialize<fredapi.Model.Live.StatsMatchSituationResponse>(rawJson);

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
                    else if (task.Key == "MatchDetailsExtended")
                    {
                        try
                        {
                            if (firstDoc.TryGetProperty("event", out var eventProperty))
                            {
                                var eventValue = eventProperty.GetString();
                                if (eventValue == "exception")
                                {
                                    _logger.LogWarning("Received exception event for match {MatchId} in {ResponseType}", match.Id, task.Key);
                                    continue;
                                }
                            }

                            var options = new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true,
                                AllowTrailingCommas = true,
                                ReadCommentHandling = JsonCommentHandling.Skip,
                                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
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
                }
                await AddHumanLikeDelay(300, 500);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching match data for match {match.Id}");
                await AddHumanLikeDelay(1000, 2000);
            }
        }
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

    // Methods to get the last sent matches (can be called by hub methods)
    public static List<ClientMatch> GetLastSentArbitrageMatches() => _lastSentArbitrageMatches;
    public static List<ClientMatch> GetLastSentAllMatches() => _lastSentAllMatches;

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
}

public class ApiResponse
{
    [JsonPropertyName("bizCode")]
    public int BizCode { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; }

    [JsonPropertyName("data")]
    public List<TournamentData> Data { get; set; }
}

public class TournamentData
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("events")]
    public List<Event> Events { get; set; }
}

public class Event
{
    [JsonPropertyName("eventId")]
    public string EventId { get; set; }

    [JsonPropertyName("gameId")]
    public string GameId { get; set; }

    [JsonPropertyName("homeTeamId")]
    public string HomeTeamId { get; set; }

    [JsonPropertyName("homeTeamName")]
    public string HomeTeamName { get; set; }

    [JsonPropertyName("awayTeamId")]
    public string AwayTeamId { get; set; }

    [JsonPropertyName("awayTeamName")]
    public string AwayTeamName { get; set; }

    [JsonPropertyName("startTime")]
    public string StartTime { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("setScore")]
    public string SetScore { get; set; }

    [JsonPropertyName("period")]
    public string Period { get; set; }

    [JsonPropertyName("matchStatus")]
    public string MatchStatus { get; set; }

    [JsonPropertyName("playedSeconds")]
    public string PlayedSeconds { get; set; }

    [JsonPropertyName("markets")]
    public List<MarketData> Markets { get; set; }

    [JsonPropertyName("sport")]
    public Sport Sport { get; set; }
}

public class Sport
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("category")]
    public Category Category { get; set; }
}

public class Category
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("tournament")]
    public Tournament Tournament { get; set; }
}

public class Tournament
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }
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

    [JsonPropertyName("group")]
    public string Group { get; set; }

    [JsonPropertyName("groupId")]
    public string GroupId { get; set; }

    [JsonPropertyName("marketGuide")]
    public string MarketGuide { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("favourite")]
    public int Favourite { get; set; }

    [JsonPropertyName("outcomes")]
    public List<OutcomeData> Outcomes { get; set; }

    [JsonPropertyName("farNearOdds")]
    public int FarNearOdds { get; set; }

    [JsonPropertyName("sourceType")]
    public string SourceType { get; set; }

    [JsonPropertyName("availableScore")]
    public string AvailableScore { get; set; }

    [JsonPropertyName("banned")]
    public bool Banned { get; set; }
}

public class OutcomeData
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("desc")]
    public string Desc { get; set; }

    [JsonPropertyName("odds")]
    public string Odds { get; set; }

    [JsonPropertyName("probability")]
    public string Probability { get; set; }

    [JsonPropertyName("isActive")]
    public int IsActive { get; set; }
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
    [JsonPropertyName("queryUrl")]
    public string QueryUrl { get; set; }

    [JsonPropertyName("doc")]
    public List<MatchDetailsDoc> Doc { get; set; }
}

public class MatchDetailsDoc
{
    [JsonPropertyName("event")]
    public string Event { get; set; }

    [JsonPropertyName("_dob")]
    public long Dob { get; set; }

    [JsonPropertyName("_maxage")]
    public int MaxAge { get; set; }

    [JsonPropertyName("data")]
    public MatchDetailsData Data { get; set; }
}

public class MatchDetailsData
{
    [JsonPropertyName("_doc")]
    public string Doc { get; set; }

    [JsonPropertyName("_matchid")]
    public long MatchId { get; set; }

    [JsonPropertyName("teams")]
    public Teams Teams { get; set; }

    [JsonPropertyName("index")]
    public List<object> Index { get; set; }

    [JsonPropertyName("values")]
    public Dictionary<string, StatValue> Values { get; set; }

    [JsonPropertyName("types")]
    public Dictionary<string, string> Types { get; set; }
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
                            data.Values = JsonSerializer.Deserialize<Dictionary<string, StatValue>>(ref reader, options);
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
    public override Dictionary<string, object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("value")]
    public Dictionary<string, object> Value { get; set; }
}