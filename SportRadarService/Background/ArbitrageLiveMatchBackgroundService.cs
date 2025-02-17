using System.Text.Json;
using System.Text.Json.Serialization;
using fredapi.SignalR;
using Microsoft.AspNetCore.SignalR;

namespace fredapi.SportRadarService.Background.ArbitrageLiveMatchBackgroundService;

public partial class ArbitrageLiveMatchBackgroundService : BackgroundService
{
    private readonly ILogger<ArbitrageLiveMatchBackgroundService> _logger;
    private readonly IHubContext<LiveMatchHub> _hubContext;
    private readonly HttpClient _httpClient;
    private readonly MarketValidator _marketValidator;
    
    private const int DelayMinutes = 1;
    private const decimal MaxAcceptableMargin = 10.0m;

    public ArbitrageLiveMatchBackgroundService(
        ILogger<ArbitrageLiveMatchBackgroundService> logger,
        IHubContext<LiveMatchHub> hubContext)
    {
        _logger = logger;
        _hubContext = hubContext;
        _marketValidator = new MarketValidator(logger);
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
            _logger.LogInformation($"Total matches received from API: {flattenedEvents.Count}");

            var matchEvents = ProcessEvents(flattenedEvents);
            LogMatchStatistics(matchEvents, flattenedEvents.Count);

            await StreamMatchesToClientsAsync(matchEvents);
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

// Continuation of ArbitrageLiveMatchBackgroundService
    private (Market market, bool hasArbitrage) ProcessMarket(MarketData marketData, string matchId)
    {
        try
        {
            var market = new Market
            {
                Id = marketData.Id,
                Description = marketData.Desc,
                Specifier = marketData.Specifier,
                Outcomes = ProcessOutcomes(marketData.Outcomes)
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
    private async Task StreamMatchesToClientsAsync(List<Match> matches)
    {
        if (!matches.Any()) return;

        try
        {
            var clientMatches = matches.Select(match => new ClientMatch
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
                    ProfitPercentage = m.ProfitPercentage,
                    Outcomes = m.Outcomes.Select(o => new ClientOutcome
                    {
                        Id = o.Id,
                        Description = o.Description,
                        Odds = o.Odds,
                        StakePercentage = o.StakePercentage
                    }).ToList()
                }).ToList(),
                LastUpdated = DateTime.UtcNow
            }).ToList();

            await _hubContext.Clients.All.SendAsync("ReceiveArbitrageLiveMatches", clientMatches);
            
            _logger.LogInformation(
                "Streamed {MatchCount} matches with {ArbitrageCount} arbitrage opportunities",
                clientMatches.Count,
                clientMatches.Sum(m => m.Markets.Count));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming matches to clients");
        }
    }}

public class MarketValidator
{
    private readonly ILogger _logger;

    public MarketValidator(ILogger logger)
    {
        _logger = logger;
    }

    public bool ValidateMarket(MarketData market, int? outcomeCount = null)
    {
        try
        {
            if (!ValidateBasicStructure(market))
                return false;

            var actualOutcomeCount = outcomeCount ?? market.Outcomes.Count;
            
            return market.Desc?.ToLower() switch
            {
                // Match Outcomes
                var m when m.Contains("1x2") => ValidateMatchOutcomes(market),
                var m when m.Contains("double chance") => ValidateDoubleChance(market),
                var m when m.Contains("draw no bet") => ValidateDrawNoBet(market),
                
                // Goals Markets
                var m when m.Contains("over/under") => ValidateOverUnder(market),
                var m when m.Contains("team total goals") => ValidateTeamTotalGoals(market),
                
                // Both Teams Markets
                var m when m.Contains("gg/ng") || m.Contains("btts") => ValidateBothTeamsToScore(market),
                
                // Half Markets
                var m when m.Contains("1st half") || m.Contains("2nd half") => ValidateHalfMarket(market),
                
                // Next Goal Markets
                var m when m.Contains("next goal") => ValidateNextGoal(market),
                
                // Combo Markets
                var m when m.Contains("1x2 & gg/ng") => Validate1X2AndBTTS(market),
                var m when m.Contains("1x2 & over/under") => Validate1X2AndOverUnder(market),
                
                // Asian Markets
                var m when m.Contains("asian handicap") => ValidateAsianHandicap(market),
                var m when m.Contains("asian total") => ValidateAsianTotal(market),
                
                // Corner Markets
                var m when m.Contains("corner match") => ValidateCornerMatch(market),
                var m when m.Contains("total corners") => ValidateTotalCorners(market),
                
                _ => false
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error validating market {MarketId}", market.Id);
            return false;
        }
    }

    private bool ValidateBasicStructure(MarketData market)
    {
        if (market == null || 
            string.IsNullOrEmpty(market.Desc) || 
            market.Outcomes == null || 
            !market.Outcomes.Any())
            return false;

        return market.Outcomes.All(o => 
            !string.IsNullOrEmpty(o.Id) && 
            !string.IsNullOrEmpty(o.Desc) && 
            !string.IsNullOrEmpty(o.Odds) &&
            decimal.TryParse(o.Odds, out var odds) && 
            odds > 1.0m);
    }

    private bool ValidateMatchOutcomes(MarketData market)
    {
        if (market.Outcomes.Count != 3)
            return false;

        var descriptions = market.Outcomes.Select(o => o.Desc.ToLower()).ToList();
        return descriptions.Contains("home") && 
               descriptions.Contains("draw") && 
               descriptions.Contains("away");
    }

    private bool ValidateDoubleChance(MarketData market)
    {
        if (market.Outcomes.Count != 3)
            return false;

        var descriptions = market.Outcomes.Select(o => o.Desc.ToLower()).ToList();
        return descriptions.Contains("home or draw") && 
               descriptions.Contains("home or away") && 
               descriptions.Contains("draw or away");
    }

    private bool ValidateDrawNoBet(MarketData market)
    {
        if (market.Outcomes.Count != 2)
            return false;

        var descriptions = market.Outcomes.Select(o => o.Desc.ToLower()).ToList();
        return descriptions.Contains("home") && 
               descriptions.Contains("away");
    }

    private bool ValidateOverUnder(MarketData market)
    {
        if (market.Outcomes.Count != 2 || string.IsNullOrEmpty(market.Specifier))
            return false;

        if (!market.Specifier.StartsWith("total=") || 
            !decimal.TryParse(market.Specifier.Substring(6), out var total))
            return false;

        var descriptions = market.Outcomes.Select(o => o.Desc.ToLower()).ToList();
        return descriptions.Any(d => d.Contains("over")) && 
               descriptions.Any(d => d.Contains("under"));
    }

    private bool ValidateTeamTotalGoals(MarketData market)
    {
        if (market.Outcomes.Count != 2 || string.IsNullOrEmpty(market.Specifier))
            return false;

        if (!market.Specifier.StartsWith("total=") || 
            !decimal.TryParse(market.Specifier.Substring(6), out var total))
            return false;

        var descriptions = market.Outcomes.Select(o => o.Desc.ToLower()).ToList();
        return descriptions.Any(d => d.Contains("over")) && 
               descriptions.Any(d => d.Contains("under"));
    }

    private bool ValidateBothTeamsToScore(MarketData market)
    {
        if (market.Outcomes.Count != 2)
            return false;

        var descriptions = market.Outcomes.Select(o => o.Desc.ToLower()).ToList();
        return descriptions.Contains("yes") && descriptions.Contains("no");
    }

    private bool ValidateHalfMarket(MarketData market)
    {
        var marketDesc = market.Desc.ToLower();
        
        if (marketDesc.Contains("correct score"))
        {
            return market.Outcomes.All(o => 
                o.Desc.Contains(":") || o.Desc.ToLower() == "other");
        }
        
        if (marketDesc.Contains("result"))
        {
            return market.Outcomes.Count == 3;
        }

        return false;
    }

    private bool ValidateNextGoal(MarketData market)
    {
        if (market.Outcomes.Count != 3 || string.IsNullOrEmpty(market.Specifier))
            return false;

        if (!market.Specifier.StartsWith("goalnr=") || 
            !int.TryParse(market.Specifier.Substring(7), out var goalNumber))
            return false;

        var descriptions = market.Outcomes.Select(o => o.Desc.ToLower()).ToList();
        return descriptions.Contains("home") && 
               descriptions.Contains("none") && 
               descriptions.Contains("away");
    }

    private bool Validate1X2AndBTTS(MarketData market)
    {
        if (market.Outcomes.Count != 6)
            return false;

        var descriptions = market.Outcomes.Select(o => o.Desc.ToLower()).ToList();
        return descriptions.Contains("home & yes") &&
               descriptions.Contains("home & no") &&
               descriptions.Contains("draw & yes") &&
               descriptions.Contains("draw & no") &&
               descriptions.Contains("away & yes") &&
               descriptions.Contains("away & no");
    }

    private bool Validate1X2AndOverUnder(MarketData market)
    {
        if (market.Outcomes.Count != 6 || string.IsNullOrEmpty(market.Specifier))
            return false;

        if (!market.Specifier.StartsWith("total=") || 
            !decimal.TryParse(market.Specifier.Substring(6), out var total))
            return false;

        var descriptions = market.Outcomes.Select(o => o.Desc.ToLower()).ToList();
        return descriptions.Contains("home & over") &&
               descriptions.Contains("home & under") &&
               descriptions.Contains("draw & over") &&
               descriptions.Contains("draw & under") &&
               descriptions.Contains("away & over") &&
               descriptions.Contains("away & under");
    }

    private bool ValidateAsianHandicap(MarketData market) =>
        market.Outcomes.Count == 2 && !string.IsNullOrEmpty(market.Specifier);

    private bool ValidateAsianTotal(MarketData market) =>
        market.Outcomes.Count == 2 && !string.IsNullOrEmpty(market.Specifier);

    private bool ValidateCornerMatch(MarketData market) =>
        market.Outcomes.Count == 3;

    private bool ValidateTotalCorners(MarketData market) =>
        market.Outcomes.Count == 2 && !string.IsNullOrEmpty(market.Specifier);
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