using System.Text.Json;
using fredapi.Model;
using fredapi.Model.ApiResponse;
using fredapi.Model.SportMatchesResponse;
using fredapi.SignalR;
using fredapi.SportRadarService.TokenService;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.SignalR;
using Match = fredapi.Model.SportMatchesResponse.Match;
using Team = fredapi.Model.SportMatchesResponse.Team;
using Teams = fredapi.Model.SportMatchesResponse.Teams;

namespace fredapi.SportRadarService.Background;

public class LiveMatchBackgroundService(
    IServiceProvider serviceProvider,
    ILogger<LiveMatchBackgroundService> logger,
    IHubContext<LiveMatchHub> hubContext)
    : BackgroundService
{
    
    private static readonly Random Random = new();
    private readonly int _batchSize = new Random().Next(50,100);
    private const int DelaySecs = 1;
    private bool _isInitialFetch;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _isInitialFetch = string.IsNullOrWhiteSpace(TokenService.TokenService.ApiToken);
                using var scope = serviceProvider.CreateScope();

                if (_isInitialFetch)
                {
                    var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
                    await tokenService.GetSportRadarToken();
                }
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
                    await Task.Delay(TimeSpan.FromMinutes(DelaySecs), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    logger.LogInformation("Live match service is shutting down");
                }
            }
        }
    }

    private static async Task AddHumanLikeDelay(int minMs, int maxMs)
    {
        var delay = Random.Next(minMs, maxMs);
        await Task.Delay(delay);
    }

    private async Task ProcessMatchesAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var matchService = scope.ServiceProvider.GetRequiredService<SportRadarService>();

        var matchesResult = await matchService.GetLiveMatchesAsync();
        if (matchesResult is not Ok<JsonDocument> okResult || okResult.Value is null)
        {
            logger.LogWarning("Failed to fetch live matches or result was null");
            return;
        }

        var matches = ExtractAndValidateMatches(okResult.Value);
        if (!matches.Any())
        {
            logger.LogInformation("No valid matches to process");
            return;
        }

        try
        {
            var client = new HttpClient();
            var response =
                await client.GetAsync(
                    "https://www.sportybet.com/api/ng/factsCenter/liveOrPrematchEvents?sportId=sr%3Asport%3A1&_t=1736116770164",
                    stoppingToken);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync(stoppingToken);
            var apiResponse = JsonSerializer.Deserialize<ApiResponse>(jsonString);
            
            var flattenedEvents = apiResponse.Data
                .SelectMany(t => t.Events.Select(e =>
                {
                    e.Sport.Category.Tournament.Name = t.Name;
                    return e;
                }))
                .ToList();

            var matchEvents = flattenedEvents
                .Where(x =>
                    !(x.Sport.Category.Tournament.Name?.ToLower().Contains("srl") ?? false) &&
                    !(x.HomeTeamName?.ToLower().Contains("srl") ?? false) &&
                    !(x.AwayTeamName?.ToLower().Contains("srl") ?? false)
                )
                .Select(x => new Match
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
                }).ToList();

            var desiredIds = matchEvents.Select(x => x.Id).ToHashSet();
            matches.RemoveAll(x => !desiredIds.Contains(x.Id));
            matches.AddRange(matchEvents.Where(x => !matches.Select(m => m.Id).Contains(x.Id)));
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error processing external match data");
        }

        logger.LogInformation($"Processing {matches.Count} matches");

        var enrichedMatches = new List<EnrichedMatch>();
        foreach (var matchBatch in matches.Chunk(_batchSize))
        {
            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                var enrichmentTasks = matchBatch.Select(m => EnrichMatchAsync(m, matchService, stoppingToken));
                var batchResults = await Task.WhenAll(enrichmentTasks);
                enrichedMatches.AddRange(batchResults);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing match batch");
            }
        }

        if (enrichedMatches.Any())
        {
            await StreamMatchesToClientsAsync(enrichedMatches.ToArray());
        }
    }

    private async Task<EnrichedMatch> EnrichMatchAsync(
        Match match,
        SportRadarService matchService,
        CancellationToken cancellationToken)
    {
        var enrichedMatch = new EnrichedMatch
        {
            MatchId = match.Id.ToString(),
            SeasonId = match.SeasonId.ToString(),
            Team1Id = match.Teams.Home.Id.ToString(),
            Team2Id = match.Teams.Away.Id.ToString(),
            CoreMatchData = JsonSerializer.Serialize(match)
        };

        try
        {
            // Group 1: Critical real-time updates (minimal delays)
            await FetchCriticalUpdates(matchService, enrichedMatch, cancellationToken);
            await AddHumanLikeDelay(300, 500);

            // Group 2: Live Statistics & Match Situation
            await FetchLiveStatistics(matchService, enrichedMatch, cancellationToken);
            await AddHumanLikeDelay(300, 500);

            // Group 3: Line-ups and Squad Information
            await FetchLineUpsAndSquads(matchService, enrichedMatch, cancellationToken);
            await AddHumanLikeDelay(300, 600);

            // Group 4: Head-to-Head Comparisons
            await FetchHeadToHeadComparisons(matchService, enrichedMatch, cancellationToken);
            await AddHumanLikeDelay(300, 500);

            // Group 6: Timeline and Commentary
            await FetchTimelineAndCommentary(matchService, enrichedMatch, cancellationToken);

            return enrichedMatch;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to enrich match {match.Id}");
            throw;
        }
    }

    private async Task FetchCriticalUpdates(
        SportRadarService matchService,
        EnrichedMatch enrichedMatch,
        CancellationToken cancellationToken)
    {
        var criticalTasks = new Dictionary<string, Task<IResult>>
        {
            { "MatchInfo", matchService.GetMatchInfoAsync(enrichedMatch.MatchId) },
            { "MatchTimelineDelta", matchService.GetMatchTimelineDeltaAsync(enrichedMatch.MatchId) },
            { "MatchTimeline", matchService.GetMatchTimelineAsync(enrichedMatch.MatchId) }
        };

        foreach (var task in criticalTasks)
        {
            if (cancellationToken.IsCancellationRequested) break;
            var result = await task.Value;
            if (result is Ok<JsonDocument> okResult)
            {
                enrichedMatch.GetType().GetProperty(task.Key)?.SetValue(
                    enrichedMatch,
                    okResult.Value?.RootElement.GetRawText());
            }

            await AddHumanLikeDelay(50, 150);
        }
    }

    private async Task FetchLiveStatistics(
        SportRadarService matchService,
        EnrichedMatch enrichedMatch,
        CancellationToken cancellationToken)
    {
        var statisticsTasks = new Dictionary<string, Task<IResult>>
        {
            { "MatchSituation", matchService.GetMatchSituationAsync(enrichedMatch.MatchId) },
            { "MatchDetailsExtended", matchService.GetMatchDetailsExtendedAsync(enrichedMatch.MatchId) },
            { "MatchForm", matchService.GetStatsMatchFormAsync(enrichedMatch.MatchId) },
            { "SeasonLiveTable", matchService.GetSeasonLiveTableAsync(enrichedMatch.SeasonId) },
        };

        foreach (var task in statisticsTasks.OrderBy(_ => Random.Next()))
        {
            if (cancellationToken.IsCancellationRequested) break;
            var result = await task.Value;
            if (result is Ok<JsonDocument> okResult)
            {
                enrichedMatch.GetType().GetProperty(task.Key)?.SetValue(
                    enrichedMatch,
                    okResult.Value?.RootElement.GetRawText());
            }

            await AddHumanLikeDelay(150, 400);
        }
    }

    private async Task FetchLineUpsAndSquads(
        SportRadarService matchService,
        EnrichedMatch enrichedMatch,
        CancellationToken cancellationToken)
    {
        var squadTasks = new Dictionary<string, Task<IResult>>
        {
            { "MatchSquads", matchService.GetMatchSquadsAsync(enrichedMatch.MatchId) },
        };

        foreach (var task in squadTasks.OrderBy(_ => Random.Next()))
        {
            if (cancellationToken.IsCancellationRequested) break;
            var result = await task.Value;
            if (result is Ok<JsonDocument> okResult)
            {
                enrichedMatch.GetType().GetProperty(task.Key)?.SetValue(
                    enrichedMatch,
                    okResult.Value?.RootElement.GetRawText());
            }

            await AddHumanLikeDelay(300, 500);
        }
    }

    private async Task FetchHeadToHeadComparisons(
        SportRadarService matchService,
        EnrichedMatch enrichedMatch,
        CancellationToken cancellationToken)
    {
        var h2hTasks = new Dictionary<string, Task<IResult>>
        {
            { "BookmakerOdds", matchService.GetMatchOddsAsync(enrichedMatch.MatchId) },
        };

        foreach (var task in h2hTasks.OrderBy(_ => Random.Next()))
        {
            if (cancellationToken.IsCancellationRequested) break;
            var result = await task.Value;
            if (result is Ok<JsonDocument> okResult)
            {
                enrichedMatch.GetType().GetProperty(task.Key)?.SetValue(
                    enrichedMatch,
                    okResult.Value?.RootElement.GetRawText());
            }

            await AddHumanLikeDelay(300, 500);
        }
    }

    private async Task FetchTimelineAndCommentary(
        SportRadarService matchService,
        EnrichedMatch enrichedMatch,
        CancellationToken cancellationToken)
    {
        var timelineTasks = new Dictionary<string, Task<IResult>>
        {
            { "MatchPhrases", matchService.GetMatchPhrasesAsync(enrichedMatch.MatchId) },
            { "MatchFunFacts", matchService.GetMatchFunFactsAsync(enrichedMatch.MatchId) },
            { "MatchPhrasesDelta", matchService.GetMatchPhrasesDeltaAsync(enrichedMatch.MatchId) },
        };

        foreach (var task in timelineTasks.OrderBy(_ => Random.Next()))
        {
            if (cancellationToken.IsCancellationRequested) break;
            var result = await task.Value;
            if (result is Ok<JsonDocument> okResult)
            {
                enrichedMatch.GetType().GetProperty(task.Key)?.SetValue(
                    enrichedMatch,
                    okResult.Value?.RootElement.GetRawText());
            }

            await AddHumanLikeDelay(250, 600);
        }
    }

    private async Task StreamMatchesToClientsAsync(EnrichedMatch[] matches)
    {
        if (!matches.Any()) return;

        try
        {
            var clientMatches = matches
                .Where(m => !string.IsNullOrEmpty(m.CoreMatchData)) // Filter out matches without core data
                .Select(match => new
                {
                    match.MatchId,
                    match.SeasonId,
                    Team1 = match.Team1Id,
                    Team2 = match.Team2Id,
                    CoreData = JsonSerializer.Deserialize<dynamic>(match.CoreMatchData ?? "{}"),
                    MatchInfo = JsonSerializer.Deserialize<dynamic>(match.MatchInfo ?? "{}"),
                    MatchTimeline = JsonSerializer.Deserialize<dynamic>(match.MatchTimeline ?? "{}"),
                    MatchTimelineDelta = JsonSerializer.Deserialize<dynamic>(match.MatchTimelineDelta ?? "{}"),
                    MatchDetailsExtended = JsonSerializer.Deserialize<dynamic>(match.MatchDetailsExtended ?? "{}"),
                    MatchSquads = JsonSerializer.Deserialize<dynamic>(match.MatchSquads ?? "{}"),
                    MatchSituation = JsonSerializer.Deserialize<dynamic>(match.MatchSituation ?? "{}"),
                    MatchForm = JsonSerializer.Deserialize<dynamic>(match.MatchForm ?? "{}"),
                    SeasonLiveTable = JsonSerializer.Deserialize<dynamic>(match.SeasonLiveTable ?? "{}"),
                    BookmakerOdds = JsonSerializer.Deserialize<dynamic>(match.BookmakerOdds ?? "{}"),
                    MatchPhrases = JsonSerializer.Deserialize<dynamic>(match.MatchPhrases ?? "{}"),
                    MatchFunFacts = JsonSerializer.Deserialize<dynamic>(match.MatchFunFacts ?? "{}"),
                    MatchPhrasesDelta = JsonSerializer.Deserialize<dynamic>(match.MatchPhrasesDelta ?? "{}"),
                    LastUpdated = DateTime.UtcNow
                })
                .ToList();

           
            await hubContext.Clients.All.SendAsync("ReceiveLiveMatches", clientMatches);
            logger.LogInformation($"Streamed {clientMatches.Count} matches to available clients");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error streaming matches to clients");
        }
    }

    private List<Match> ExtractAndValidateMatches(JsonDocument document)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var response = JsonSerializer.Deserialize<SportMatchesResponse>(
                document.RootElement.GetRawText(),
                options);

            var result = response?.Doc.SelectMany(d => d.Data.Sport.RealCategories)
                .SelectMany(cat => cat.Tournaments)
                .SelectMany(t => t.Matches)
                .Where(x => x.MatchStatus == "live")
                .Where(ValidateMatch)
                .ToList() ?? new List<Match>();

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error extracting matches from JsonDocument");
            return new List<Match>();
        }
    }

    private bool ValidateMatch(Match match)
    {
        if (match.Id == 0 || string.IsNullOrEmpty(match.SeasonId.ToString()) ||
            match.Teams.Home.Id == 0 || match.Teams.Away.Id == 0)
        {
            logger.LogWarning($"Match {match.Id} missing required fields");
            return false;
        }

        return true;
    }
}