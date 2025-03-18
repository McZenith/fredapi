using System.Text.Json;
using fredapi.Database;
using fredapi.SportRadarService.Background.ArbitrageLiveMatchBackgroundService;
using fredapi.SportRadarService.Background.UpcomingArbitrageBackgroundService;
using fredapi.SportRadarService.TokenService;
using MongoDB.Bson;
using MongoDB.Driver;
using ApiResponse = fredapi.SportRadarService.Background.UpcomingArbitrageBackgroundService.ApiResponse;
using Market = fredapi.SportRadarService.Background.UpcomingArbitrageBackgroundService.Market;

namespace fredapi.SportRadarService.Background;

public class UpcomingMatchEnrichmentService : BackgroundService
{
    private readonly ILogger<UpcomingMatchEnrichmentService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly HttpClient _httpClient;
    private const int MaxRetries = 3;
    private const int PageSize = 100;
    private const int PageLimit = 9;
    private static readonly Random Random = new();

    public UpcomingMatchEnrichmentService(
        ILogger<UpcomingMatchEnrichmentService> logger,
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
                var sportRadarService = scope.ServiceProvider.GetRequiredService<global::fredapi.SportRadarService.SportRadarService>();
                var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
                await tokenService.GetSportRadarToken();

                var enrichedMatchesCollection = mongoDbService.GetCollection<EnrichedSportMatch>("EnrichedSportMatches");

                // Create TTL index for automatic expiration
                await CreateTTLIndex(enrichedMatchesCollection, stoppingToken);

                // Fetch upcoming matches from API
                var matches = await FetchAllUpcomingMatchesAsync(stoppingToken);
                _logger.LogInformation($"Fetched {matches.Count} upcoming matches");

                // Filter out matches that have SRL in both teams
                var filteredMatches = matches
                    .Where(match => !HasSRLInBothTeams(match))
                    .ToList();

                _logger.LogInformation($"After filtering out SRL matches, {filteredMatches.Count} matches remain");

                // Skip matches that are already enriched
                var matchesToEnrich = await FilterOutAlreadyEnrichedMatches(mongoDbService, filteredMatches, stoppingToken);
                _logger.LogInformation($"After filtering already enriched matches, {matchesToEnrich.Count} matches need enrichment");

                if (matchesToEnrich.Any())
                {
                    // Enrich the matches
                    var enrichedMatches = await EnrichMatchesAsync(matchesToEnrich, sportRadarService, stoppingToken);

                    // Store enriched matches
                    await StoreEnrichedMatchesAsync(enrichedMatchesCollection, enrichedMatches, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in match enrichment processing");
            }
            finally
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Match enrichment service is shutting down");
                }
            }
        }
    }

    private async Task CreateTTLIndex(IMongoCollection<EnrichedSportMatch> collection, CancellationToken stoppingToken)
    {
        var indexKeysDefinition = Builders<EnrichedSportMatch>.IndexKeys.Ascending(x => x.CreatedAt);
        var indexOptions = new CreateIndexOptions { ExpireAfter = TimeSpan.FromHours(36) };
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<EnrichedSportMatch>(indexKeysDefinition, indexOptions),
            cancellationToken: stoppingToken);
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

                var validResults = results
                    .Where(r => r?.Data?.Tournaments != null)
                    .SelectMany(r => r.Data.Tournaments)
                    .SelectMany(t => t.Events)
                    .ToList();

                allMatches.AddRange(validResults);

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

                var url = BuildApiUrl(pageNum, timestamp);
                var response = await _httpClient.GetAsync(url, stoppingToken);

                if (IsInvalidResponse(response))
                    return null;

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

    private string BuildApiUrl(int pageNum, long timestamp) =>
        $"https://www.sportybet.com/api/ng/factsCenter/pcUpcomingEvents?" +
        $"sportId=sr%3Asport%3A1&marketId=1%2C18%2C19%2C20%2C10%2C29%2C11%2C26%2C36%2C14%2C60100" +
        $"&pageSize={PageSize}&pageNum={pageNum}&option=1&timeline=24&_t={timestamp}";

    private bool IsInvalidResponse(HttpResponseMessage response) =>
        response.StatusCode == System.Net.HttpStatusCode.NotFound ||
        response.StatusCode == System.Net.HttpStatusCode.Forbidden;

    private bool IsValidMatch(MatchData match) =>
        match != null &&
        !string.IsNullOrEmpty(match.EventId) &&
        !string.IsNullOrEmpty(match.HomeTeamId) &&
        !string.IsNullOrEmpty(match.AwayTeamId) &&
        !string.IsNullOrEmpty(match.HomeTeamName) &&
        !string.IsNullOrEmpty(match.AwayTeamName);

    private SportMatch CreateSportMatch(MatchData match)
    {
        return new SportMatch
        {
            Id = ObjectId.GenerateNewId(),
            MatchId = match.EventId.Split(':')[2],
            Teams = new SportTeams
            {
                Home = new SportTeam
                {
                    Id = match.HomeTeamId.Split(':')[2],
                    Name = match.HomeTeamName
                },
                Away = new SportTeam
                {
                    Id = match.AwayTeamId.Split(':')[2],
                    Name = match.AwayTeamName
                }
            },
            SeasonId = match.Sport.Category.Tournament.Id.Split(':')[2],
            TournamentName = match.Sport.Category.Tournament.Name ?? "Unknown Tournament",
            CreatedAt = DateTime.UtcNow,
            Markets = match.Markets,
            MatchTime = DateTime.TryParse(match.StartTime.ToString(), out DateTime matchTime)
                ? matchTime
                : DateTime.UtcNow
        };
    }

    private async Task<List<MatchData>> FilterOutAlreadyEnrichedMatches(
        MongoDbService mongoDbService,
        List<MatchData> matches,
        CancellationToken stoppingToken)
    {
        try
        {
            var enrichedCollection = mongoDbService.GetCollection<EnrichedSportMatch>("EnrichedSportMatches");

            // Get IDs of matches that are already enriched
            var enrichedMatchIds = await enrichedCollection.Find(FilterDefinition<EnrichedSportMatch>.Empty)
                .Project(x => x.MatchId)
                .ToListAsync(stoppingToken);

            // Return matches that haven't been enriched yet
            return matches.Where(match => !enrichedMatchIds.Contains(match.EventId)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error filtering out already enriched matches");
            return matches; // Return original list if error occurs
        }
    }

    private bool HasSRLInBothTeams(MatchData match)
    {
        var homeTeamHasSRL = match.HomeTeamName.Contains("SRL", StringComparison.OrdinalIgnoreCase);
        var awayTeamHasSRL = match.AwayTeamName.Contains("SRL", StringComparison.OrdinalIgnoreCase);

        return homeTeamHasSRL && awayTeamHasSRL;
    }

    private async Task<List<EnrichedSportMatch>> EnrichMatchesAsync(
        List<MatchData> matches,
        global::fredapi.SportRadarService.SportRadarService sportRadarService,
        CancellationToken stoppingToken)
    {
        var enrichedMatches = new List<EnrichedSportMatch>();

        foreach (var match in matches)
        {
            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                if (!IsValidMatch(match)) continue;

                _logger.LogInformation($"Enriching match {match.EventId}: {match.HomeTeamName} vs {match.AwayTeamName}");

                var sportMatch = CreateSportMatch(match);
                var enrichedMatch = await EnrichMatchAsync(sportMatch, sportRadarService);
                enrichedMatches.Add(enrichedMatch);

                // Add a human-like delay between enrichments
                await AddHumanLikeDelay(1500, 3000);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error enriching match {match.EventId}");
            }
        }

        return enrichedMatches;
    }

    private async Task<EnrichedSportMatch> EnrichMatchAsync(
        SportMatch match,
        global::fredapi.SportRadarService.SportRadarService sportRadarService)
    {
        var enrichedMatch = new EnrichedSportMatch
        {
            Id = ObjectId.GenerateNewId(),
            MatchId = match.MatchId,
            OriginalMatch = match,
            CreatedAt = DateTime.UtcNow,
            MatchTime = match.MatchTime,
            SeasonId = match.SeasonId,
            Markets = match.Markets,
            IsValid = true // Start assuming the match is valid
        };

        // Run tasks in parallel for faster processing
        var team1Id = enrichedMatch.OriginalMatch.Teams.Home.Id;
        var team2Id = enrichedMatch.OriginalMatch.Teams.Away.Id;
        var matchId = match.MatchId;
        var seasonId = match.SeasonId;

        // Prepare all tasks to run concurrently
        var tasks = new List<Task>();

        // Task 1: Fetch Team Position History
        tasks.Add(Task.Run(async () =>
        {
            await FetchTeamTableSlice(sportRadarService, enrichedMatch);
            await AddHumanLikeDelay(200, 400); // Shorter delay between concurrent tasks
        }));

        // Task 2 & 3: Fetch Team Last X Stats
        tasks.Add(Task.Run(async () =>
        {
            await FetchTeamLastXStats(sportRadarService, enrichedMatch);
            await AddHumanLikeDelay(200, 400);
        }));

        // Task 4: Fetch Team Versus Recent
        tasks.Add(Task.Run(async () =>
        {
            await FetchTeamVersusRecent(sportRadarService, enrichedMatch);
            await AddHumanLikeDelay(200, 400);
        }));

        // Task 5 & 6: Fetch Team Scoring Conceding
        tasks.Add(Task.Run(async () =>
        {
            await FetchTeamScoringConceding(sportRadarService, enrichedMatch);
            await AddHumanLikeDelay(200, 400);
        }));

        // Task 7 & 8: Fetch Team Last X Extended
        tasks.Add(Task.Run(async () =>
        {
            await FetchTeamLastXExtended(sportRadarService, enrichedMatch);
        }));

        // Wait for all tasks to complete
        await Task.WhenAll(tasks);

        // Check if we have enough valid data to consider this match properly enriched
        bool isComplete = true;

        // Check if all data models are present
        if (enrichedMatch.TeamTableSlice == null) isComplete = false;
        if (enrichedMatch.LastXStatsTeam1 == null) isComplete = false;
        if (enrichedMatch.LastXStatsTeam2 == null) isComplete = false;
        if (enrichedMatch.TeamVersusRecent == null) isComplete = false;
        if (enrichedMatch.Team1ScoringConceding == null) isComplete = false;
        if (enrichedMatch.Team2ScoringConceding == null) isComplete = false;
        if (enrichedMatch.Team1LastX == null) isComplete = false;
        if (enrichedMatch.Team2LastX == null) isComplete = false;

        // Set isValid flag based on completeness
        enrichedMatch.IsValid = isComplete;

        if (!isComplete)
        {
            _logger.LogWarning($"Match {enrichedMatch.MatchId} is considered invalid due to missing required data");
        }

        return enrichedMatch;
    }

    private async Task FetchTeamTableSlice(
        global::fredapi.SportRadarService.SportRadarService sportRadarService,
        EnrichedSportMatch enrichedMatch)
    {
        try
        {
            var matchId = enrichedMatch.MatchId;
            if (string.IsNullOrEmpty(matchId))
            {
                _logger.LogWarning($"Could not get match ID for match {enrichedMatch.MatchId}");
                return;
            }

            // Skip known problematic match IDs
            if (matchId == "57871789" || matchId == "111111111906998")
            {
                _logger.LogWarning($"Skipping known problematic match ID {matchId} for table slice");
                return;
            }

            // Use the more reliable match table slice API
            var result = await sportRadarService.GetStatsSeasonMatchTableSpliceAsync(matchId);
            if (result is Microsoft.AspNetCore.Http.HttpResults.Ok<JsonDocument> okResult)
            {
                var rawJson = okResult.Value?.RootElement.GetRawText() ?? "{}";

                // Log sample of raw JSON for debugging
                var logSample = rawJson.Length > 200 ? rawJson.Substring(0, 200) + "..." : rawJson;
                _logger.LogDebug($"Raw table slice JSON for match {matchId}: {logSample}");

                if (!IsErrorResponse(rawJson) && TryExtractData(rawJson, out var dataJson))
                {
                    try
                    {
                        // Try to deserialize into the model with proper options
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
                            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                            IgnoreReadOnlyProperties = true,
                            ReadCommentHandling = JsonCommentHandling.Skip,
                            AllowTrailingCommas = true
                        };

                        // Try parsing the JSON to check if it's valid before deserialization
                        try
                        {
                            using var doc = JsonDocument.Parse(dataJson);

                            // Handle array response
                            if (doc.RootElement.ValueKind == JsonValueKind.Array)
                            {
                                _logger.LogWarning($"Table slice JSON for match {matchId} has array root element. Converting to object format.");

                                // Create a new object with the array as tablerows
                                var arrayData = JsonSerializer.Deserialize<List<TableRowInfo>>(dataJson, options);

                                // Create a new table slice model with the array data
                                enrichedMatch.TeamTableSlice = new TeamTableSliceModel
                                {
                                    MatchId = matchId,
                                    TableRows = arrayData ?? new List<TableRowInfo>(),
                                    TotalRows = arrayData?.Count ?? 0,
                                    CurrentRound = 0,
                                    MaxRounds = 0
                                };

                                _logger.LogInformation($"Successfully processed array table data for match {matchId} with {arrayData?.Count ?? 0} teams");
                                return;
                            }

                            // Check if it has the expected structure
                            if (!doc.RootElement.TryGetProperty("tablerows", out var _))
                            {
                                _logger.LogWarning($"Table slice JSON for match {matchId} missing tablerows property");
                                // Create empty fallback model
                                enrichedMatch.TeamTableSlice = new TeamTableSliceModel
                                {
                                    MatchId = matchId,
                                    TableRows = new List<TableRowInfo>(),
                                    TotalRows = 0
                                };
                                return;
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogWarning(ex, $"Invalid JSON format in table slice for match {matchId}");
                            return;
                        }

                        // Add our robust converter
                        options.Converters.Add(new RootObjectConverter<TeamTableSliceModel>(_logger, $"team table slice for match {enrichedMatch.MatchId}"));

                        var tableSlice = JsonSerializer.Deserialize<TeamTableSliceModel>(dataJson, options);

                        if (tableSlice != null)
                        {
                            // Store the table slice directly
                            enrichedMatch.TeamTableSlice = tableSlice;

                            if (tableSlice.TableRows != null && tableSlice.TableRows.Any())
                            {
                                _logger.LogInformation($"Successfully processed table data for match {matchId} with {tableSlice.TableRows.Count} teams");
                            }
                            else
                            {
                                _logger.LogWarning($"No table rows found for match {matchId}");
                            }
                        }
                        else
                        {
                            _logger.LogWarning($"Failed to deserialize table slice for match {matchId}");
                            // Create empty fallback model
                            enrichedMatch.TeamTableSlice = new TeamTableSliceModel
                            {
                                MatchId = matchId,
                                TableRows = new List<TableRowInfo>(),
                                TotalRows = 0
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing table data for match {matchId}");
                        // Create empty fallback model
                        enrichedMatch.TeamTableSlice = new TeamTableSliceModel
                        {
                            MatchId = matchId,
                            TableRows = new List<TableRowInfo>(),
                            TotalRows = 0
                        };
                    }
                }
                else
                {
                    _logger.LogWarning($"Invalid response format for team table slice for match {enrichedMatch.MatchId}");
                    // Create empty fallback model
                    enrichedMatch.TeamTableSlice = new TeamTableSliceModel
                    {
                        MatchId = matchId,
                        TableRows = new List<TableRowInfo>(),
                        TotalRows = 0
                    };
                }
            }
            else
            {
                _logger.LogWarning($"Failed to get table slice for match {matchId}");
                // Create empty fallback model
                enrichedMatch.TeamTableSlice = new TeamTableSliceModel
                {
                    MatchId = matchId,
                    TableRows = new List<TableRowInfo>(),
                    TotalRows = 0
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching team table slice for match {enrichedMatch.MatchId}");
            // Create empty fallback model
            enrichedMatch.TeamTableSlice = new TeamTableSliceModel
            {
                MatchId = enrichedMatch.MatchId,
                TableRows = new List<TableRowInfo>(),
                TotalRows = 0
            };
        }
    }

    private async Task FetchTeamLastXStats(
        global::fredapi.SportRadarService.SportRadarService sportRadarService,
        EnrichedSportMatch enrichedMatch)
    {
        try
        {
            var team1Id = enrichedMatch.OriginalMatch.Teams.Home.Id;
            var team2Id = enrichedMatch.OriginalMatch.Teams.Away.Id;

            var team1Task = sportRadarService.GetTeamLastXAsync(team1Id);
            var team2Task = sportRadarService.GetTeamLastXAsync(team2Id);

            await Task.WhenAll(team1Task, team2Task);

            // Create consistent serialization options
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
            };

            if (team1Task.Result is Microsoft.AspNetCore.Http.HttpResults.Ok<JsonDocument> okTeam1Result)
            {
                var rawJson = okTeam1Result.Value?.RootElement.GetRawText() ?? "{}";

                if (!IsErrorResponse(rawJson) && TryExtractData(rawJson, out var dataJson))
                {
                    enrichedMatch.LastXStatsTeam1 = SafeDeserialize<TeamLastXStatsModel>(
                        dataJson,
                        options,
                        $"team 1 last X stats for match {enrichedMatch.MatchId}");
                }
                else
                {
                    _logger.LogWarning($"Invalid response format for team 1 last X stats for match {enrichedMatch.MatchId}");
                }
            }

            if (team2Task.Result is Microsoft.AspNetCore.Http.HttpResults.Ok<JsonDocument> okTeam2Result)
            {
                var rawJson = okTeam2Result.Value?.RootElement.GetRawText() ?? "{}";

                if (!IsErrorResponse(rawJson) && TryExtractData(rawJson, out var dataJson))
                {
                    enrichedMatch.LastXStatsTeam2 = SafeDeserialize<TeamLastXStatsModel>(
                        dataJson,
                        options,
                        $"team 2 last X stats for match {enrichedMatch.MatchId}");
                }
                else
                {
                    _logger.LogWarning($"Invalid response format for team 2 last X stats for match {enrichedMatch.MatchId}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching team last X stats for match {enrichedMatch.MatchId}");
        }
    }

    private async Task FetchTeamVersusRecent(
        global::fredapi.SportRadarService.SportRadarService sportRadarService,
        EnrichedSportMatch enrichedMatch)
    {
        try
        {
            var team1Id = enrichedMatch.OriginalMatch.Teams.Home.Id;
            var team2Id = enrichedMatch.OriginalMatch.Teams.Away.Id;

            var result = await sportRadarService.GetTeamVersusRecentAsync(team1Id, team2Id);
            if (result is Microsoft.AspNetCore.Http.HttpResults.Ok<JsonDocument> okResult)
            {
                var rawJson = okResult.Value?.RootElement.GetRawText() ?? "{}";

                if (!IsErrorResponse(rawJson) && TryExtractData(rawJson, out var dataJson))
                {
                    // Use consistent serialization options with more lenient settings
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
                        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                        IgnoreReadOnlyProperties = true,
                        ReadCommentHandling = JsonCommentHandling.Skip,
                        AllowTrailingCommas = true
                    };

                    enrichedMatch.TeamVersusRecent = SafeDeserialize<TeamVersusRecentModel>(
                        dataJson,
                        options,
                        $"team versus recent for match {enrichedMatch.MatchId}");
                }
                else
                {
                    _logger.LogWarning($"Invalid response format for team versus recent for match {enrichedMatch.MatchId}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching team versus recent for match {enrichedMatch.MatchId}");
        }
    }

    private async Task FetchTeamScoringConceding(
        global::fredapi.SportRadarService.SportRadarService sportRadarService,
        EnrichedSportMatch enrichedMatch)
    {
        try
        {
            var seasonId = enrichedMatch.SeasonId;
            if (string.IsNullOrEmpty(seasonId))
            {
                _logger.LogWarning($"Could not get season ID for match {enrichedMatch.MatchId}");
                return;
            }

            var team1Id = enrichedMatch.OriginalMatch.Teams.Home.Id;
            var team2Id = enrichedMatch.OriginalMatch.Teams.Away.Id;

            var team1Task = sportRadarService.GetStatsSeasonTeamscoringConcedingAsync(seasonId, team1Id);
            var team2Task = sportRadarService.GetStatsSeasonTeamscoringConcedingAsync(seasonId, team2Id);

            await Task.WhenAll(team1Task, team2Task);

            // Create consistent serialization options
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
            };

            if (team1Task.Result is Microsoft.AspNetCore.Http.HttpResults.Ok<JsonDocument> okTeam1Result)
            {
                var rawJson = okTeam1Result.Value?.RootElement.GetRawText() ?? "{}";

                if (!IsErrorResponse(rawJson) && TryExtractData(rawJson, out var dataJson))
                {
                    enrichedMatch.Team1ScoringConceding = SafeDeserialize<TeamScoringConcedingModel>(
                        dataJson,
                        options,
                        $"team 1 scoring conceding for match {enrichedMatch.MatchId}");
                }
                else
                {
                    _logger.LogWarning($"Invalid response format for team 1 scoring conceding for match {enrichedMatch.MatchId}");
                }
            }

            if (team2Task.Result is Microsoft.AspNetCore.Http.HttpResults.Ok<JsonDocument> okTeam2Result)
            {
                var rawJson = okTeam2Result.Value?.RootElement.GetRawText() ?? "{}";

                if (!IsErrorResponse(rawJson) && TryExtractData(rawJson, out var dataJson))
                {
                    enrichedMatch.Team2ScoringConceding = SafeDeserialize<TeamScoringConcedingModel>(
                        dataJson,
                        options,
                        $"team 2 scoring conceding for match {enrichedMatch.MatchId}");
                }
                else
                {
                    _logger.LogWarning($"Invalid response format for team 2 scoring conceding for match {enrichedMatch.MatchId}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching team scoring conceding for match {enrichedMatch.MatchId}");
        }
    }

    private async Task FetchTeamLastXExtended(
        global::fredapi.SportRadarService.SportRadarService sportRadarService,
        EnrichedSportMatch enrichedMatch)
    {
        try
        {
            var team1Id = enrichedMatch.OriginalMatch.Teams.Home.Id;
            var team2Id = enrichedMatch.OriginalMatch.Teams.Away.Id;

            var team1Task = sportRadarService.GetTeamLastXExtendedAsync(team1Id);
            var team2Task = sportRadarService.GetTeamLastXExtendedAsync(team2Id);

            await Task.WhenAll(team1Task, team2Task);

            // Create consistent serialization options
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
            };

            if (team1Task.Result is Microsoft.AspNetCore.Http.HttpResults.Ok<JsonDocument> okTeam1Result)
            {
                var rawJson = okTeam1Result.Value?.RootElement.GetRawText() ?? "{}";

                if (!IsErrorResponse(rawJson) && TryExtractData(rawJson, out var dataJson))
                {
                    enrichedMatch.Team1LastX = SafeDeserialize<TeamLastXExtendedModel>(
                        dataJson,
                        options,
                        $"team 1 last X extended for match {enrichedMatch.MatchId}");
                }
                else
                {
                    _logger.LogWarning($"Invalid response format for team 1 last X extended for match {enrichedMatch.MatchId}");
                }
            }

            if (team2Task.Result is Microsoft.AspNetCore.Http.HttpResults.Ok<JsonDocument> okTeam2Result)
            {
                var rawJson = okTeam2Result.Value?.RootElement.GetRawText() ?? "{}";

                if (!IsErrorResponse(rawJson) && TryExtractData(rawJson, out var dataJson))
                {
                    enrichedMatch.Team2LastX = SafeDeserialize<TeamLastXExtendedModel>(
                        dataJson,
                        options,
                        $"team 2 last X extended for match {enrichedMatch.MatchId}");
                }
                else
                {
                    _logger.LogWarning($"Invalid response format for team 2 last X extended for match {enrichedMatch.MatchId}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching team last X extended for match {enrichedMatch.MatchId}");
        }
    }

    private static async Task AddHumanLikeDelay(int minMs, int maxMs)
    {
        var delay = Random.Next(minMs, maxMs);
        await Task.Delay(delay);
    }

    private async Task StoreEnrichedMatchesAsync(
        IMongoCollection<EnrichedSportMatch> collection,
        List<EnrichedSportMatch> matches,
        CancellationToken stoppingToken)
    {
        try
        {
            if (!matches.Any())
            {
                _logger.LogInformation("No enriched matches to store");
                return;
            }

            // Filter out invalid matches
            var validMatches = matches.Where(m => m.IsValid).ToList();
            var invalidCount = matches.Count - validMatches.Count;

            if (invalidCount > 0)
            {
                _logger.LogInformation($"Filtered out {invalidCount} invalid matches");
            }

            if (!validMatches.Any())
            {
                _logger.LogInformation("No valid enriched matches to store");
                return;
            }

            // Save fully enriched matches to data.json for analysis
            await SaveFullyEnrichedMatchesToJsonAsync(validMatches);

            // First, get existing match documents to preserve their IDs
            var matchIds = validMatches.Select(m => m.MatchId).ToList();
            var existingMatches = await collection.Find(
                Builders<EnrichedSportMatch>.Filter.In(x => x.MatchId, matchIds)
            ).ToListAsync(stoppingToken);

            // Create a dictionary to quickly look up existing matches by matchId
            var existingMatchesDict = existingMatches.ToDictionary(m => m.MatchId, m => m);

            // Prepare bulk operations
            var bulkOps = new List<WriteModel<EnrichedSportMatch>>();

            foreach (var match in validMatches)
            {
                // Check if the match already exists
                if (existingMatchesDict.TryGetValue(match.MatchId, out var existingMatch))
                {
                    // Preserve the original ID
                    match.Id = existingMatch.Id;

                    // Use ReplaceOne with simple matchId filter
                    var filter = Builders<EnrichedSportMatch>.Filter.Eq(x => x.MatchId, match.MatchId);
                    bulkOps.Add(new ReplaceOneModel<EnrichedSportMatch>(filter, match));
                }
                else
                {
                    // For new matches, use InsertOne
                    bulkOps.Add(new InsertOneModel<EnrichedSportMatch>(match));
                }
            }

            if (bulkOps.Count > 0)
            {
                var result = await collection.BulkWriteAsync(
                            bulkOps,
                    new BulkWriteOptions { IsOrdered = false },
                    cancellationToken: stoppingToken
                );

                _logger.LogInformation(
                            "Enriched matches stored. Matched: {0}, Modified: {1}, Inserted: {2}",
                    result.MatchedCount,
                    result.ModifiedCount,
                            result.InsertedCount
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing enriched matches");
            throw;
        }
    }

    private async Task SaveFullyEnrichedMatchesToJsonAsync(List<EnrichedSportMatch> matches)
    {
        try
        {
            // Find matches that have ALL enrichment data available
            var fullyEnrichedMatches = matches.Where(m =>
                m.IsValid &&
                m.TeamTableSlice != null && m.TeamTableSlice.TableRows?.Count > 0 &&
                m.LastXStatsTeam1 != null && m.LastXStatsTeam1.LastMatches?.Count > 0 &&
                m.LastXStatsTeam2 != null && m.LastXStatsTeam2.LastMatches?.Count > 0 &&
                m.TeamVersusRecent != null && m.TeamVersusRecent.Matches?.Count > 0 &&
                m.Team1ScoringConceding != null &&
                m.Team2ScoringConceding != null &&
                m.Team1LastX != null && m.Team1LastX.Matches?.Count > 0 &&
                m.Team2LastX != null && m.Team2LastX.Matches?.Count > 0
            ).ToList();

            if (fullyEnrichedMatches.Count == 0)
            {
                _logger.LogInformation("No fully enriched matches found to save to JSON");
                return;
            }

            // Select one random match that has all the data
            var random = new Random();
            var sampleMatch = fullyEnrichedMatches[random.Next(fullyEnrichedMatches.Count)];

            // Create file path - in the app root directory
            var directory = AppDomain.CurrentDomain.BaseDirectory;
            var filePath = Path.Combine(directory, "data.json");

            // Serialize with indented formatting for readability
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.Serialize(sampleMatch, options);
            await File.WriteAllTextAsync(filePath, json);

            _logger.LogInformation($"Saved fully enriched match {sampleMatch.MatchId} to {filePath}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving fully enriched matches to JSON");
        }
    }

    // Check if response is an error response based on pattern matching
    private bool IsErrorResponse(string json)
    {
        if (string.IsNullOrEmpty(json))
            return true;

        try
        {
            // Check for patterns in the error response
            if (json.Contains("\"message\"") && json.Contains("\"code\"") && json.Contains("\"name\""))
            {
                try
                {
                    var errorResponse = JsonSerializer.Deserialize<fredapi.Model.Live.ErrorResponse.ErrorResponse>(
                        json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (errorResponse?.Doc != null && errorResponse.Doc.Any() && errorResponse.Doc[0].Data?.Name != null)
                    {
                        return true;
                    }
                }
                catch
                {
                    // Continue with other checks
                }
            }

            return false;
        }
        catch
        {
            return true;
        }
    }

    // Update the TryExtractData method to better handle edge cases
    private bool TryExtractData(string json, out string dataJson)
    {
        dataJson = null;

        if (string.IsNullOrEmpty(json))
            return false;

        // Special handling for problematic match ID
        if (json.Contains("111111111906998"))
        {
            _logger.LogWarning($"Detected problematic match ID 111111111906998. Using special handling.");
            dataJson = "{}"; // Return empty object for this match
            return true;
        }

        try
        {
            using var document = JsonDocument.Parse(json);

            // Log first 500 chars of JSON for debugging purposes
            var logSample = json.Length > 500 ? json.Substring(0, 500) + "..." : json;
            _logger.LogDebug($"Processing JSON: {logSample}");

            // Check if response has the expected structure with "doc" array
            if (!document.RootElement.TryGetProperty("doc", out var docElement) ||
                docElement.ValueKind != JsonValueKind.Array ||
                docElement.GetArrayLength() == 0)
            {
                // If there's no "doc" property, the API might have responded with a different format
                // Return the entire JSON as-is as a fallback and let the model handle it
                _logger.LogWarning($"Unexpected API response format (missing 'doc' array). Using raw JSON instead.");
                dataJson = json;
                return false;
            }

            // Get the first document and extract "data" property
            var firstDoc = docElement[0];
            if (!firstDoc.TryGetProperty("data", out var dataProperty) ||
                dataProperty.ValueKind == JsonValueKind.Null)
            {
                // If there's no "data" property, the API might have responded with an error or different format
                _logger.LogWarning($"Unexpected API response format (missing 'data' property in doc[0]). Using raw JSON instead.");
                dataJson = json;
                return false;
            }

            dataJson = dataProperty.GetRawText();
            return true;
        }
        catch (Exception ex)
        {
            // If parsing fails completely, just return the raw JSON as fallback
            _logger.LogWarning(ex, $"Failed to parse API response: {ex.Message}. Using raw JSON instead.");
            dataJson = json;
            return false;
        }
    }

    // Update the SafeDeserialize method to use the root object converter
    private T SafeDeserialize<T>(string json, JsonSerializerOptions options, string errorContext) where T : class, new()
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            _logger.LogWarning($"Empty JSON data for {errorContext}");
            return new T(); // Return empty object instead of null
        }

        // If this is our problematic match ID, return an empty object
        if (errorContext.Contains("111111111906998") || errorContext.Contains("111111111841726"))
        {
            _logger.LogWarning($"Using empty object for known problematic match ID in {errorContext}");
            return new T();
        }

        // Special handling for problematic JSON responses
        if (json == "{}" || json == "[]" || json == "null" || json.Length < 5)
        {
            _logger.LogWarning($"Empty or minimal JSON structure for {errorContext}");
            return new T();
        }

        // Log the first 100 characters of JSON for debugging (only in case of unusual structure)
        if (!json.StartsWith("{") && !json.StartsWith("["))
        {
            var logSample = json.Length > 100 ? json.Substring(0, 100) + "..." : json;
            _logger.LogWarning($"Unusual JSON structure for {errorContext}: {logSample}");
        }

        try
        {
            // Add our custom root object converter to handle malformed JSON
            var deserializerOptions = new JsonSerializerOptions(options);
            deserializerOptions.Converters.Add(new RootObjectConverter<T>(_logger, errorContext));

            // Try to deserialize with our robust converter
            var result = JsonSerializer.Deserialize<T>(json, deserializerOptions);

            // If result is null, return a new instance instead
            return result ?? new T();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, $"JSON deserialization error for {errorContext}: {ex.Message}");
            // Include first part of the JSON in the log
            var logSample = json.Length > 200 ? json.Substring(0, 200) + "..." : json;
            _logger.LogDebug($"Problematic JSON for {errorContext}: {logSample}");
            return new T(); // Return empty object instead of null
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unexpected error during deserialization for {errorContext}: {ex.Message}");
            return new T(); // Return empty object instead of null
        }
    }
}

public class SportMatch
{
    public ObjectId Id { get; set; }
    public string MatchId { get; set; }
    public string SeasonId { get; set; }
    public SportTeams Teams { get; set; }
    public string TournamentName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime MatchTime { get; set; }
    public List<MarketData> Markets { get; set; }
}

public class EnrichedSportMatch
{
    public ObjectId Id { get; set; }
    public string SeasonId { get; set; }
    public string MatchId { get; set; }
    public SportMatch OriginalMatch { get; set; }

    public List<MarketData> Markets { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime MatchTime { get; set; }
    public bool IsValid { get; set; } = true;

    // Enriched data models
    public TeamTableSliceModel? TeamTableSlice { get; set; }
    public TeamLastXStatsModel? LastXStatsTeam1 { get; set; }
    public TeamLastXStatsModel? LastXStatsTeam2 { get; set; }
    public TeamVersusRecentModel? TeamVersusRecent { get; set; }
    public TeamScoringConcedingModel? Team1ScoringConceding { get; set; }
    public TeamScoringConcedingModel? Team2ScoringConceding { get; set; }
    public TeamLastXExtendedModel? Team1LastX { get; set; }
    public TeamLastXExtendedModel? Team2LastX { get; set; }
}

public class SportTeams
{
    public SportTeam Home { get; set; }
    public SportTeam Away { get; set; }
}

public class SportTeam
{
    public string Id { get; set; }
    public string Name { get; set; }
}

// Team Position History models
public class TeamPositionHistoryModel
{
    [System.Text.Json.Serialization.JsonPropertyName("teams")]
    public Dictionary<string, TeamInfo> Teams { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("teamCount")]
    public int TeamCount { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("roundCount")]
    public int RoundCount { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("jersey")]
    public Dictionary<string, JerseyInfo> Jersey { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("season")]
    public SeasonInfo Season { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("tables")]
    public Dictionary<string, TableInfo> Tables { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("positionData")]
    public Dictionary<string, PositionData> PositionData { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("previousSeason")]
    public Dictionary<string, SeasonPos> PreviousSeason { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("currentSeason")]
    public Dictionary<string, List<SeasonPos>> CurrentSeason { get; set; } = new();
}

public class SeasonInfo
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("abbr")]
    public string Abbr { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("start")]
    public TimeInfo Start { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("end")]
    public TimeInfo End { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("year")]
    public string Year { get; set; }
}

public class TimeInfo
{
    [System.Text.Json.Serialization.JsonPropertyName("time")]
    public string Time { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("date")]
    public string Date { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }
}

public class TableInfo
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("abbr")]
    public string Abbr { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("seasonId")]
    public string SeasonId { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("start")]
    public TimeInfo Start { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("end")]
    public TimeInfo End { get; set; }
}

public class PositionData
{
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("shortName")]
    public string ShortName { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("cssClass")]
    public string CssClass { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("position")]
    public int Position { get; set; }
}

public class SeasonPos
{
    [System.Text.Json.Serialization.JsonPropertyName("round")]
    public int Round { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("position")]
    public int Position { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("seasonId")]
    public string SeasonId { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("matchId")]
    public string MatchId { get; set; }
}

public class JerseyInfo
{
    [System.Text.Json.Serialization.JsonPropertyName("base")]
    public string Base { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("sleeve")]
    public string Sleeve { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("number")]
    public string Number { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("type")]
    public string Type { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("real")]
    public bool Real { get; set; }
}

public class TeamInfo
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("mediumname")]
    public string MediumName { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("abbr")]
    public string Abbr { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("haslogo")]
    public bool HasLogo { get; set; }
}

// Team Scoring-Conceding models
public class TeamScoringConcedingModel
{
    [System.Text.Json.Serialization.JsonPropertyName("team")]
    public TeamInfo Team { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("stats")]
    public TeamStats Stats { get; set; } = new();
}

public class TeamStats
{
    [System.Text.Json.Serialization.JsonPropertyName("totalMatches")]
    public MatchCount TotalMatches { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("totalWins")]
    public MatchCount TotalWins { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("scoring")]
    public ScoringStats Scoring { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("conceding")]
    public ConcedingStats Conceding { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("averageGoalsByMinutes")]
    public Dictionary<string, double> AverageGoalsByMinutes { get; set; } = new();
}

public class MatchCount
{
    [System.Text.Json.Serialization.JsonPropertyName("total")]
    public int Total { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("home")]
    public int Home { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("away")]
    public int Away { get; set; }
}

public class ScoringStats
{
    [System.Text.Json.Serialization.JsonPropertyName("goalsScored")]
    public MatchCount GoalsScored { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("atLeastOneGoal")]
    public MatchCount AtLeastOneGoal { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("failedToScore")]
    public MatchCount FailedToScore { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("scoringAtHalftime")]
    public MatchCount ScoringAtHalftime { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("scoringAtFulltime")]
    public MatchCount ScoringAtFulltime { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("bothTeamsScored")]
    public MatchCount BothTeamsScored { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("goalsScoredAverage")]
    public Averages GoalsScoredAverage { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("atLeastOneGoalAverage")]
    public Averages AtLeastOneGoalAverage { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("failedToScoreAverage")]
    public Averages FailedToScoreAverage { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("scoringAtHalftimeAverage")]
    public Averages ScoringAtHalftimeAverage { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("scoringAtFulltimeAverage")]
    public Averages ScoringAtFulltimeAverage { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("goalMarginAtVictoryAverage")]
    public Averages GoalMarginAtVictoryAverage { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("halftimeGoalMarginAtVictoryAverage")]
    public Averages HalftimeGoalMarginAtVictoryAverage { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("bothTeamsScoredAverage")]
    public Averages BothTeamsScoredAverage { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("goalsByMinutes")]
    public Dictionary<string, Averages> GoalsByMinutes { get; set; } = new();
}

public class ConcedingStats
{
    [System.Text.Json.Serialization.JsonPropertyName("goalsConceded")]
    public MatchCount GoalsConceded { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("cleanSheets")]
    public MatchCount CleanSheets { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("goalsConcededFirstHalf")]
    public MatchCount GoalsConcededFirstHalf { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("goalsConcededAverage")]
    public Averages GoalsConcededAverage { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("cleanSheetsAverage")]
    public Averages CleanSheetsAverage { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("goalsConcededFirstHalfAverage")]
    public Averages GoalsConcededFirstHalfAverage { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("minutesPerGoalConceded")]
    public Averages MinutesPerGoalConceded { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("goalsByMinutes")]
    public Dictionary<string, Averages> GoalsByMinutes { get; set; } = new();
}

public class Averages
{
    [System.Text.Json.Serialization.JsonPropertyName("total")]
    public double Total { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("home")]
    public double Home { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("away")]
    public double Away { get; set; }
}

// Team LastX models
public class TeamLastXStatsModel
{
    [System.Text.Json.Serialization.JsonPropertyName("team")]
    public TeamInfo Team { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("matches")]
    public List<MatchStat> LastMatches { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("tournaments")]
    public Dictionary<string, TournamentInfo> Tournaments { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("uniquetournaments")]
    public Dictionary<string, UniqueTournamentInfo> UniqueTournaments { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("realcategories")]
    public Dictionary<string, RealCategoryInfo> RealCategories { get; set; } = new();
}

public class GenericStringConverter : System.Text.Json.Serialization.JsonConverter<string>
{
    public override string Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case System.Text.Json.JsonTokenType.String:
                return reader.GetString();
            case System.Text.Json.JsonTokenType.Number:
                return reader.GetInt64().ToString();
            case System.Text.Json.JsonTokenType.True:
                return "true";
            case System.Text.Json.JsonTokenType.False:
                return "false";
            case System.Text.Json.JsonTokenType.Null:
                return null;
            default:
                throw new System.Text.Json.JsonException($"Cannot convert {reader.TokenType} to string");
        }
    }

    public override void Write(System.Text.Json.Utf8JsonWriter writer, string value, System.Text.Json.JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}

public class StadiumIdConverter : System.Text.Json.Serialization.JsonConverter<string>
{
    public override string Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case System.Text.Json.JsonTokenType.String:
                return reader.GetString();
            case System.Text.Json.JsonTokenType.Number:
                return reader.GetInt64().ToString();
            default:
                throw new System.Text.Json.JsonException($"Cannot convert {reader.TokenType} to string");
        }
    }

    public override void Write(System.Text.Json.Utf8JsonWriter writer, string value, System.Text.Json.JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}

public class MatchStat
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    [System.Text.Json.Serialization.JsonConverter(typeof(GenericStringConverter))]
    public string Id { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("time")]
    public MatchTimeInfo Time { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("round")]
    public int? Round { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("result")]
    public ResultInfo Result { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("periods")]
    public Dictionary<string, ScoreInfo> Periods { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("seasonId")]
    [System.Text.Json.Serialization.JsonConverter(typeof(GenericStringConverter))]
    public string SeasonId { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("teams")]
    public MatchTeamsInfo Teams { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("neutralground")]
    public bool NeutralGround { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("comment")]
    public string Comment { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("stadiumid")]
    [System.Text.Json.Serialization.JsonConverter(typeof(GenericStringConverter))]
    public string StadiumId { get; set; }
}

public class MatchTimeInfo
{
    [System.Text.Json.Serialization.JsonPropertyName("time")]
    public string Time { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("date")]
    public string Date { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }
}

public class ResultInfo
{
    [System.Text.Json.Serialization.JsonPropertyName("home")]
    public int? Home { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("away")]
    public int? Away { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("period")]
    public string Period { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("winner")]
    public string Winner { get; set; }
}

public class ScoreInfo
{
    [System.Text.Json.Serialization.JsonPropertyName("home")]
    public int Home { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("away")]
    public int Away { get; set; }
}

public class MatchTeamsInfo
{
    [System.Text.Json.Serialization.JsonPropertyName("home")]
    public MatchTeam Home { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("away")]
    public MatchTeam Away { get; set; }
}

public class MatchTeam
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("mediumname")]
    public string MediumName { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("abbr")]
    public string Abbr { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("haslogo")]
    public bool HasLogo { get; set; }
}

public class TournamentInfo
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    [System.Text.Json.Serialization.JsonConverter(typeof(GenericStringConverter))]
    public string Id { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("abbr")]
    public string Abbr { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("seasonId")]
    [System.Text.Json.Serialization.JsonConverter(typeof(GenericStringConverter))]
    public string SeasonId { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("year")]
    public string Year { get; set; }
}

public class UniqueTournamentInfo
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("currentSeason")]
    [System.Text.Json.Serialization.JsonConverter(typeof(GenericStringConverter))]
    public string CurrentSeason { get; set; }
}

public class RealCategoryInfo
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("countryCode")]
    public CountryInfo CountryCode { get; set; }
}

public class CountryInfo
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("code")]
    public string Code { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("continent")]
    public string Continent { get; set; }
}

// Team LastX Extended models
public class TeamLastXExtendedModel
{
    [System.Text.Json.Serialization.JsonPropertyName("team")]
    public TeamInfo Team { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("matches")]
    public List<ExtendedMatchStat> Matches { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("tournaments")]
    public Dictionary<string, TournamentInfo> Tournaments { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("uniquetournaments")]
    public Dictionary<string, UniqueTournamentInfo> UniqueTournaments { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("realcategories")]
    public Dictionary<string, RealCategoryInfo> RealCategories { get; set; } = new();
}

public class ExtendedMatchStat : MatchStat
{
    [System.Text.Json.Serialization.JsonPropertyName("corners")]
    public CornersInfo Corners { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("firstgoal")]
    public string FirstGoal { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("lastgoal")]
    public string LastGoal { get; set; }
}

public class CornersInfo
{
    [System.Text.Json.Serialization.JsonPropertyName("home")]
    public int Home { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("away")]
    public int Away { get; set; }
}

// Team Versus Recent models
public class TeamVersusRecentModel
{
    [System.Text.Json.Serialization.JsonPropertyName("livematchid")]
    public string LiveMatchId { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("matches")]
    public List<HeadToHeadMatch> Matches { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("tournaments")]
    public Dictionary<string, TournamentInfo> Tournaments { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("realcategories")]
    public Dictionary<string, RealCategoryInfo> RealCategories { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("teams")]
    public Dictionary<string, TeamInfo> Teams { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("currentmanagers")]
    public Dictionary<string, List<ManagerInfo>> CurrentManagers { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("jersey")]
    public Dictionary<string, JerseyInfo> Jersey { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("next")]
    [System.Text.Json.Serialization.JsonConverter(typeof(NextMatchInfoConverter))]
    public NextMatchInfo? Next { get; set; }
}

public class HeadToHeadMatch : MatchStat
{
    [System.Text.Json.Serialization.JsonPropertyName("manager")]
    public ManagerData Manager { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("referee")]
    public List<RefereeInfo> Referee { get; set; } = new();
}

public class ManagerData
{
    [System.Text.Json.Serialization.JsonPropertyName("home")]
    public ManagerInfo Home { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("away")]
    public ManagerInfo Away { get; set; }
}

public class ManagerInfo
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("fullName")]
    public string FullName { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("birthDate")]
    public DateInfo BirthDate { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("nationality")]
    public CountryInfo Nationality { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("haslogo")]
    public bool HasLogo { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("membersince")]
    public DateInfo MemberSince { get; set; }
}

public class DateInfo
{
    [System.Text.Json.Serialization.JsonPropertyName("time")]
    public string Time { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("date")]
    public string Date { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }
}

public class RefereeInfo
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("fullName")]
    public string FullName { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("nationality")]
    public CountryInfo Nationality { get; set; }
}

public class NextMatchInfo : MatchStat
{
    [System.Text.Json.Serialization.JsonPropertyName("stadium")]
    public StadiumInfo Stadium { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("matchDifficultyRating")]
    public Dictionary<string, int> MatchDifficultyRating { get; set; } = new();
}

public class StadiumInfo
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("city")]
    public string City { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("country")]
    public string Country { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("capacity")]
    public string Capacity { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("countryCode")]
    public CountryInfo CountryCode { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("googleCoords")]
    public string GoogleCoords { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("homeTeams")]
    public List<TeamInfo> HomeTeams { get; set; } = new();
}

// Match Form models
public class MatchFormModel
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("teams")]
    public TeamsFormData Teams { get; set; }
}

public class TeamsFormData
{
    [System.Text.Json.Serialization.JsonPropertyName("home")]
    public TeamFormData Home { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("away")]
    public TeamFormData Away { get; set; }
}

public class TeamFormData
{
    [System.Text.Json.Serialization.JsonPropertyName("team")]
    public TeamInfo Team { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("form")]
    public List<FormEntry> Form { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("streak")]
    public StreakInfo Streak { get; set; }
}

public class FormEntry
{
    [System.Text.Json.Serialization.JsonPropertyName("type")]
    public string Type { get; set; } // W, L, D
}

public class StreakInfo
{
    [System.Text.Json.Serialization.JsonPropertyName("type")]
    public string Type { get; set; } // W, L, D

    [System.Text.Json.Serialization.JsonPropertyName("value")]
    public int Value { get; set; }
}

public class NextMatchInfoConverter : System.Text.Json.Serialization.JsonConverter<NextMatchInfo>
{
    public override NextMatchInfo Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
    {
        try
        {
            // Skip this property if it's null or of wrong type
            if (reader.TokenType == System.Text.Json.JsonTokenType.Null)
                return null;

            // Save reader state and try to read as object
            var originalReader = reader;
            var jsonObject = JsonDocument.ParseValue(ref reader);

            // If it's an object and has at least the basic required properties, deserialize
            if (jsonObject.RootElement.ValueKind == JsonValueKind.Object &&
                jsonObject.RootElement.TryGetProperty("id", out _) &&
                jsonObject.RootElement.TryGetProperty("teams", out _))
            {
                // Clone options but remove this converter to avoid infinite recursion
                var newOptions = new JsonSerializerOptions(options);
                var convertersWithoutThis = options.Converters.Where(c => !(c is NextMatchInfoConverter)).ToList();

                foreach (var converter in convertersWithoutThis)
                {
                    newOptions.Converters.Add(converter);
                }

                return JsonSerializer.Deserialize<NextMatchInfo>(jsonObject.RootElement.GetRawText(), newOptions);
            }

            // Return null if not a valid NextMatchInfo object
            return null;
        }
        catch
        {
            // In case of any exception, just return null
            return null;
        }
    }

    public override void Write(System.Text.Json.Utf8JsonWriter writer, NextMatchInfo value, System.Text.Json.JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        // Clone options but remove this converter to avoid infinite recursion
        var newOptions = new JsonSerializerOptions();
        foreach (var converter in options.Converters.Where(c => !(c is NextMatchInfoConverter)))
        {
            newOptions.Converters.Add(converter);
        }

        // Copy other relevant options
        newOptions.PropertyNameCaseInsensitive = options.PropertyNameCaseInsensitive;
        newOptions.PropertyNamingPolicy = options.PropertyNamingPolicy;
        newOptions.NumberHandling = options.NumberHandling;

        var json = JsonSerializer.Serialize(value, newOptions);
        using var jsonDoc = JsonDocument.Parse(json);
        jsonDoc.RootElement.WriteTo(writer);
    }
}

// Add a utility class to help with malformed JSON
public class RootObjectConverter<T> : System.Text.Json.Serialization.JsonConverter<T> where T : class, new()
{
    private readonly ILogger _logger;
    private readonly string _contextName;

    public RootObjectConverter(ILogger logger, string contextName)
    {
        _logger = logger;
        _contextName = contextName;
    }

    public override T Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
    {
        try
        {
            // If we encounter null, return empty object
            if (reader.TokenType == System.Text.Json.JsonTokenType.Null)
            {
                _logger.LogWarning($"Found null value while deserializing {_contextName}");
                return new T();
            }

            // If we encounter a primitive value, log and return empty object
            if (reader.TokenType != System.Text.Json.JsonTokenType.StartObject &&
                reader.TokenType != System.Text.Json.JsonTokenType.StartArray)
            {
                _logger.LogWarning($"Found unexpected token type {reader.TokenType} while deserializing {_contextName}");
                return new T();
            }

            // Try to parse the JSON document
            var doc = JsonDocument.ParseValue(ref reader);
            string rawJson = doc.RootElement.GetRawText();

            // Handle empty objects or arrays
            if (rawJson == "{}" || rawJson == "[]" || rawJson.Length < 5)
            {
                _logger.LogWarning($"Found empty or minimal JSON structure while deserializing {_contextName}");
                return new T();
            }

            // Create a clone of the options without this converter to avoid infinite recursion
            var newOptions = new JsonSerializerOptions(options);
            var convertersToKeep = options.Converters.Where(c => !(c is RootObjectConverter<T>)).ToList();
            newOptions.Converters.Clear();

            foreach (var converter in convertersToKeep)
            {
                newOptions.Converters.Add(converter);
            }

            // Special handling for known problematic match IDs
            if (_contextName.Contains("111111111841726") || _contextName.Contains("111111111906998"))
            {
                _logger.LogWarning($"Using empty object for known problematic match ID in {_contextName}");
                return new T();
            }

            // Handle array when object expected
            if (doc.RootElement.ValueKind == JsonValueKind.Array &&
                typeof(T) != typeof(List<>) && !typeof(T).IsArray)
            {
                _logger.LogWarning($"Found array when object expected for {_contextName}, attempting to convert");

                // If the array is empty, return empty object
                if (doc.RootElement.GetArrayLength() == 0)
                {
                    return new T();
                }

                // Try to use the first element if it's an object
                try
                {
                    var firstElement = doc.RootElement[0];
                    if (firstElement.ValueKind == JsonValueKind.Object)
                    {
                        return JsonSerializer.Deserialize<T>(firstElement.GetRawText(), newOptions) ?? new T();
                    }
                }
                catch
                {
                    // If extracting first element fails, return empty object
                    return new T();
                }
            }

            // Try standard deserialization
            try
            {
                var result = JsonSerializer.Deserialize<T>(rawJson, newOptions);
                return result ?? new T();
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, $"Error in standard deserialization for {_contextName}: {ex.Message}");

                // Return empty instance as fallback
                return new T();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unexpected error in RootObjectConverter for {_contextName}: {ex.Message}");
            return new T();
        }
    }

    public override void Write(System.Text.Json.Utf8JsonWriter writer, T value, System.Text.Json.JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        // Create options without this converter to avoid infinite recursion
        var newOptions = new JsonSerializerOptions(options);
        var convertersToKeep = options.Converters.Where(c => !(c is RootObjectConverter<T>)).ToList();
        newOptions.Converters.Clear();

        foreach (var converter in convertersToKeep)
        {
            newOptions.Converters.Add(converter);
        }

        var json = JsonSerializer.Serialize(value, newOptions);
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.WriteTo(writer);
    }
}

// First, add a new model class for table data
public class TeamTableSliceModel
{
    [System.Text.Json.Serialization.JsonPropertyName("matchid")]
    [System.Text.Json.Serialization.JsonConverter(typeof(GenericStringConverter))]
    public string MatchId { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("totalrows")]
    public int TotalRows { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("tablerows")]
    public List<TableRowInfo> TableRows { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("tournament")]
    public TournamentTableInfo Tournament { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("currentround")]
    public int? CurrentRound { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("maxrounds")]
    public int? MaxRounds { get; set; }

    // Helper methods to find team positions
    public TableRowInfo GetTeamPosition(string teamId) =>
        TableRows?.FirstOrDefault(row => row.Team?.Id == teamId);
}

public class TableRowInfo
{
    [System.Text.Json.Serialization.JsonPropertyName("pos")]
    public int Position { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("posHome")]
    public int HomePosition { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("posAway")]
    public int AwayPosition { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("team")]
    public TeamTableInfo Team { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("pointsTotal")]
    public int Points { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("winTotal")]
    public int Wins { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("drawTotal")]
    public int Draws { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("lossTotal")]
    public int Losses { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("goalsForTotal")]
    public int GoalsFor { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("goalsAgainstTotal")]
    public int GoalsAgainst { get; set; }
}

public class TeamTableInfo
{
    [System.Text.Json.Serialization.JsonPropertyName("_id")]
    [System.Text.Json.Serialization.JsonConverter(typeof(GenericStringConverter))]
    public string Id { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("mediumname")]
    public string MediumName { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("abbr")]
    public string Abbr { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("haslogo")]
    public bool HasLogo { get; set; }
}

public class TournamentTableInfo
{
    [System.Text.Json.Serialization.JsonPropertyName("_id")]
    [System.Text.Json.Serialization.JsonConverter(typeof(GenericStringConverter))]
    public string Id { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("seasonid")]
    [System.Text.Json.Serialization.JsonConverter(typeof(GenericStringConverter))]
    public string SeasonId { get; set; }
}