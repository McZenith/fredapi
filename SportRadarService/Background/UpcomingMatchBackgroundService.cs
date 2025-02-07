using System.Text.Json;
using fredapi.Database;
using fredapi.Model.SportMatchesResponse;
using fredapi.SportRadarService.TokenService;
using Microsoft.AspNetCore.Http.HttpResults;
using MongoDB.Bson;
using MongoDB.Driver;

namespace fredapi.SportRadarService.Background;

public class UpcomingMatchBackgroundService(
    IServiceProvider serviceProvider,
    ILogger<UpcomingMatchBackgroundService> logger)
    : BackgroundService
{
    private static readonly Random Random = new();
    private readonly int _batchSize = new Random().Next(50,100);
    private const int MaxRetries = 3;
    private const int RetryDelayMs = 1000;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var matchService = scope.ServiceProvider.GetService<SportRadarService>();
                var mongoDbService = scope.ServiceProvider.GetRequiredService<MongoDbService>();
                var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
                await tokenService.GetSportRadarToken();

                var matchesCollection = mongoDbService.GetCollection<Model.EnrichedMatch>("DailyMatches");

                var indexKeysDefinition = Builders<Model.EnrichedMatch>.IndexKeys.Ascending(x => x.CreatedAt);
                var indexOptions = new CreateIndexOptions { ExpireAfter = TimeSpan.FromHours(36) };
                await matchesCollection.Indexes.CreateOneAsync(
                    new CreateIndexModel<Model.EnrichedMatch>(indexKeysDefinition, indexOptions),
                    cancellationToken: stoppingToken);

                // 1) Fetch all upcoming matches
                if (matchService == null) continue;
                var matchesResult = await matchService.GetUpcomingMatchesAsync();

                // 2) Check if the result is Ok<JsonDocument>
                if (matchesResult is Ok<JsonDocument> okResult && okResult.Value is not null)
                {
                    try
                    {
                        // 3) Extract matches with retry logic
                        var allMatches = await RetryWithExponentialBackoff(
                            () => ExtractAndValidateMatches(okResult.Value),
                            MaxRetries,
                            "match extraction"
                        );

                        if (!allMatches.Any())
                        {
                            logger.LogInformation("No valid matches to process after extraction attempts.");
                            continue;
                        }

                        // 4) Get all match IDs
                        var allMatchIds = allMatches.Select(m => m.Id.ToString()).ToList();

                        // 5) Check which matches already exist in DB (bulk operation)
                        var filter = Builders<Model.EnrichedMatch>.Filter.In(x => x.MatchId, allMatchIds);
                        var existingMatches = await matchesCollection
                            .Find(filter)
                            .Project(x => x.MatchId)
                            .ToListAsync(stoppingToken);

                        // 6) Find matches that need enrichment
                        var matchesToEnrich = allMatches
                            .Where(m => !existingMatches.Contains(m.Id.ToString()))
                            .ToList();

                        if (!matchesToEnrich.Any())
                        {
                            logger.LogInformation("All matches are already enriched.");
                            continue;
                        }

                        logger.LogInformation(
                            "Found {TotalMatches} matches, {NewMatches} need enrichment. Processing in batches of {BatchSize}",
                            allMatches.Count,
                            matchesToEnrich.Count,
                            _batchSize);

                        // 7) Process new matches in batches
                        foreach (var matchBatch in matchesToEnrich.Chunk(_batchSize))
                        {
                            if (stoppingToken.IsCancellationRequested) break;

                            try
                            {
                                // 8) Enrich matches in the current batch
                                var enrichmentTasks = matchBatch.Select(m =>
                                    EnrichMatchAsync(m, matchService));
                                var enrichedMatches = await Task.WhenAll(enrichmentTasks);

                                // 9) Prepare bulk write operations for the current batch
                                var upsertModels = enrichedMatches.Select(match =>
                                {
                                    var matchFilter = Builders<Model.EnrichedMatch>
                                        .Filter.Eq(x => x.MatchId, match.MatchId);
                                    return new ReplaceOneModel<Model.EnrichedMatch>(matchFilter, match)
                                    {
                                        IsUpsert = true
                                    };
                                }).ToList();

                                // 10) Execute bulk write for the current batch
                                var bulkResult = await matchesCollection.BulkWriteAsync(
                                    upsertModels,
                                    new BulkWriteOptions { IsOrdered = false },
                                    cancellationToken: stoppingToken
                                );

                                logger.LogInformation(
                                    "Batch upsert complete. Matched: {0}, Modified: {1}, Upserts: {2}",
                                    bulkResult.MatchedCount,
                                    bulkResult.ModifiedCount,
                                    bulkResult.Upserts.Count
                                );

                                await AddHumanLikeDelay(2000, 3000);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "Error processing match batch");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing matches after retries");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in match enrichment process");
            }
            finally
            {
                try
                {
                    await Task.Delay(TimeSpan.FromHours(3), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    logger.LogInformation("Match enrichment service is shutting down");
                }
            }
        }
    }

    private async Task<T> RetryWithExponentialBackoff<T>(
        Func<T> action,
        int maxRetries,
        string operationName)
    {
        var retryCount = 0;
        while (true)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                retryCount++;
                if (retryCount > maxRetries)
                {
                    logger.LogError(ex,
                        "Failed {Operation} after {RetryCount} attempts",
                        operationName,
                        retryCount);
                    throw;
                }

                var delay = RetryDelayMs * Math.Pow(2, retryCount - 1);
                logger.LogWarning(
                    "Attempt {RetryCount} of {MaxRetries} for {Operation} failed. Retrying in {Delay}ms. Error: {Error}",
                    retryCount,
                    maxRetries,
                    operationName,
                    delay,
                    ex.Message);

                await Task.Delay((int)delay);
            }
        }
    }

    private List<Match> ExtractAndValidateMatches(JsonDocument document)
    {
        var extractedMatches = ExtractMatches(document);
        return extractedMatches
            .Where(x => !string.IsNullOrWhiteSpace(x.SeasonId.ToString()))
            .Where(ValidateMatch)
            .ToList();
    }

    private List<Match> ExtractMatches(JsonDocument document)
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

            return response?.Doc
                .SelectMany(d => d.Data.Sport.RealCategories)
                .SelectMany(cat => cat.Tournaments)
                .SelectMany(t => t.Matches)
                .ToList() ?? new List<Match>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error extracting matches from JsonDocument");
            throw; // Rethrowing to trigger retry logic
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

    private static async Task AddHumanLikeDelay(int minMs, int maxMs)
    {
        var delay = Random.Next(minMs, maxMs);
        await Task.Delay(delay);
    }

    private async Task<Model.EnrichedMatch> EnrichMatchAsync(
        Match match,
        SportRadarService matchService)
    {
        var enrichedMatch = new Model.EnrichedMatch
        {
            Id = ObjectId.GenerateNewId(),
            MatchId = match.Id.ToString(),
            SeasonId = match.SeasonId.ToString(),
            Team1Id = match.Teams.Home.Id.ToString(),
            Team2Id = match.Teams.Away.Id.ToString(),
            CoreMatchData = JsonSerializer.Serialize(match),
            CreatedAt = DateTime.UtcNow,
            MatchTime = DateTime.Parse(match.Dt.Date)
        };

        // Group 1: Essential match information
        await FetchEssentialMatchInfo(matchService, enrichedMatch);
        await AddHumanLikeDelay(500, 1000);

        // Group 2: Season tables and standings
        await FetchSeasonTablesAndStandings(matchService, enrichedMatch);
        await AddHumanLikeDelay(800, 1200);

        // Group 3: Team-specific information
        await FetchTeamSpecificInfo(matchService, enrichedMatch);
        await AddHumanLikeDelay(600, 1000);

        // Group 4: Season statistics
        await FetchSeasonStatistics(matchService, enrichedMatch);
        await AddHumanLikeDelay(700, 1100);

        // Group 5: Match-specific details
        await FetchMatchSpecificDetails(matchService, enrichedMatch);
        await AddHumanLikeDelay(500, 900);

        // Group 6: Additional content and metadata
        await FetchAdditionalContent(matchService, enrichedMatch);
        await AddHumanLikeDelay(400, 800);

        // Group 7: Extended statistics
        await FetchExtendedStats(matchService, enrichedMatch);

        return enrichedMatch;
    }

    private async Task FetchExtendedStats(
        SportRadarService matchService,
        Model.EnrichedMatch enrichedMatch)
    {
        var tasks = new Dictionary<string, Task<IResult>>
        {
            {
                "TeamScoringConceding",
                matchService.GetStatsSeasonTeamscoringConcedingAsync(enrichedMatch.SeasonId, enrichedMatch.Team1Id)
            },
            { "Team1LastX", matchService.GetTeamLastXExtendedAsync(enrichedMatch.Team1Id) },
            { "Team2LastX", matchService.GetTeamLastXExtendedAsync(enrichedMatch.Team2Id) },
            { "Team1NextX", matchService.GetTeamNextXAsync(enrichedMatch.Team1Id) },
            { "Team2NextX", matchService.GetTeamNextXAsync(enrichedMatch.Team2Id) },
            { "MatchForm", matchService.GetStatsMatchFormAsync(enrichedMatch.MatchId) },
            { "TableSlice", matchService.GetStatsSeasonMatchTableSpliceAsync(enrichedMatch.SeasonId) },
            { "SeasonGoals", matchService.GetSeasonGoalsAsync(enrichedMatch.SeasonId) },
            { "TopCards", matchService.GetSeasonTopCardsAsync(enrichedMatch.SeasonId) }
        };

        foreach (var task in tasks.OrderBy(_ => Random.Next()))
        {
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

    private async Task FetchEssentialMatchInfo(
        SportRadarService matchService,
        Model.EnrichedMatch enrichedMatch)
    {
        var tasks = new Dictionary<string, Task<IResult>>
        {
            { "MatchInfo", matchService.GetMatchInfoAsync(enrichedMatch.MatchId) },
            { "MatchSquads", matchService.GetMatchSquadsAsync(enrichedMatch.MatchId) },
            { "MatchDetailsExtended", matchService.GetMatchDetailsExtendedAsync(enrichedMatch.MatchId) }
        };

        foreach (var task in tasks)
        {
            var result = await task.Value;
            if (result is Ok<JsonDocument> okResult)
            {
                enrichedMatch.GetType().GetProperty(task.Key)?.SetValue(
                    enrichedMatch,
                    okResult.Value?.RootElement.GetRawText());
            }

            await AddHumanLikeDelay(200, 400);
        }
    }

    private async Task FetchSeasonTablesAndStandings(
        SportRadarService matchService,
        Model.EnrichedMatch enrichedMatch)
    {
        var tasks = new Dictionary<string, Task<IResult>>
        {
            { "DynamicTable", matchService.GetSeasonDynamicTableAsync(enrichedMatch.SeasonId) },
            { "FormTable", matchService.GetStatsFormTableAsync(enrichedMatch.SeasonId) },
            { "SeasonLiveTable", matchService.GetSeasonLiveTableAsync(enrichedMatch.SeasonId) },
            { "Tables", matchService.GetSeasonTablesAsync(enrichedMatch.SeasonId) }
        };

        foreach (var task in tasks.OrderBy(_ => Random.Next()))
        {
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

    private async Task FetchTeamSpecificInfo(
        SportRadarService matchService,
        Model.EnrichedMatch enrichedMatch)
    {
        var tasks = new Dictionary<string, Task<IResult>>
        {
            { "TeamInfo1", matchService.GetStatsTeamInfoAsync(enrichedMatch.Team1Id) },
            { "TeamInfo2", matchService.GetStatsTeamInfoAsync(enrichedMatch.Team2Id) },
            { "TeamSquad1", matchService.GetStatsTeamSquadAsync(enrichedMatch.Team1Id) },
            { "TeamSquad2", matchService.GetStatsTeamSquadAsync(enrichedMatch.Team2Id) },
            { "LastXStatsTeam1", matchService.GetTeamLastXAsync(enrichedMatch.Team1Id) },
            { "LastXStatsTeam2", matchService.GetTeamLastXAsync(enrichedMatch.Team2Id) },
            { "TeamVersusRecent", matchService.GetTeamVersusRecentAsync(enrichedMatch.Team1Id, enrichedMatch.Team2Id) }
        };

        foreach (var task in tasks.OrderBy(_ => Random.Next()))
        {
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

    private async Task FetchSeasonStatistics(
        SportRadarService matchService,
        Model.EnrichedMatch enrichedMatch)
    {
        var tasks = new Dictionary<string, Task<IResult>>
        {
            { "OverUnderStats", matchService.GetStatsSeasonOverUnderAsync(enrichedMatch.SeasonId) },
            {
                "TeamPositionHistory",
                matchService.GetStatsSeasonTeamPositionHistoryAsync(enrichedMatch.SeasonId, enrichedMatch.Team1Id,
                    enrichedMatch.Team2Id)
            },
            {
                "TeamFixtures",
                matchService.GetStatsSeasonTeamFixturesAsync(enrichedMatch.SeasonId, enrichedMatch.Team1Id)
            },
            {
                "TeamDisciplinary",
                matchService.GetStatsSeasonTeamDisciplinaryAsync(enrichedMatch.SeasonId, enrichedMatch.Team1Id)
            },
            { "Fixtures", matchService.GetStatsSeasonFixturesAsync(enrichedMatch.SeasonId) },
            { "UniqueTeamStats", matchService.GetStatsSeasonUniqueTeamStatsAsync(enrichedMatch.SeasonId) },
            { "SeasonTopGoals", matchService.GetStatsSeasonTopGoalsAsync(enrichedMatch.SeasonId) }
        };

        foreach (var task in tasks.OrderBy(_ => Random.Next()))
        {
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

    private async Task FetchMatchSpecificDetails(
        SportRadarService matchService,
        Model.EnrichedMatch enrichedMatch)
    {
        var tasks = new Dictionary<string, Task<IResult>>
        {
            { "BookmakerOdds", matchService.GetMatchOddsAsync(enrichedMatch.MatchId) },
            { "MatchInsights", matchService.GetMatchInsightsAsync(enrichedMatch.MatchId) },
            { "MatchSituation", matchService.GetMatchSituationAsync(enrichedMatch.MatchId) },
            { "MatchTimeline", matchService.GetMatchTimelineAsync(enrichedMatch.MatchId) },
            { "MatchTimelineDelta", matchService.GetMatchTimelineDeltaAsync(enrichedMatch.MatchId) },
            { "CupBrackets", matchService.GetBracketsAsync(enrichedMatch.MatchId) }
        };

        foreach (var task in tasks.OrderBy(_ => Random.Next()))
        {
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

    private async Task FetchAdditionalContent(
        SportRadarService matchService,
        Model.EnrichedMatch enrichedMatch)
    {
        var tasks = new Dictionary<string, Task<IResult>>
        {
            { "MatchPhrases", matchService.GetMatchPhrasesAsync(enrichedMatch.MatchId) },
            { "MatchPhrasesDelta", matchService.GetMatchPhrasesDeltaAsync(enrichedMatch.MatchId) },
            { "MatchFunFacts", matchService.GetMatchFunFactsAsync(enrichedMatch.MatchId) },
            { "SeasonMeta", matchService.GetSeasonMetadataAsync(enrichedMatch.SeasonId) }
        };

        foreach (var task in tasks.OrderBy(_ => Random.Next()))
        {
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
}