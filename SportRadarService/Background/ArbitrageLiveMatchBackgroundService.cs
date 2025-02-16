using System.Text.Json;
using fredapi.Model.ApiResponse;
using fredapi.SignalR;
using Microsoft.AspNetCore.SignalR;

namespace fredapi.SportRadarService.Background.ArbitrageLiveMatchBackgroundService;

public partial class ArbitrageLiveMatchBackgroundService(
    ILogger<ArbitrageLiveMatchBackgroundService> logger,
    IHubContext<LiveMatchHub> hubContext)
    : BackgroundService
{
    private const int DelayMinutes = 1;
    private const decimal MaxAcceptableMargin = 10.0m;

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
                logger.LogError(ex, "Error in match processing");
            }
            finally
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(DelayMinutes), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    logger.LogInformation("Live match service is shutting down");
                }
            }
        }
    }

    private async Task StreamMatchesToClientsAsync(List<Match> matches)
    {
        if (!matches.Any()) return;

        try
        {
            var clientMatches = matches.Select(match => new
            {
                match.Id,
                match.SeasonId,
                Teams = new
                {
                    Home = new { match.Teams.Home.Id, match.Teams.Home.Name },
                    Away = new { match.Teams.Away.Id, match.Teams.Away.Name }
                },
                match.TournamentName,
                match.Score,
                match.Period,
                match.MatchStatus,
                match.PlayedTime,
                Markets = match.Markets.Select(m => new
                {
                    m.Id,
                    m.Description,
                    m.Specifier,
                    m.Margin,
                    m.ProfitPercentage,
                    Outcomes = m.Outcomes.Select(o => new
                    {
                        o.Id,
                        o.Description,
                        o.Odds,
                        o.StakePercentage
                    })
                }),
                LastUpdated = DateTime.UtcNow
            }).ToList();

            await hubContext.Clients.All.SendAsync("ReceiveArbitrageLiveMatches", clientMatches);
            logger.LogInformation($"Streamed {clientMatches.Count} matches with {clientMatches.Sum(m => m.Markets.Count())} arbitrage opportunities");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error streaming matches to clients");
        }
    }
}

public partial class ArbitrageLiveMatchBackgroundService
{
    private decimal CalculateMarginPercentage(List<Outcome> outcomes)
    {
        var impliedProbabilities = outcomes.Select(o => 1m / o.Odds);
        var margin = (impliedProbabilities.Sum() - 1m) * 100;
        return margin;
    }
    
    private bool IsValidMarketOutcomes(string marketDescription, int outcomeCount)
{
    return marketDescription.ToLower() switch
    {
        // Match Outcomes
        var m when m.Contains("1x2") || m.Contains("match result") => outcomeCount == 3,
        var m when m.Contains("double chance") => outcomeCount == 3,
        var m when m.Contains("draw no bet") || m.Contains("dnb") => outcomeCount == 2,
        
        // Goals Markets
        var m when m.Contains("over/under") || m.Contains("o/u") || m.Contains("total goals") => outcomeCount == 2,
        var m when m.Contains("alternative total goals") => outcomeCount == 2,
        var m when m.Contains("team total goals") => outcomeCount == 2,
        var m when m.Contains("exact goals") => outcomeCount == 2,
        
        // Both Teams Markets
        var m when m.Contains("gg/ng") || m.Contains("btts") || m.Contains("both teams to score") => outcomeCount == 2,
        var m when m.Contains("btts and over/under") => outcomeCount == 2,
        var m when m.Contains("btts and match result") => outcomeCount == 3,
        
        // Half Markets
        var m when m.Contains("1st half result") || m.Contains("2nd half result") => outcomeCount == 3,
        var m when m.Contains("half time/full time") || m.Contains("ht/ft") => outcomeCount == 3,
        var m when m.Contains("1st half goals") || m.Contains("2nd half goals") => outcomeCount == 2,
        var m when m.Contains("half with most goals") => outcomeCount == 3,
        
        // Asian Markets
        var m when m.Contains("asian handicap") || m.Contains("ah") => outcomeCount == 2,
        var m when m.Contains("asian total") => outcomeCount == 2,
        
        // Corner Markets
        var m when m.Contains("corner match") => outcomeCount == 3,
        var m when m.Contains("total corners") || m.Contains("corner line") => outcomeCount == 2,
        var m when m.Contains("corner handicap") => outcomeCount == 2,
        var m when m.Contains("first corner") || m.Contains("last corner") => outcomeCount == 3,
        
        // Card Markets
        var m when m.Contains("total cards") || m.Contains("booking points") => outcomeCount == 2,
        var m when m.Contains("team cards") => outcomeCount == 2,
        var m when m.Contains("red card") => outcomeCount == 2,
        
        // Score Markets
        var m when m.Contains("correct score") => outcomeCount == 2,
        var m when m.Contains("winning margin") => outcomeCount == 3,
        var m when m.Contains("team to score first") || m.Contains("team to score last") => outcomeCount == 3,
        var m when m.Contains("clean sheet") => outcomeCount == 2,
        
        // Specific Time Period Markets
        var m when m.Contains("result after") || m.Contains("score after") => outcomeCount == 3,
        var m when m.Contains("next goal") => outcomeCount == 3,
        var m when m.Contains("goals in period") => outcomeCount == 2,
        
        // Combination Markets
        var m when m.Contains("result and both teams to score") => outcomeCount == 3,
        var m when m.Contains("result and total goals") => outcomeCount == 3,
        var m when m.Contains("score cast") => outcomeCount == 2,
        
        // Qualifying/Progress Markets
        var m when m.Contains("to qualify") || m.Contains("to advance") => outcomeCount == 2,
        var m when m.Contains("method of victory") => outcomeCount == 3,
        
        // Default case for unrecognized markets
        _ => false
    };
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
    private async Task ProcessMatchesAsync(CancellationToken stoppingToken)
    {
        try
        {
            var client = new HttpClient();
            var response = await client.GetAsync(
                "https://www.sportybet.com/api/ng/factsCenter/liveOrPrematchEvents?sportId=sr%3Asport%3A1&_t=1736116770164",
                stoppingToken);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync(stoppingToken);
            var apiResponse = JsonSerializer.Deserialize<ApiResponse>(jsonString);

            if (apiResponse?.Data == null)
            {
                logger.LogWarning("No data received from API");
                return;
            }

            var flattenedEvents = apiResponse.Data
                .SelectMany(t => t.Events.Select(e =>
                {
                    e.Sport.Category.Tournament.Name = t.Name;
                    return e;
                }))
                .ToList();

            logger.LogInformation($"Total matches received from API: {flattenedEvents.Count}");

            var matchEvents = flattenedEvents
                .Select(x =>
                {
                    // First, create potential markets
                    var potentialMarkets = x.Markets
                        .Where(m => m.Status != 2 && m.Status != 3) // Exclude suspended and settled markets
                        .Where(m => m.Outcomes != null && 
                                    IsValidMarketOutcomes(m.Desc ?? "", m.Outcomes.Count))
                        .Select(m =>
                        {
                            var market = new Market
                            {
                                Id = m.Id,
                                Description = m.Desc,
                                Specifier = m.Specifier,
                                Outcomes = m.Outcomes
                                    .Where(o => o != null && o.IsActive == 1 && !string.IsNullOrEmpty(o.Odds))
                                    .Select(o => new Outcome
                                    {
                                        Id = o.Id ?? "",
                                        Description = o.Desc ?? "",
                                        Odds = decimal.Parse(o.Odds)
                                    }).ToList()
                            };

                            // Only proceed if we still have the required number of outcomes after filtering
                            if (!IsValidMarketOutcomes(market.Description, market.Outcomes.Count))
                            {
                                return (market: market, hasArbitrage: false);
                            }

                            var (hasArbitrage, stakePercentages, profitPercentage) = 
                                CalculateArbitrageOpportunity(market);
        
                            if (hasArbitrage)
                            {
                                // Add stake percentages to outcomes and profit percentage to market
                                for (int i = 0; i < market.Outcomes.Count; i++)
                                {
                                    market.Outcomes[i].StakePercentage = stakePercentages[i];
                                }
                                market.ProfitPercentage = profitPercentage;

                                logger.LogInformation(
                                    "Found arbitrage opportunity in match {MatchId}, market {MarketId} with {ProfitPercentage}% profit",
                                    x.EventId,
                                    market.Id,
                                    profitPercentage);
                            }

                            return (market, hasArbitrage);
                        })
                        .Where(m => m.market.Outcomes.Any())
                        .ToList();

                    // Then filter for arbitrage opportunities
                    var arbitrageMarkets = potentialMarkets
                        .Where(m => m.hasArbitrage)
                        .Select(m => m.market)
                        .ToList();

                    return new Match
                    {
                        Id = (int)(long.Parse(x.EventId.Split(':')[2]) % int.MaxValue),
                        Teams = new Teams
                        {
                            Home = new Team
                            {
                                Id = (int)(long.Parse(x.HomeTeamId.Split(':')[2]) % int.MaxValue),
                                Name = x.HomeTeamName,
                            },
                            Away = new Team
                            {
                                Id = (int)(long.Parse(x.AwayTeamId.Split(':')[2]) % int.MaxValue),
                                Name = x.AwayTeamName,
                            }
                        },
                        SeasonId = (int)(long.Parse(x.Sport.Category.Tournament.Id.Split(':')[2]) % int.MaxValue),
                        Result = null,
                        TournamentName = x.Sport.Category.Tournament.Name,
                        Score = x.SetScore,
                        Period = x.Period,
                        MatchStatus = x.MatchStatus,
                        PlayedTime = x.PlayedSeconds,
                        Markets = arbitrageMarkets
                    };
                })
                .Where(m => m.Markets.Any())
                .ToList();

            var totalArbitrageOpportunities = matchEvents.Sum(m => m.Markets.Count);
            logger.LogInformation($"Found {matchEvents.Count} matches with arbitrage opportunities");
            logger.LogInformation($"Total arbitrage opportunities: {totalArbitrageOpportunities}");
            logger.LogInformation($"Arbitrage opportunity rate: {(decimal)matchEvents.Count / flattenedEvents.Count:P2}");

            await StreamMatchesToClientsAsync(matchEvents);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching and processing matches");
        }
    }
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