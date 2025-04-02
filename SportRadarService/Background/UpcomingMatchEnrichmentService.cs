using System.Text.Json;
using System.Text.Json.Serialization;
using fredapi.Database;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using fredapi.SportRadarService.Background.ArbitrageLiveMatchBackgroundService;
using fredapi.SportRadarService.Background.UpcomingArbitrageBackgroundService;
using ApiResponse = fredapi.SportRadarService.Background.UpcomingArbitrageBackgroundService.ApiResponse;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Runtime.Serialization;

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
        try
        {
            _logger.LogInformation("UpcomingMatch Enrichment Service starting.");

            var mongoDbService = _serviceProvider.GetRequiredService<MongoDbService>();
            var collection = mongoDbService.GetCollection<EnrichedSportMatch>("EnrichedSportMatches");

            // Create TTL index (if not exists) to automatically delete old matches
            await CreateTTLIndex(collection, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Fetching upcoming matches...");

                    // Create a new instance of SportRadarService for this run
                    using var scope = _serviceProvider.CreateScope();
                    var sportRadarService = scope.ServiceProvider.GetRequiredService<global::fredapi.SportRadarService.SportRadarService>();

                    // Fetch matches with retry logic
                    var matches = await RetryWithExponentialBackoff(
                        () => FetchAllUpcomingMatchesAsync(stoppingToken),
                        MaxRetries,
                        "fetch upcoming matches"
                    );

                    _logger.LogInformation($"Found {matches.Count} upcoming matches.");

                    if (matches.Count == 0)
                    {
                        _logger.LogInformation("No matches found. Waiting before next attempt.");
                        await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
                        continue;
                    }

                    // Get all match IDs for bulk filtering
                    var allMatchIds = matches.Select(m => m.EventId).ToList();

                    // Filter out matches that have already been enriched (bulk operation)
                    var existingMatchIds = await collection
                        .FindWithDiskUse(Builders<EnrichedSportMatch>.Filter.In(x => x.MatchId, allMatchIds.Select(id => id.Split(':')[2])))
                        .Project(x => x.MatchId)
                        .ToListAsync(stoppingToken);

                    // Find matches that need enrichment
                    var matchesToEnrich = matches
                        .Where(m => !existingMatchIds.Contains(m.EventId.Split(':')[2]))
                        .ToList();

                    _logger.LogInformation($"After filtering, {matchesToEnrich.Count} matches need enrichment.");

                    if (matchesToEnrich.Count > 0)
                    {
                        // Process in smaller batches to improve performance
                        int batchSize = 10; // Process 10 matches per batch
                        _logger.LogInformation($"Processing {matchesToEnrich.Count} matches in batches of {batchSize}");

                        foreach (var batch in matchesToEnrich.Chunk(batchSize))
                        {
                            if (stoppingToken.IsCancellationRequested) break;

                            try
                            {
                                // Process matches in parallel within each batch
                                var enrichmentTasks = batch
                                    .Where(IsValidMatch)
                                    .Select(match =>
                                    {
                                        var sportMatch = CreateSportMatch(match);
                                        return EnrichMatchAsync(sportMatch, sportRadarService);
                                    })
                                    .ToList();

                                // Wait for all enrichment tasks to complete
                                var enrichedMatches = await Task.WhenAll(enrichmentTasks);
                                var validMatches = enrichedMatches.Where(m => m.IsValid).ToList();

                                if (validMatches.Any())
                                {
                                    // Store the batch of enriched matches in database
                                    await StoreEnrichedMatchesAsync(collection, validMatches, stoppingToken);
                                    _logger.LogInformation($"Successfully stored {validMatches.Count} enriched matches in database");
                                }

                                // Add delay between batches to avoid overwhelming API
                                await AddHumanLikeDelay(2000, 3000);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error processing batch of matches");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in match enrichment process");
                }
                finally
                {
                    try
                    {
                        // Wait for 6 hours before the next run
                        _logger.LogInformation("Enrichment cycle complete. Next run in 6 hours.");
                        var nextRunTime = DateTime.Now.AddHours(6);
                        _logger.LogInformation($"Next enrichment run scheduled for: {nextRunTime}");
                        await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("UpcomingMatch Enrichment Service is shutting down");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in UpcomingMatchEnrichmentService");
            throw;
        }
    }

    private async Task<T> RetryWithExponentialBackoff<T>(
        Func<Task<T>> action,
        int maxRetries,
        string operationName)
    {
        var retryCount = 0;
        var retryDelayMs = 1000; // Start with 1 second delay

        while (true)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                retryCount++;
                if (retryCount > maxRetries)
                {
                    _logger.LogError(ex,
                        "Failed {Operation} after {RetryCount} attempts",
                        operationName,
                        retryCount);
                    throw;
                }

                var delay = retryDelayMs * Math.Pow(2, retryCount - 1);
                _logger.LogWarning(
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

        // Create tasks for all pages to fetch concurrently in chunks
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
            MatchTime = GetUtcDateTime(match.StartTime)
        };
    }

    private DateTime GetUtcDateTime(dynamic startTime)
    {
        try
        {
            // Handle JsonElement type from System.Text.Json
            if (startTime is System.Text.Json.JsonElement jsonElement)
            {
                // Handle null or undefined JSON values
                if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Null ||
                    jsonElement.ValueKind == System.Text.Json.JsonValueKind.Undefined)
                {
                    _logger.LogWarning("Match StartTime JsonElement is null or undefined, using fallback time");
                    return DateTime.UtcNow.AddDays(1);
                }

                // Handle number values (timestamps)
                if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Number)
                {
                    // Try to get as long first (for timestamps)
                    if (jsonElement.TryGetInt64(out long longValue))
                    {
                        // Convert to seconds if in milliseconds
                        if (longValue > 10000000000) // Typical millisecond timestamp is 13 digits
                            longValue /= 1000;

                        return DateTimeOffset.FromUnixTimeSeconds(longValue).UtcDateTime;
                    }
                }

                // Handle string values
                if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    string stringValue = jsonElement.GetString();

                    // Try to parse as numeric timestamp first
                    if (!string.IsNullOrEmpty(stringValue) &&
                        long.TryParse(stringValue, out long numericTime))
                    {
                        // Convert to seconds if in milliseconds
                        if (numericTime > 10000000000) // Typical millisecond timestamp is 13 digits
                            numericTime /= 1000;

                        return DateTimeOffset.FromUnixTimeSeconds(numericTime).UtcDateTime;
                    }

                    // Try to parse as formatted date
                    if (!string.IsNullOrEmpty(stringValue) &&
                        DateTime.TryParse(stringValue, out DateTime parsedTime))
                    {
                        // Ensure we're working with UTC time
                        if (parsedTime.Kind != DateTimeKind.Utc)
                            return DateTime.SpecifyKind(parsedTime, DateTimeKind.Utc);
                        return parsedTime;
                    }
                }

                // If we couldn't handle the specific JsonElement type
                _logger.LogWarning($"Unhandled JsonElement type: {jsonElement.ValueKind}, using fallback time");
                return DateTime.UtcNow.AddDays(1);
            }

            // Handle null case (original logic)
            if (startTime == null)
            {
                _logger.LogWarning("Match StartTime is null, using fallback time");
                return DateTime.UtcNow.AddDays(1); // Use tomorrow as fallback
            }

            // Handle Unix timestamp (in milliseconds)
            if (startTime is long longTime)
            {
                // Convert to seconds if in milliseconds
                if (longTime > 10000000000) // Typical millisecond timestamp is 13 digits
                    longTime /= 1000;

                return DateTimeOffset.FromUnixTimeSeconds(longTime).UtcDateTime;
            }

            // Try to parse as string
            if (startTime is string strTime)
            {
                // Try to parse as numeric timestamp first
                if (long.TryParse(strTime, out long numericTime))
                {
                    // Convert to seconds if in milliseconds
                    if (numericTime > 10000000000) // Typical millisecond timestamp is 13 digits
                        numericTime /= 1000;

                    return DateTimeOffset.FromUnixTimeSeconds(numericTime).UtcDateTime;
                }

                // Try to parse as formatted date
                if (DateTime.TryParse(strTime, out DateTime parsedTime))
                {
                    // Ensure we're working with UTC time
                    if (parsedTime.Kind != DateTimeKind.Utc)
                        return DateTime.SpecifyKind(parsedTime, DateTimeKind.Utc);
                    return parsedTime;
                }
            }

            // Handle other types by converting to string first
            try
            {
                string strValue = startTime.ToString();
                if (!string.IsNullOrEmpty(strValue) &&
                    DateTime.TryParse(strValue, out DateTime parsedTime))
                {
                    if (parsedTime.Kind != DateTimeKind.Utc)
                        return DateTime.SpecifyKind(parsedTime, DateTimeKind.Utc);
                    return parsedTime;
                }
            }
            catch
            {
                // Ignore conversion errors
            }

            // If we got here, we couldn't parse the time
            _logger.LogWarning($"Failed to parse match StartTime: {startTime}, using fallback time");
            return DateTime.UtcNow.AddDays(1); // Use tomorrow as fallback
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing match StartTime: {startTime}");
            return DateTime.UtcNow.AddDays(1); // Use tomorrow as fallback in case of error
        }
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
            var enrichedMatchIds = await enrichedCollection.FindWithDiskUse(FilterDefinition<EnrichedSportMatch>.Empty)
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
            SeasonId = match.SeasonId,
            MatchId = match.MatchId,
            OriginalMatch = match,
            CreatedAt = DateTime.UtcNow,
            MatchTime = match.MatchTime
        };

        try
        {
            // Organize API calls into groups that can be executed in parallel
            _logger.LogInformation($"Enriching match {match.MatchId}: {match.Teams.Home.Name} vs {match.Teams.Away.Name}");

            // Get match info to extract the correct seasonId
            string seasonId = match.SeasonId;
            try
            {
                var matchInfoResult = await sportRadarService.GetMatchInfoAsync(match.MatchId.ToString());
                if (matchInfoResult is Ok<JsonDocument> okResult && okResult.Value != null)
                {
                    var json = okResult.Value.RootElement.GetRawText();

                    // Extract seasonId from match info response
                    try
                    {
                        var root = JsonDocument.Parse(json).RootElement;
                        if (root.TryGetProperty("doc", out var docElement) &&
                            docElement.ValueKind == JsonValueKind.Array &&
                            docElement.GetArrayLength() > 0)
                        {

                            var firstDoc = docElement[0];
                            if (firstDoc.TryGetProperty("data", out var dataElement) &&
                                dataElement.TryGetProperty("match", out var matchElement) &&
                                matchElement.TryGetProperty("_seasonid", out var seasonIdElement))
                            {

                                // Handle both string and number types for seasonId
                                string extractedSeasonId = seasonIdElement.ValueKind == JsonValueKind.String
                                    ? seasonIdElement.GetString()
                                    : seasonIdElement.GetRawText().Trim('"');

                                seasonId = !string.IsNullOrEmpty(extractedSeasonId) ? extractedSeasonId : seasonId;
                                _logger.LogInformation($"Extracted seasonId {seasonId} from match_info for match {match.MatchId}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error extracting seasonId from match_info for match {match.MatchId}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting match_info for match {match.MatchId}");
            }

            // Small delay between getting seasonId and starting other requests
            await Task.Delay(300);

            // Create all tasks at once
            var allTasks = new Dictionary<string, Task<IResult>>
            {
                // Essential match and team data
                { "TeamTableSlice", sportRadarService.GetStatsSeasonMatchTableSpliceAsync(match.MatchId) },
                { "LastXStatsTeam1", sportRadarService.GetTeamLastXAsync(match.Teams.Home.Id) },
                { "LastXStatsTeam2", sportRadarService.GetTeamLastXAsync(match.Teams.Away.Id) },
                { "TeamVersusRecent", sportRadarService.GetTeamVersusRecentAsync(match.Teams.Home.Id, match.Teams.Away.Id) },
                
                // Season and team stats with correct seasonId
                { "Team1ScoringConceding", sportRadarService.GetStatsSeasonTeamscoringConcedingAsync(seasonId, match.Teams.Home.Id) },
                { "Team2ScoringConceding", sportRadarService.GetStatsSeasonTeamscoringConcedingAsync(seasonId, match.Teams.Away.Id) },
                { "Team1LastX", sportRadarService.GetTeamLastXExtendedAsync(match.Teams.Home.Id) },
                { "Team2LastX", sportRadarService.GetTeamLastXExtendedAsync(match.Teams.Away.Id) }
            };

            // Execute all tasks in parallel and wait for completion
            await Task.WhenAll(allTasks.Values);

            // Process results
            foreach (var kvp in allTasks)
            {
                try
                {
                    var result = await kvp.Value;
                    if (result is Ok<JsonDocument> okResult && okResult.Value != null)
                    {
                        var json = okResult.Value.RootElement.GetRawText();

                        // Set the property using reflection
                        var property = typeof(EnrichedSportMatch).GetProperty(kvp.Key);
                        if (property != null)
                        {
                            // Using type-specific deserialization with RootObjectConverter to handle malformed JSON
                            var options = new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            };

                            // Add the custom converter for this specific type
                            options.Converters.Add(new RootObjectConverter<TeamTableSliceModel>(_logger, "TeamTableSlice"));
                            options.Converters.Add(new RootObjectConverter<TeamLastXStatsModel>(_logger, "TeamLastXStats"));
                            options.Converters.Add(new RootObjectConverter<TeamVersusRecentModel>(_logger, "TeamVersusRecent"));
                            options.Converters.Add(new RootObjectConverter<TeamScoringConcedingModel>(_logger, "TeamScoringConceding"));
                            options.Converters.Add(new RootObjectConverter<TeamLastXExtendedModel>(_logger, "TeamLastXExtended"));

                            // Use SafeDeserialize for null handling and error handling with proper type
                            switch (kvp.Key)
                            {
                                case "TeamTableSlice":
                                    var tableSliceValue = SafeDeserialize<TeamTableSliceModel>(json, options, kvp.Key);
                                    property.SetValue(enrichedMatch, tableSliceValue);
                                    break;
                                case "LastXStatsTeam1":
                                case "LastXStatsTeam2":
                                    var lastXValue = SafeDeserialize<TeamLastXStatsModel>(json, options, kvp.Key);
                                    property.SetValue(enrichedMatch, lastXValue);
                                    break;
                                case "TeamVersusRecent":
                                    var versusValue = SafeDeserialize<TeamVersusRecentModel>(json, options, kvp.Key);
                                    property.SetValue(enrichedMatch, versusValue);
                                    break;
                                case "Team1ScoringConceding":
                                case "Team2ScoringConceding":
                                    var scoringValue = SafeDeserialize<TeamScoringConcedingModel>(json, options, kvp.Key);
                                    property.SetValue(enrichedMatch, scoringValue);
                                    break;
                                case "Team1LastX":
                                case "Team2LastX":
                                    var lastXExtValue = SafeDeserialize<TeamLastXExtendedModel>(json, options, kvp.Key);
                                    property.SetValue(enrichedMatch, lastXExtValue);
                                    break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing {kvp.Key} for match {match.MatchId}");
                }
            }

            return enrichedMatch;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error enriching match {match.MatchId}");

            // Still return the partially enriched match
            enrichedMatch.IsValid = false;
            return enrichedMatch;
        }
    }

    private async Task ProcessTaskGroup(Dictionary<string, Task<IResult>> tasks, EnrichedSportMatch enrichedMatch)
    {
        // Process all tasks in parallel
        await Task.WhenAll(tasks.Values);

        // Process results
        foreach (var task in tasks)
        {
            try
            {
                var result = await task.Value;

                if (result is Ok<JsonDocument> okResult && okResult.Value != null)
                {
                    var json = okResult.Value.RootElement.GetRawText();

                    // Set the property using reflection
                    var property = typeof(EnrichedSportMatch).GetProperty(task.Key);

                    if (property != null)
                    {
                        // Using type-specific deserialization with RootObjectConverter to handle malformed JSON
                        var propertyType = property.PropertyType;
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };

                        // Add the custom converter for this specific type
                        options.Converters.Add(new RootObjectConverter<TeamTableSliceModel>(_logger, "TeamTableSlice"));
                        options.Converters.Add(new RootObjectConverter<TeamLastXStatsModel>(_logger, "TeamLastXStats"));
                        options.Converters.Add(new RootObjectConverter<TeamVersusRecentModel>(_logger, "TeamVersusRecent"));
                        options.Converters.Add(new RootObjectConverter<TeamScoringConcedingModel>(_logger, "TeamScoringConceding"));
                        options.Converters.Add(new RootObjectConverter<TeamLastXExtendedModel>(_logger, "TeamLastXExtended"));

                        // Use SafeDeserialize for null handling and error handling with proper type
                        if (task.Key == "TeamTableSlice")
                        {
                            var value = SafeDeserialize<TeamTableSliceModel>(json, options, task.Key);
                            property.SetValue(enrichedMatch, value);
                        }
                        else if (task.Key == "LastXStatsTeam1" || task.Key == "LastXStatsTeam2")
                        {
                            var value = SafeDeserialize<TeamLastXStatsModel>(json, options, task.Key);
                            property.SetValue(enrichedMatch, value);
                        }
                        else if (task.Key == "TeamVersusRecent")
                        {
                            var value = SafeDeserialize<TeamVersusRecentModel>(json, options, task.Key);
                            property.SetValue(enrichedMatch, value);
                        }
                        else if (task.Key == "Team1ScoringConceding" || task.Key == "Team2ScoringConceding")
                        {
                            var value = SafeDeserialize<TeamScoringConcedingModel>(json, options, task.Key);
                            property.SetValue(enrichedMatch, value);
                        }
                        else if (task.Key == "Team1LastX" || task.Key == "Team2LastX")
                        {
                            var value = SafeDeserialize<TeamLastXExtendedModel>(json, options, task.Key);
                            property.SetValue(enrichedMatch, value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing {task.Key}");
            }
        }
    }

    private static async Task AddHumanLikeDelay(int minMs, int maxMs)
    {
        var delay = Random.Next(minMs, maxMs);
        await Task.Delay(delay);
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

            // Log first 200 chars of JSON for debugging purposes
            var logSample = json.Length > 200 ? json.Substring(0, 200) + "..." : json;
            _logger.LogDebug($"Processing JSON: {logSample}");

            // First check for error messages
            if (document.RootElement.TryGetProperty("message", out var messageElement) &&
                document.RootElement.TryGetProperty("code", out var codeElement))
            {
                _logger.LogWarning($"API error response: {messageElement.GetString()} (Code: {codeElement.GetInt32()})");
                dataJson = "{}";
                return false;
            }

            // Check for queryUrl which is common in most table data responses
            if (document.RootElement.TryGetProperty("queryUrl", out _) &&
                document.RootElement.TryGetProperty("doc", out var queryDocElement) &&
                queryDocElement.ValueKind == JsonValueKind.Array &&
                queryDocElement.GetArrayLength() > 0)
            {
                var firstQueryDoc = queryDocElement[0];
                if (firstQueryDoc.TryGetProperty("data", out var queryDataProperty))
                {
                    _logger.LogInformation($"Found queryUrl structure with embedded data object.");
                    dataJson = queryDataProperty.GetRawText();
                    return true;
                }
            }

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

    // Update the SafeDeserialize method to handle problematic properties
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
            // Special handling for TeamTableSliceModel
            if (typeof(T) == typeof(TeamTableSliceModel))
            {
                _logger.LogInformation($"Using special handling for TeamTableSliceModel in {errorContext}");

                try
                {
                    // Parse document to modify problematic properties
                    using var doc = JsonDocument.Parse(json);
                    var modified = false;
                    var jsonObj = new Dictionary<string, JsonElement>();

                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        // Handle special cases for known problematic properties
                        if (prop.Name == "currentround" && prop.Value.ValueKind == JsonValueKind.Null)
                        {
                            // Skip null currentround - the model has it as nullable
                            jsonObj.Add(prop.Name, prop.Value.Clone());
                        }
                        else if (prop.Name == "parenttableids" && prop.Value.ValueKind == JsonValueKind.Object)
                        {
                            // Handle parenttableids when it's an empty object but we expect array
                            if (!prop.Value.EnumerateObject().Any())
                            {
                                _logger.LogWarning($"Converting empty parenttableids object to empty array");
                                jsonObj.Add(prop.Name, JsonDocument.Parse("[]").RootElement);
                            }
                            else
                            {
                                // If it has properties, convert them to an array of strings
                                _logger.LogWarning($"Converting parenttableids object to array with properties");
                                var arrayElements = new List<string>();
                                foreach (var item in prop.Value.EnumerateObject())
                                {
                                    arrayElements.Add(item.Name);
                                }
                                jsonObj.Add(prop.Name, JsonSerializer.SerializeToElement(arrayElements));
                            }
                            modified = true;
                        }
                        else if (prop.Name == "presentationid" && prop.Value.ValueKind == JsonValueKind.Number)
                        {
                            // Convert presentationid number to string
                            _logger.LogWarning($"Converting presentationid from number {prop.Value.GetRawText()} to string");
                            jsonObj.Add(prop.Name, JsonDocument.Parse($"\"{prop.Value.GetRawText()}\"").RootElement);
                            modified = true;
                        }
                        else if (prop.Name == "rules" && prop.Value.ValueKind == JsonValueKind.Null)
                        {
                            // Convert null rules to empty array
                            _logger.LogWarning($"Converting null rules to empty array");
                            jsonObj.Add(prop.Name, JsonDocument.Parse("[]").RootElement);
                            modified = true;
                        }
                        else if (prop.Name == "rules" && prop.Value.ValueKind == JsonValueKind.Object)
                        {
                            // Convert rules object to array with single object
                            _logger.LogWarning($"Converting rules from object to array");
                            jsonObj.Add(prop.Name, JsonDocument.Parse($"[{prop.Value.GetRawText()}]").RootElement);
                            modified = true;
                        }
                        else
                        {
                            jsonObj.Add(prop.Name, prop.Value.Clone());
                        }
                    }

                    if (modified)
                    {
                        // Serialize the modified object and use that for deserialization
                        var modifiedJson = JsonSerializer.Serialize(jsonObj);
                        _logger.LogInformation($"Using modified JSON for TeamTableSliceModel");

                        // Add our custom root object converter to handle malformed JSON
                        var tableSliceOptions = new JsonSerializerOptions(options)
                        {
                            PropertyNameCaseInsensitive = true,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                            NumberHandling = JsonNumberHandling.AllowReadingFromString,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                            IgnoreReadOnlyProperties = true,
                            ReadCommentHandling = JsonCommentHandling.Skip,
                            AllowTrailingCommas = true,
                            MaxDepth = 128
                        };
                        tableSliceOptions.Converters.Add(new RootObjectConverter<T>(_logger, errorContext));

                        var tableSliceResult = JsonSerializer.Deserialize<T>(modifiedJson, tableSliceOptions);
                        return tableSliceResult ?? new T();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Special handling for TeamTableSliceModel failed: {ex.Message}");
                    // Fall through to standard deserialization
                }
            }
            // Special handling for TeamScoringConcedingModel
            else if (typeof(T) == typeof(TeamScoringConcedingModel))
            {
                _logger.LogInformation($"Using special handling for TeamScoringConcedingModel in {errorContext}");

                try
                {
                    using var doc = JsonDocument.Parse(json);

                    // For debugging
                    var logSample = json.Length > 200 ? json.Substring(0, 200) + "..." : json;
                    _logger.LogDebug($"TeamScoringConcedingModel JSON: {logSample}");

                    // First, let's check if this is wrapped in a doc array structure
                    if (doc.RootElement.TryGetProperty("doc", out var docProperty) &&
                        docProperty.ValueKind == JsonValueKind.Array &&
                        docProperty.GetArrayLength() > 0)
                    {
                        _logger.LogInformation("Found doc array structure in TeamScoringConcedingModel");

                        // Check if the first doc has data property
                        var firstDoc = docProperty[0];
                        if (firstDoc.TryGetProperty("data", out var dataProperty))
                        {
                            _logger.LogInformation("Found data property in doc[0], using this for deserialization");
                            json = dataProperty.GetRawText();

                            // Create new options for deserialization
                            var scoringOptions = new JsonSerializerOptions(options)
                            {
                                PropertyNameCaseInsensitive = true,
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                NumberHandling = JsonNumberHandling.AllowReadingFromString,
                                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                            };

                            var scoringResult = JsonSerializer.Deserialize<T>(json, scoringOptions);
                            return scoringResult ?? new T();
                        }
                    }

                    // Check for common error patterns
                    if (doc.RootElement.TryGetProperty("team", out var teamElement) &&
                        doc.RootElement.TryGetProperty("stats", out _))
                    {
                        // Structure looks correct - proceed with standard deserialization
                    }
                    else if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.EnumerateObject().Count() == 0)
                    {
                        // Empty object
                        _logger.LogWarning($"Empty object for TeamScoringConcedingModel");
                        return new T();
                    }
                    else
                    {
                        // Structure doesn't match our model, create a minimal valid object
                        _logger.LogWarning($"Creating minimal valid object for TeamScoringConcedingModel");
                        var minimalJson = "{\"team\":{},\"stats\":{\"totalmatches\":{},\"totalwins\":{},\"scoring\":{},\"conceding\":{}}}";

                        var concedingOptions = new JsonSerializerOptions(options)
                        {
                            PropertyNameCaseInsensitive = true,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                            NumberHandling = JsonNumberHandling.AllowReadingFromString,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        };

                        var concedingResult = JsonSerializer.Deserialize<T>(minimalJson, concedingOptions);
                        return concedingResult ?? new T();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Special handling for TeamScoringConcedingModel failed: {ex.Message}");
                    // Fall through to standard deserialization
                }
            }

            // Add our custom root object converter to handle malformed JSON
            var deserializerOptions = new JsonSerializerOptions(options)
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                NumberHandling = JsonNumberHandling.AllowReadingFromString,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                IgnoreReadOnlyProperties = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                MaxDepth = 128
            };
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

    // New method to store enriched matches in batches for improved performance
    private async Task StoreEnrichedMatchesAsync(
        IMongoCollection<EnrichedSportMatch> collection,
        List<EnrichedSportMatch> matches,
        CancellationToken stoppingToken)
    {
        if (!matches.Any()) return;

        try
        {
            // Prepare bulk write operations
            var bulkOperations = matches.Select(match =>
            {
                var filter = Builders<EnrichedSportMatch>.Filter.Eq(x => x.MatchId, match.MatchId);
                return new ReplaceOneModel<EnrichedSportMatch>(filter, match) { IsUpsert = true };
            }).ToList();

            // Execute bulk write
            var result = await collection.BulkWriteAsync(
                bulkOperations,
                new BulkWriteOptions { IsOrdered = false },
                cancellationToken: stoppingToken
            );

            _logger.LogInformation(
                "Batch database operation completed. Matched: {0}, Modified: {1}, Upserted: {2}",
                result.MatchedCount,
                result.ModifiedCount,
                result.Upserts.Count
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing batch of enriched matches");
            throw;
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

    // Replace direct Markets property with a property that redirects to OriginalMatch.Markets
    [System.Text.Json.Serialization.JsonIgnore]
    public List<MarketData> Markets
    {
        get => OriginalMatch?.Markets;
        set => OriginalMatch.Markets = value;
    }

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
    [System.Text.Json.Serialization.JsonPropertyName("_doc")]
    public string Doc { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("_id")]
    public int Id { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("_rcid")]
    public int Rcid { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("_sid")]
    public int Sid { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("mediumname")]
    public string MediumName { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("suffix")]
    public string? Suffix { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("abbr")]
    public string Abbr { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("nickname")]
    public string? Nickname { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("teamtypeid")]
    public int TeamTypeId { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("iscountry")]
    public bool IsCountry { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("sex")]
    public string Sex { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("haslogo")]
    public bool HasLogo { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("founded")]
    public string? Founded { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("website")]
    public string? Website { get; set; }
}

// Team Scoring-Conceding models
public class TeamScoringConcedingModel
{
    [JsonPropertyName("team")]
    public TeamInfo? Team { get; set; }

    [JsonPropertyName("stats")]
    public TeamStats? Stats { get; set; } = new();

    [JsonPropertyName("queryUrl")]
    public string? QueryUrl { get; set; }

    [JsonPropertyName("event")]
    public string? Event { get; set; }

    [JsonPropertyName("_dob")]
    public long? Dob { get; set; }

    [JsonPropertyName("_maxage")]
    public int? Maxage { get; set; }
}

public class TeamStats
{
    [JsonPropertyName("totalmatches")]
    public MatchCount TotalMatches { get; set; }

    [JsonPropertyName("totalwins")]
    public MatchCount TotalWins { get; set; }
    [JsonPropertyName("lossTotal")]
    public int LossTotal { get; set; }

    [JsonPropertyName("lossAway")]
    public int LossAway { get; set; }

    [JsonPropertyName("drawTotal")]
    public int DrawTotal { get; set; }

    [JsonPropertyName("drawHome")]
    public int DrawHome { get; set; }

    [JsonPropertyName("drawAway")]
    public int DrawAway { get; set; }

    [JsonPropertyName("lossHome")]
    public int LossHome { get; set; }

    [JsonPropertyName("scoring")]
    public ScoringStats Scoring { get; set; }

    [JsonPropertyName("conceding")]
    public ConcedingStats Conceding { get; set; }

    [JsonPropertyName("averagegoalsbyminutes")]
    [JsonIgnore]
    public Dictionary<string, Averages> AverageGoalsByMinutes { get; set; } = new();

    // Add a JsonExtensionData attribute to capture all other properties
    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsExtensionDataProcessed { get; private set; }

    [OnDeserialized]
    internal void OnDeserialized(StreamingContext context)
    {
        if (ExtensionData != null && !IsExtensionDataProcessed)
        {
            ProcessAverageGoalsByMinutes();
            IsExtensionDataProcessed = true;
        }
    }

    private void ProcessAverageGoalsByMinutes()
    {
        if (ExtensionData == null)
            return;

        // Look for the "averagegoalsbyminutes" property in ExtensionData
        if (ExtensionData.TryGetValue("averagegoalsbyminutes", out JsonElement element) && element.ValueKind == JsonValueKind.Object)
        {
            AverageGoalsByMinutes = new Dictionary<string, Averages>();

            // Process each time range (e.g., "0-15", "16-30", etc.)
            foreach (JsonProperty timeRange in element.EnumerateObject())
            {
                if (timeRange.Value.ValueKind == JsonValueKind.Object)
                {
                    // Create a new Averages object
                    var averages = new Averages();

                    // Try to get total, home, and away values
                    if (timeRange.Value.TryGetProperty("total", out JsonElement totalElement))
                    {
                        averages.Total = ParseDouble(totalElement);
                    }

                    if (timeRange.Value.TryGetProperty("home", out JsonElement homeElement))
                    {
                        averages.Home = ParseDouble(homeElement);
                    }

                    if (timeRange.Value.TryGetProperty("away", out JsonElement awayElement))
                    {
                        averages.Away = ParseDouble(awayElement);
                    }

                    // Add to the dictionary
                    AverageGoalsByMinutes[timeRange.Name] = averages;
                }
            }

            // Remove the property from ExtensionData to avoid duplication
            ExtensionData.Remove("averagegoalsbyminutes");
        }
    }

    private double ParseDouble(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Number:
                if (element.TryGetDouble(out double number))
                    return number;
                break;
            case JsonValueKind.String:
                if (double.TryParse(element.GetString(), out double parsed))
                    return parsed;
                break;
        }
        return 0;
    }
}

public class MatchCount
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("home")]
    public int Home { get; set; }

    [JsonPropertyName("away")]
    public int Away { get; set; }
}

public class ScoringStats
{
    [JsonPropertyName("goalsscored")]
    public MatchCount GoalsScored { get; set; } = new();

    [JsonPropertyName("atleastonegoal")]
    public MatchCount AtLeastOneGoal { get; set; } = new();

    [JsonPropertyName("failedtoscore")]
    public MatchCount FailedToScore { get; set; } = new();

    [JsonPropertyName("scoringathalftime")]
    public MatchCount ScoringAtHalftime { get; set; } = new();

    [JsonPropertyName("scoringatfulltime")]
    public MatchCount ScoringAtFulltime { get; set; } = new();

    [JsonPropertyName("bothteamsscored")]
    public MatchCount BothTeamsScored { get; set; } = new();

    [JsonPropertyName("goalsscoredaverage")]
    public Averages GoalsScoredAverage { get; set; } = new();

    [JsonPropertyName("atleastonegoalaverage")]
    public Averages AtLeastOneGoalAverage { get; set; } = new();

    [JsonPropertyName("failedtoscoreaverage")]
    public Averages FailedToScoreAverage { get; set; } = new();

    [JsonPropertyName("scoringathalftimeaverage")]
    public Averages ScoringAtHalftimeAverage { get; set; } = new();

    [JsonPropertyName("scoringatfulltimeaverage")]
    public Averages ScoringAtFulltimeAverage { get; set; } = new();

    [JsonPropertyName("goalmarginatvictoryaverage")]
    public Averages GoalMarginAtVictoryAverage { get; set; } = new();

    [JsonPropertyName("halftimegoalmarginatvictoryaverage")]
    public Averages HalftimeGoalMarginAtVictoryAverage { get; set; } = new();

    [JsonPropertyName("bothteamsscoredaverage")]
    public Averages BothTeamsScoredAverage { get; set; } = new();

    [JsonPropertyName("goalsbyminutes")]
    [JsonIgnore]
    public Dictionary<string, Averages> GoalsByMinutes { get; set; } = new();

    // Add a JsonExtensionData attribute to capture all other properties
    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsExtensionDataProcessed { get; private set; }

    [OnDeserialized]
    internal void OnDeserialized(StreamingContext context)
    {
        if (ExtensionData != null && !IsExtensionDataProcessed)
        {
            ProcessGoalsByMinutes();
            IsExtensionDataProcessed = true;
        }
    }

    private void ProcessGoalsByMinutes()
    {
        if (ExtensionData == null)
            return;

        // Look for the "goalsbyminutes" property in ExtensionData
        if (ExtensionData.TryGetValue("goalsbyminutes", out JsonElement element) && element.ValueKind == JsonValueKind.Object)
        {
            GoalsByMinutes = new Dictionary<string, Averages>();

            // Process each time range
            foreach (JsonProperty timeRange in element.EnumerateObject())
            {
                if (timeRange.Value.ValueKind == JsonValueKind.Object)
                {
                    // Create a new Averages object
                    var averages = new Averages();

                    // Try to get total, home, and away values
                    if (timeRange.Value.TryGetProperty("total", out JsonElement totalElement))
                    {
                        averages.Total = ParseDouble(totalElement);
                    }

                    if (timeRange.Value.TryGetProperty("home", out JsonElement homeElement))
                    {
                        averages.Home = ParseDouble(homeElement);
                    }

                    if (timeRange.Value.TryGetProperty("away", out JsonElement awayElement))
                    {
                        averages.Away = ParseDouble(awayElement);
                    }

                    // Add to the dictionary
                    GoalsByMinutes[timeRange.Name] = averages;
                }
            }

            // Remove the property from ExtensionData to avoid duplication
            ExtensionData.Remove("goalsbyminutes");
        }
    }

    private double ParseDouble(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Number:
                if (element.TryGetDouble(out double number))
                    return number;
                break;
            case JsonValueKind.String:
                if (double.TryParse(element.GetString(), out double parsed))
                    return parsed;
                break;
        }
        return 0;
    }
}

public class ConcedingStats
{
    [JsonPropertyName("goalsconceded")]
    public MatchCount GoalsConceded { get; set; } = new();

    [JsonPropertyName("cleansheets")]
    public MatchCount CleanSheets { get; set; } = new();

    [JsonPropertyName("goalsconcededfirsthalf")]
    public MatchCount GoalsConcededFirstHalf { get; set; } = new();

    [JsonPropertyName("goalsconcededaverage")]
    public Averages GoalsConcededAverage { get; set; } = new();

    [JsonPropertyName("cleansheetsaverage")]
    public Averages CleanSheetsAverage { get; set; } = new();

    [JsonPropertyName("goalsconcededfirsthalfaverage")]
    public Averages GoalsConcededFirstHalfAverage { get; set; } = new();

    [JsonPropertyName("minutespergoalconceded")]
    public Averages MinutesPerGoalConceded { get; set; } = new();

    [JsonPropertyName("goalsbyminutes")]
    [JsonIgnore]
    public Dictionary<string, Averages> GoalsByMinutes { get; set; } = new();

    // Add a JsonExtensionData attribute to capture all other properties
    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsExtensionDataProcessed { get; private set; }

    [OnDeserialized]
    internal void OnDeserialized(StreamingContext context)
    {
        if (ExtensionData != null && !IsExtensionDataProcessed)
        {
            ProcessGoalsByMinutes();
            IsExtensionDataProcessed = true;
        }
    }

    private void ProcessGoalsByMinutes()
    {
        if (ExtensionData == null)
            return;

        // Look for the "goalsbyminutes" property in ExtensionData
        if (ExtensionData.TryGetValue("goalsbyminutes", out JsonElement element) && element.ValueKind == JsonValueKind.Object)
        {
            GoalsByMinutes = new Dictionary<string, Averages>();

            // Process each time range
            foreach (JsonProperty timeRange in element.EnumerateObject())
            {
                if (timeRange.Value.ValueKind == JsonValueKind.Object)
                {
                    // Create a new Averages object
                    var averages = new Averages();

                    // Try to get total, home, and away values
                    if (timeRange.Value.TryGetProperty("total", out JsonElement totalElement))
                    {
                        averages.Total = ParseDouble(totalElement);
                    }

                    if (timeRange.Value.TryGetProperty("home", out JsonElement homeElement))
                    {
                        averages.Home = ParseDouble(homeElement);
                    }

                    if (timeRange.Value.TryGetProperty("away", out JsonElement awayElement))
                    {
                        averages.Away = ParseDouble(awayElement);
                    }

                    // Add to the dictionary
                    GoalsByMinutes[timeRange.Name] = averages;
                }
            }

            // Remove the property from ExtensionData to avoid duplication
            ExtensionData.Remove("goalsbyminutes");
        }
    }

    private double ParseDouble(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Number:
                if (element.TryGetDouble(out double number))
                    return number;
                break;
            case JsonValueKind.String:
                if (double.TryParse(element.GetString(), out double parsed))
                    return parsed;
                break;
        }
        return 0;
    }
}

public class Averages
{
    [JsonPropertyName("total")]
    [JsonConverter(typeof(DoubleConverter))]
    public double Total { get; set; }

    [JsonPropertyName("home")]
    [JsonConverter(typeof(DoubleConverter))]
    public double Home { get; set; }

    [JsonPropertyName("away")]
    [JsonConverter(typeof(DoubleConverter))]
    public double Away { get; set; }
}

// Team LastX models
public class TeamLastXStatsModel
{
    [JsonPropertyName("team")]
    public TeamInfo Team { get; set; }

    [JsonPropertyName("matches")]
    public List<MatchStat> LastMatches { get; set; } = new();

    [JsonPropertyName("tournaments")]
    public Dictionary<string, TournamentInfo> Tournaments { get; set; } = new();

    [JsonPropertyName("uniquetournaments")]
    public Dictionary<string, UniqueTournamentInfo> UniqueTournaments { get; set; } = new();

    [JsonPropertyName("realcategories")]
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
    [JsonPropertyName("id")]
    [JsonConverter(typeof(GenericStringConverter))]
    public string Id { get; set; }

    [JsonPropertyName("time")]
    public MatchTimeInfo Time { get; set; }

    [JsonPropertyName("round")]
    public int? Round { get; set; }

    [JsonPropertyName("roundname")]
    public RoundInfo? RoundName { get; set; }

    [JsonPropertyName("week")]
    public int? Week { get; set; }

    [JsonPropertyName("result")]
    public ResultInfo Result { get; set; }

    [JsonPropertyName("periods")]
    public Dictionary<string, ScoreInfo> Periods { get; set; } = new();

    [JsonPropertyName("seasonId")]
    [JsonConverter(typeof(GenericStringConverter))]
    public string SeasonId { get; set; }

    [JsonPropertyName("_seasonid")]
    [JsonConverter(typeof(GenericStringConverter))]
    public string _SeasonId { get; set; }

    [JsonPropertyName("teams")]
    public MatchTeamsInfo Teams { get; set; }

    [JsonPropertyName("neutralground")]
    public bool NeutralGround { get; set; }

    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    [JsonPropertyName("stadiumid")]
    [JsonConverter(typeof(GenericStringConverter))]
    public string? StadiumId { get; set; }

    [JsonPropertyName("_doc")]
    public string? Doc { get; set; }

    [JsonPropertyName("_sid")]
    public int? Sid { get; set; }

    [JsonPropertyName("_rcid")]
    public int? Rcid { get; set; }

    [JsonPropertyName("_tid")]
    public int? Tid { get; set; }

    [JsonPropertyName("_utid")]
    public int? Utid { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("tobeannounced")]
    public bool? ToBeAnnounced { get; set; }

    [JsonPropertyName("postponed")]
    public bool? Postponed { get; set; }

    [JsonPropertyName("canceled")]
    public bool? Canceled { get; set; }

    [JsonPropertyName("inlivescore")]
    public bool? InLiveScore { get; set; }

    [JsonPropertyName("bestof")]
    public string? BestOf { get; set; }

    [JsonPropertyName("walkover")]
    public bool? WalkOver { get; set; }

    [JsonPropertyName("retired")]
    public bool? Retired { get; set; }

    [JsonPropertyName("disqualified")]
    public bool? Disqualified { get; set; }
}

public class RoundInfo
{
    [JsonPropertyName("_doc")]
    public string? Doc { get; set; }

    [JsonPropertyName("_id")]
    [JsonConverter(typeof(GenericStringConverter))]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    [JsonConverter(typeof(GenericStringConverter))]
    public string Name { get; set; }

    [JsonPropertyName("displaynumber")]
    public string? DisplayNumber { get; set; }

    [JsonPropertyName("shortname")]
    public string? ShortName { get; set; }

    [JsonPropertyName("cuproundnumber")]
    public string? CupRoundNumber { get; set; }

    [JsonPropertyName("statisticssortorder")]
    public int? StatisticsSortOrder { get; set; }
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
    public string? Period { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("winner")]
    public string? Winner { get; set; }
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
    [System.Text.Json.Serialization.JsonPropertyName("_doc")]
    public string Doc { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("_id")]
    public int Id { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("_sid")]
    public int Sid { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("_rcid")]
    public int Rcid { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("cc")]
    public CountryCodeInfo Cc { get; set; }
}

public class CountryCodeInfo
{
    [System.Text.Json.Serialization.JsonPropertyName("_doc")]
    public string Doc { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("_id")]
    public int Id { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("a2")]
    public string A2 { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("a3")]
    public string A3 { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("ioc")]
    public string Ioc { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("continentid")]
    public int ContinentId { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("continent")]
    public string Continent { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("population")]
    public int Population { get; set; }
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

    [System.Text.Json.Serialization.JsonPropertyName("queryUrl")]
    public string? QueryUrl { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("_dob")]
    public long? Dob { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("_maxage")]
    public int? MaxAge { get; set; }
}

public class ExtendedMatchStat : MatchStat
{
    [System.Text.Json.Serialization.JsonPropertyName("corners")]
    public CornersInfo? Corners { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("firstgoal")]
    public string? FirstGoal { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("lastgoal")]
    public string? LastGoal { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("cuproundmatchnumber")]
    public string? CupRoundMatchNumber { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("cuproundnumberofmatches")]
    public string? CupRoundNumberOfMatches { get; set; }
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
    [JsonPropertyName("livematchid")]
    public string? LiveMatchId { get; set; }

    [JsonPropertyName("matches")]
    public List<HeadToHeadMatch> Matches { get; set; } = new();

    [JsonPropertyName("tournaments")]
    public Dictionary<string, TournamentInfo> Tournaments { get; set; } = new();

    [JsonPropertyName("realcategories")]
    public Dictionary<string, RealCategoryInfo> RealCategories { get; set; } = new();

    [JsonPropertyName("teams")]
    public Dictionary<string, TeamInfo> Teams { get; set; } = new();

    [JsonPropertyName("currentmanagers")]
    public Dictionary<string, List<ManagerInfo>> CurrentManagers { get; set; } = new();

    [JsonPropertyName("jersey")]
    public Dictionary<string, JerseyInfo> Jersey { get; set; } = new();

    [JsonPropertyName("next")]
    public NextMatchInfo? Next { get; set; }

    // Empty constructor to handle empty data arrays
    public TeamVersusRecentModel() { }
}

public class HeadToHeadMatch : MatchStat
{
    [JsonPropertyName("referee")]
    public List<RefereeInfo> Referee { get; set; } = new();

    [JsonPropertyName("manager")]
    public ManagerData? Manager { get; set; }

    [JsonPropertyName("cuproundmatchnumber")]
    public string? CupRoundMatchNumber { get; set; }

    [JsonPropertyName("cuproundnumberofmatches")]
    public string? CupRoundNumberOfMatches { get; set; }

    [JsonPropertyName("matchStatus")]
    [MongoDB.Bson.Serialization.Attributes.BsonElement("MatchStatus")]
    public string? Status { get; set; }

    [JsonPropertyName("tobeannounced")]
    [MongoDB.Bson.Serialization.Attributes.BsonElement("HeadToHeadToBeAnnounced")]
    public bool ToBeAnnounced { get; set; }

    [JsonPropertyName("postponed")]
    [MongoDB.Bson.Serialization.Attributes.BsonElement("HeadToHeadPostponed")]
    public bool Postponed { get; set; }

    [JsonPropertyName("canceled")]
    [MongoDB.Bson.Serialization.Attributes.BsonElement("HeadToHeadCanceled")]
    public bool Canceled { get; set; }

    [JsonPropertyName("inlivescore")]
    [MongoDB.Bson.Serialization.Attributes.BsonElement("HeadToHeadInLiveScore")]
    public bool InLiveScore { get; set; }

    [JsonPropertyName("bestof")]
    [MongoDB.Bson.Serialization.Attributes.BsonElement("HeadToHeadBestOf")]
    public string? BestOf { get; set; }

    [JsonPropertyName("walkover")]
    [MongoDB.Bson.Serialization.Attributes.BsonElement("HeadToHeadWalkOver")]
    public bool WalkOver { get; set; }

    [JsonPropertyName("retired")]
    [MongoDB.Bson.Serialization.Attributes.BsonElement("HeadToHeadRetired")]
    public bool Retired { get; set; }

    [JsonPropertyName("disqualified")]
    [MongoDB.Bson.Serialization.Attributes.BsonElement("HeadToHeadDisqualified")]
    public bool Disqualified { get; set; }
}

public class ManagerData
{
    [JsonPropertyName("home")]
    public ManagerInfo? Home { get; set; }

    [JsonPropertyName("away")]
    public ManagerInfo? Away { get; set; }
}

public class ManagerInfo
{
    [System.Text.Json.Serialization.JsonPropertyName("_doc")]
    public string Doc { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("_id")]
    public int Id { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("fullname")]
    public string? FullName { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("birthdate")]
    public DateInfo? BirthDate { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("nationality")]
    public CountryCodeInfo Nationality { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("primarypositiontype")]
    public string? PrimaryPositionType { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("haslogo")]
    public bool HasLogo { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("membersince")]
    public DateInfo? MemberSince { get; set; }
}

public class RefereeInfo
{
    [System.Text.Json.Serialization.JsonPropertyName("_doc")]
    public string Doc { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("_id")]
    public int Id { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("fullname")]
    public string FullName { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("birthdate")]
    public DateInfo? BirthDate { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("nationality")]
    public CountryCodeInfo Nationality { get; set; }
}

public class NextMatchInfo : MatchStat
{
    [JsonPropertyName("stadium")]
    public StadiumInfo? Stadium { get; set; }

    [JsonPropertyName("matchdifficultyrating")]
    [JsonConverter(typeof(MatchDifficultyRatingConverter))]
    public Dictionary<string, int>? MatchDifficultyRating { get; set; }
}

public class MatchDifficultyRatingConverter : JsonConverter<Dictionary<string, int>?>
{
    public override Dictionary<string, int>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.StartObject)
            return null;

        var result = new Dictionary<string, int>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            var key = reader.GetString();
            reader.Read();

            if (reader.TokenType == JsonTokenType.Number)
            {
                result[key] = reader.GetInt32();
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                if (int.TryParse(reader.GetString(), out int value))
                {
                    result[key] = value;
                }
            }
        }

        return result;
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<string, int>? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();
        foreach (var kvp in value)
        {
            writer.WritePropertyName(kvp.Key);
            writer.WriteNumberValue(kvp.Value);
        }
        writer.WriteEndObject();
    }
}

public class StadiumInfo
{
    [System.Text.Json.Serialization.JsonPropertyName("_doc")]
    public string Doc { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("_id")]
    [System.Text.Json.Serialization.JsonNumberHandling(System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString)]
    public int Id { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("description")]
    public string Description { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("city")]
    public string City { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("country")]
    public string Country { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("state")]
    public string? State { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("cc")]
    public CountryCodeInfo CountryCode { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("capacity")]
    public string Capacity { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("hometeams")]
    public List<TeamInfo> HomeTeams { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("googlecoords")]
    public string GoogleCoords { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("pitchsize")]
    public JsonElement? PitchSize { get; set; }
}

public class DateInfo
{
    [System.Text.Json.Serialization.JsonPropertyName("_doc")]
    public string Doc { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("time")]
    public string Time { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("date")]
    public string Date { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("tz")]
    public string Tz { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("tzoffset")]
    public int TzOffset { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("uts")]
    public long Uts { get; set; }
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

            // Ensure we have JsonStringDictionaryConverter for handling complex nested objects with hyphens
            bool hasStringDictionaryConverter = convertersToKeep.Any(c => c.GetType().Name.Contains("JsonStringDictionaryConverter"));
            if (!hasStringDictionaryConverter && typeof(T).GetProperties().Any(p => p.PropertyType.IsGenericType &&
                p.PropertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>)))
            {
                _logger.LogInformation($"Adding JsonStringDictionaryConverter to options for {_contextName}");

                // We need to dynamically create the right converter type
                var valueType = typeof(Averages); // Default to Averages for common use case

                // Try to find actual value type from dictionary properties
                foreach (var prop in typeof(T).GetProperties()
                    .Where(p => p.PropertyType.IsGenericType &&
                           p.PropertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>)))
                {
                    var genericArgs = prop.PropertyType.GetGenericArguments();
                    if (genericArgs.Length == 2 && genericArgs[0] == typeof(string))
                    {
                        valueType = genericArgs[1];
                        break;
                    }
                }

                // Create converter dynamically
                Type converterType = typeof(JsonStringDictionaryConverter<>).MakeGenericType(valueType);
                var converter = Activator.CreateInstance(converterType) as JsonConverter;
                if (converter != null)
                {
                    newOptions.Converters.Add(converter);
                }
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
                        _logger.LogInformation($"Found doc array structure in {_contextName}, using first element");
                        return JsonSerializer.Deserialize<T>(firstElement.GetRawText(), newOptions) ?? new T();
                    }
                }
                catch (Exception ex)
                {
                    // If extracting first element fails, return empty object
                    _logger.LogWarning(ex, $"Error extracting first element from array: {ex.Message}");
                    return new T();
                }
            }

            // Try to handle nested data structure with doc[0].data pattern
            if (doc.RootElement.TryGetProperty("doc", out var docElement) &&
                docElement.ValueKind == JsonValueKind.Array &&
                docElement.GetArrayLength() > 0)
            {
                _logger.LogInformation($"Found doc array structure in {_contextName}");
                var firstDoc = docElement[0];

                // Check if it has the data property
                if (firstDoc.TryGetProperty("data", out var dataElement) &&
                    dataElement.ValueKind == JsonValueKind.Object)
                {
                    // Extract the actual data we want to deserialize
                    var dataJson = dataElement.GetRawText();
                    _logger.LogInformation($"Found data property in doc[0], using this for deserialization");

                    // Use the data element for deserialization
                    try
                    {
                        var docResult = JsonSerializer.Deserialize<T>(dataJson, newOptions);

                        // Add query URL to result if model has QueryUrl property
                        if (docResult != null)
                        {
                            var queryUrlProperty = typeof(T).GetProperty("QueryUrl");
                            if (queryUrlProperty != null &&
                                doc.RootElement.TryGetProperty("queryUrl", out var queryUrlElement))
                            {
                                queryUrlProperty.SetValue(docResult, queryUrlElement.GetString());
                            }

                            // Add event name to result if model has Event property
                            var eventProperty = typeof(T).GetProperty("Event");
                            if (eventProperty != null &&
                                firstDoc.TryGetProperty("event", out var eventElement))
                            {
                                eventProperty.SetValue(docResult, eventElement.GetString());
                            }

                            // Add maxage to result if model has Maxage property
                            var maxageProperty = typeof(T).GetProperty("Maxage");
                            if (maxageProperty != null &&
                                firstDoc.TryGetProperty("_maxage", out var maxageElement))
                            {
                                maxageProperty.SetValue(docResult, maxageElement.GetInt32());
                            }

                            // Add dob to result if model has Dob property
                            var dobProperty = typeof(T).GetProperty("Dob");
                            if (dobProperty != null &&
                                firstDoc.TryGetProperty("_dob", out var dobElement))
                            {
                                dobProperty.SetValue(docResult, dobElement.GetInt64());
                            }
                        }

                        if (docResult != null)
                        {
                            _logger.LogInformation($"Successfully deserialized data from doc array for {_contextName}");
                            return docResult;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to deserialize data element: {ex.Message}");
                        // Continue to standard deserialization
                    }
                }
            }

            // Standard deserialization
            try
            {
                // Special handling for TeamTableSliceModel and known problematic properties
                if (typeof(T) == typeof(TeamTableSliceModel) && doc.RootElement.TryGetProperty("parenttableid", out var parentTableIdProp))
                {
                    // Check if the property is numeric but model expects string
                    if (parentTableIdProp.ValueKind == JsonValueKind.Number)
                    {
                        _logger.LogInformation($"Applying special handling for numeric parenttableid in {_contextName}");

                        // Create a copy of the JSON with the numeric value converted to string
                        using var jsonDoc = JsonDocument.Parse(rawJson);
                        var jsonCopy = new Dictionary<string, JsonElement>();

                        foreach (var property in jsonDoc.RootElement.EnumerateObject())
                        {
                            if (property.Name == "parenttableid")
                            {
                                // Skip, we'll add this with the correct type
                                continue;
                            }
                            jsonCopy.Add(property.Name, property.Value.Clone());
                        }

                        // Add parenttableid as string
                        var jsonOptions = new JsonSerializerOptions { WriteIndented = false };
                        var jsonString = JsonSerializer.Serialize(jsonCopy, jsonOptions);

                        // Insert the string value of parenttableid
                        jsonString = jsonString.Insert(jsonString.Length - 1, $",\"parenttableid\":\"{parentTableIdProp.GetRawText()}\"");

                        // Try to deserialize with the fixed JSON
                        try
                        {
                            var specialResult = JsonSerializer.Deserialize<T>(jsonString, newOptions);
                            return specialResult ?? new T();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"Special handling for parenttableid failed: {ex.Message}");
                            // Continue to standard error handling
                        }
                    }
                }

                var result = JsonSerializer.Deserialize<T>(rawJson, newOptions);
                return result ?? new T();
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, $"Error in standard deserialization for {_contextName}: {ex.Message}");

                // Add more detailed logging for type mismatches
                if (ex.Message.Contains("Cannot get the value of a token type") ||
                    ex.Message.Contains("could not be converted"))
                {
                    _logger.LogWarning($"Type mismatch detected in JSON for {_contextName}. The model properties might need JsonConverter attributes.");

                    // Extract path information if available
                    var path = "unknown";
                    if (ex.Message.Contains("Path:"))
                    {
                        var pathPart = ex.Message.Split("Path:")[1].Split('|')[0].Trim();
                        path = pathPart;
                        _logger.LogWarning($"JSON path with type mismatch: {path}");
                    }

                    // Try a more lenient approach with JsonNumberHandling
                    try
                    {
                        var lenientOptions = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                            IgnoreReadOnlyProperties = true,
                            ReadCommentHandling = JsonCommentHandling.Skip,
                            AllowTrailingCommas = true
                        };

                        // Add any converters from newOptions
                        foreach (var converter in newOptions.Converters)
                        {
                            lenientOptions.Converters.Add(converter);
                        }

                        _logger.LogInformation($"Attempting fallback deserialization with lenient options for {_contextName}");
                        var lenientResult = JsonSerializer.Deserialize<T>(rawJson, lenientOptions);
                        if (lenientResult != null)
                        {
                            _logger.LogInformation($"Fallback deserialization succeeded for {_contextName}");
                            return lenientResult;
                        }
                    }
                    catch (Exception lenientEx)
                    {
                        _logger.LogWarning(lenientEx, $"Fallback deserialization also failed for {_contextName}: {lenientEx.Message}");
                    }
                }

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
    [JsonPropertyName("_doc")]
    public string? Doc { get; set; }

    [JsonPropertyName("_id")]
    [JsonConverter(typeof(GenericStringConverter))]
    public string? Id { get; set; }

    [JsonPropertyName("parenttableid")]
    [JsonConverter(typeof(GenericStringConverter))]
    public string? ParentTableId { get; set; }

    [JsonPropertyName("leaguetypeid")]
    [JsonConverter(typeof(GenericStringConverter))]
    public string? LeagueTypeId { get; set; }

    [JsonPropertyName("parenttableids")]
    [JsonConverter(typeof(ParentTableIdsConverter))]
    public List<string> ParentTableIds { get; set; } = new();

    [JsonPropertyName("seasonid")]
    [JsonConverter(typeof(GenericStringConverter))]
    public string? SeasonId { get; set; }

    [JsonPropertyName("maxrounds")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int MaxRounds { get; set; }

    [JsonPropertyName("currentround")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int? CurrentRound { get; set; }

    [JsonPropertyName("presentationid")]
    [JsonConverter(typeof(JsonElementToStringConverter))]
    public string? PresentationId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("abbr")]
    public string? Abbr { get; set; }

    [JsonPropertyName("groupname")]
    public string? GroupName { get; set; }

    [JsonPropertyName("tournament")]
    public TournamentTableInfo? Tournament { get; set; }

    [JsonPropertyName("realcategory")]
    public RealCategoryInfo? RealCategory { get; set; }

    [JsonPropertyName("rules")]
    [JsonConverter(typeof(JsonElementToObjectConverter<RulesInfo>))]
    public List<RulesInfo> Rules { get; set; } = new();

    [JsonPropertyName("totalrows")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int TotalRows { get; set; }

    [JsonPropertyName("tablerows")]
    public List<TableRowInfo> TableRows { get; set; } = new();

    [JsonPropertyName("matchid")]
    [JsonConverter(typeof(GenericStringConverter))]
    public string? MatchId { get; set; }

    // Extra fields from the doc wrapper
    [JsonPropertyName("queryUrl")]
    public string? QueryUrl { get; set; }

    [JsonPropertyName("event")]
    public string? Event { get; set; }

    [JsonPropertyName("_dob")]
    public long? Dob { get; set; }

    [JsonPropertyName("_maxage")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int? Maxage { get; set; }
}

// Custom converter for ParentTableIds
public class ParentTableIdsConverter : JsonConverter<List<string>>
{
    public override List<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return new List<string>();
        }

        // If we have an empty object {}, return an empty list
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            var doc = JsonDocument.ParseValue(ref reader);
            if (!doc.RootElement.EnumerateObject().Any())
            {
                return new List<string>();
            }

            // Handle the case where it's an object with properties
            var result = new List<string>();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                result.Add(prop.Name);
            }
            return result;
        }

        // If we have an array, try to parse it as an array of strings
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var doc = JsonDocument.ParseValue(ref reader);
            var result = new List<string>();

            foreach (var element in doc.RootElement.EnumerateArray())
            {
                if (element.ValueKind == JsonValueKind.String)
                {
                    result.Add(element.GetString() ?? "");
                }
                else
                {
                    result.Add(element.ToString());
                }
            }

            return result;
        }

        // Default case: return empty list
        return new List<string>();
    }

    public override void Write(Utf8JsonWriter writer, List<string> value, JsonSerializerOptions options)
    {
        if (value == null || !value.Any())
        {
            writer.WriteStartArray();
            writer.WriteEndArray();
            return;
        }

        writer.WriteStartArray();
        foreach (var item in value)
        {
            writer.WriteStringValue(item);
        }
        writer.WriteEndArray();
    }
}

// Then add a new converter to handle object-to-list conversion
public class JsonElementToObjectConverter<T> : System.Text.Json.Serialization.JsonConverter<List<T>> where T : class, new()
{
    public override List<T> Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
    {
        // If we encounter null or an empty array, return empty list
        if (reader.TokenType == JsonTokenType.Null ||
            (reader.TokenType == JsonTokenType.StartArray && reader.Read() && reader.TokenType == JsonTokenType.EndArray))
        {
            return new List<T>();
        }

        // If we encounter an object, convert it to a list with a single item
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var singleItem = JsonSerializer.Deserialize<T>(doc.RootElement.GetRawText(), options);

            if (singleItem != null)
            {
                return new List<T> { singleItem };
            }
        }

        // If we encounter an array, deserialize it normally
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            return JsonSerializer.Deserialize<List<T>>(doc.RootElement.GetRawText(), options) ?? new List<T>();
        }

        // Default: return empty list
        return new List<T>();
    }

    public override void Write(System.Text.Json.Utf8JsonWriter writer, List<T> value, System.Text.Json.JsonSerializerOptions options)
    {
        if (value == null || value.Count == 0)
        {
            writer.WriteStartArray();
            writer.WriteEndArray();
            return;
        }

        writer.WriteStartArray();
        foreach (var item in value)
        {
            JsonSerializer.Serialize(writer, item, options);
        }
        writer.WriteEndArray();
    }
}

public class TableRowInfo
{
    [JsonPropertyName("_doc")]
    public string Doc { get; set; }

    [JsonPropertyName("_id")]
    [JsonConverter(typeof(GenericStringConverter))]
    [BsonIgnoreIfNull]
    public string Id { get; set; }

    [JsonPropertyName("promotion")]
    [BsonIgnoreIfNull]
    public PromotionInfo Promotion { get; set; }

    [JsonPropertyName("changeTotal")]
    public int ChangeTotal { get; set; }

    [JsonPropertyName("changeHome")]
    public int ChangeHome { get; set; }

    [JsonPropertyName("changeAway")]
    public int ChangeAway { get; set; }

    [JsonPropertyName("drawTotal")]
    public int DrawTotal { get; set; }

    [JsonPropertyName("drawHome")]
    public int DrawHome { get; set; }

    [JsonPropertyName("drawAway")]
    public int DrawAway { get; set; }

    [JsonPropertyName("goalDiffTotal")]
    public int GoalDiffTotal { get; set; }

    [JsonPropertyName("goalDiffHome")]
    public int GoalDiffHome { get; set; }

    [JsonPropertyName("goalDiffAway")]
    public int GoalDiffAway { get; set; }

    [JsonPropertyName("goalsAgainstTotal")]
    public int GoalsAgainstTotal { get; set; }

    [JsonPropertyName("goalsAgainstHome")]
    public int GoalsAgainstHome { get; set; }

    [JsonPropertyName("goalsAgainstAway")]
    public int GoalsAgainstAway { get; set; }

    [JsonPropertyName("goalsForTotal")]
    public int GoalsForTotal { get; set; }

    [JsonPropertyName("goalsForHome")]
    public int GoalsForHome { get; set; }

    [JsonPropertyName("goalsForAway")]
    public int GoalsForAway { get; set; }

    [JsonPropertyName("lossTotal")]
    public int LossTotal { get; set; }

    [JsonPropertyName("lossHome")]
    public int LossHome { get; set; }

    [JsonPropertyName("lossAway")]
    public int LossAway { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("home")]
    public int Home { get; set; }

    [JsonPropertyName("away")]
    public int Away { get; set; }

    [JsonPropertyName("pointsTotal")]
    public int PointsTotal { get; set; }

    [JsonPropertyName("pointsHome")]
    public int PointsHome { get; set; }

    [JsonPropertyName("pointsAway")]
    public int PointsAway { get; set; }

    [JsonPropertyName("pos")]
    public int Pos { get; set; }

    [JsonPropertyName("posHome")]
    public int PosHome { get; set; }

    [JsonPropertyName("posAway")]
    public int PosAway { get; set; }

    [JsonPropertyName("sortPositionTotal")]
    public int SortPositionTotal { get; set; }

    [JsonPropertyName("sortPositionHome")]
    public int SortPositionHome { get; set; }

    [JsonPropertyName("sortPositionAway")]
    public int SortPositionAway { get; set; }

    [JsonPropertyName("team")]
    public TeamTableInfo Team { get; set; }

    [JsonPropertyName("winTotal")]
    public int WinTotal { get; set; }

    [JsonPropertyName("winHome")]
    public int WinHome { get; set; }

    [JsonPropertyName("winAway")]
    public int WinAway { get; set; }
}

public class TournamentTableInfo
{
    [JsonPropertyName("_doc")]
    public string Doc { get; set; }

    [JsonPropertyName("_id")]
    public int Id { get; set; }

    [JsonPropertyName("_sid")]
    public int Sid { get; set; }

    [JsonPropertyName("_rcid")]
    public int Rcid { get; set; }

    [JsonPropertyName("_isk")]
    public int Isk { get; set; }

    [JsonPropertyName("_tid")]
    public int Tid { get; set; }

    [JsonPropertyName("_utid")]
    public int Utid { get; set; }

    [JsonPropertyName("_gender")]
    public string Gender { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("abbr")]
    public string Abbr { get; set; }

    [JsonPropertyName("ground")]
    public string Ground { get; set; }

    [JsonPropertyName("friendly")]
    public bool Friendly { get; set; }

    [JsonPropertyName("seasonid")]
    public int SeasonId { get; set; }

    [JsonPropertyName("currentseason")]
    public int CurrentSeason { get; set; }

    [JsonPropertyName("year")]
    public string Year { get; set; }

    [JsonPropertyName("seasontype")]
    public string SeasonType { get; set; }

    [JsonPropertyName("seasontypename")]
    public string SeasonTypeName { get; set; }

    [JsonPropertyName("seasontypeunique")]
    public string SeasonTypeUnique { get; set; }

    [JsonPropertyName("livetable")]
    [JsonConverter(typeof(JsonElementToStringConverter))]
    [BsonIgnoreIfNull]
    public string LiveTable { get; set; }

    [JsonPropertyName("cuprosterid")]
    public int? CupRosterId { get; set; }

    [JsonPropertyName("roundbyround")]
    public bool RoundByRound { get; set; }

    [JsonPropertyName("tournamentlevelorder")]
    public int? TournamentLevelOrder { get; set; }

    [JsonPropertyName("tournamentlevelname")]
    public string TournamentLevelName { get; set; }

    [JsonPropertyName("outdated")]
    public bool Outdated { get; set; }
}

public class TeamTableInfo
{
    [JsonPropertyName("_doc")]
    public string Doc { get; set; }

    [JsonPropertyName("_id")]
    [JsonConverter(typeof(GenericStringConverter))]
    public string Id { get; set; }

    [JsonPropertyName("_sid")]
    public int Sid { get; set; }

    [JsonPropertyName("uid")]
    public int Uid { get; set; }

    [JsonPropertyName("virtual")]
    public bool Virtual { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("mediumname")]
    public string MediumName { get; set; }

    [JsonPropertyName("abbr")]
    public string Abbr { get; set; }

    [JsonPropertyName("nickname")]
    public string Nickname { get; set; }

    [JsonPropertyName("iscountry")]
    public bool IsCountry { get; set; }

    [JsonPropertyName("haslogo")]
    public bool HasLogo { get; set; }

    [JsonPropertyName("sex")]
    public string Sex { get; set; }

    [JsonPropertyName("teamtypeid")]
    public int TeamTypeId { get; set; }

    [JsonPropertyName("suffix")]
    public string Suffix { get; set; }

    [JsonPropertyName("founded")]
    public string Founded { get; set; }

    [JsonPropertyName("website")]
    public string Website { get; set; }
}

public class PromotionInfo
{
    [JsonPropertyName("_doc")]
    public string Doc { get; set; }

    [JsonPropertyName("_id")]
    [JsonConverter(typeof(GenericStringConverter))]
    [BsonIgnoreIfNull]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("shortname")]
    public string ShortName { get; set; }

    [JsonPropertyName("cssclass")]
    public string CssClass { get; set; }

    [JsonPropertyName("position")]
    public int Position { get; set; }
}

public class RulesInfo
{
    [JsonPropertyName("_doc")]
    public string Doc { get; set; }

    [JsonPropertyName("_id")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }
}

public class JsonElementToStringConverter : System.Text.Json.Serialization.JsonConverter<string>
{
    public override string Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
    {
        if (reader.TokenType == System.Text.Json.JsonTokenType.Null)
        {
            return null;
        }

        // If it's already a string, just return it
        if (reader.TokenType == System.Text.Json.JsonTokenType.String)
        {
            return reader.GetString();
        }

        // For any other token type, capture the raw JSON and return as string
        using var doc = System.Text.Json.JsonDocument.ParseValue(ref reader);
        return doc.RootElement.GetRawText();
    }

    public override void Write(System.Text.Json.Utf8JsonWriter writer, string value, System.Text.Json.JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        // Try to write it as a raw JSON string if it's valid JSON
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(value);
            doc.RootElement.WriteTo(writer);
        }
        catch
        {
            // If not valid JSON, write as a normal string
            writer.WriteStringValue(value);
        }
    }
}

// Add this class before the next existing class
public class JsonStringDictionaryConverter<TValue> : JsonConverter<Dictionary<string, TValue>> where TValue : class, new()
{
    public override Dictionary<string, TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return new Dictionary<string, TValue>();
            }

            // Log unexpected token type
            Console.WriteLine($"JsonStringDictionaryConverter expected StartObject but got {reader.TokenType} for type {typeof(TValue).Name}");
            throw new JsonException($"Expected StartObject but got {reader.TokenType}");
        }

        var result = new Dictionary<string, TValue>();
        var valueOptions = new JsonSerializerOptions(options);

        // Create a new list of converters excluding this specific type to prevent recursion
        var convertersToKeep = valueOptions.Converters
            .Where(c => !(c is JsonStringDictionaryConverter<TValue>))
            .ToList();

        valueOptions.Converters.Clear();
        foreach (var converter in convertersToKeep)
        {
            valueOptions.Converters.Add(converter);
        }

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return result;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException($"Expected PropertyName but got {reader.TokenType}");
            }

            var propertyName = reader.GetString();
            reader.Read();

            if (reader.TokenType == JsonTokenType.Null)
            {
                result[propertyName] = null;
                continue;
            }

            try
            {
                // Create a default value if deserialization fails
                var value = new TValue();

                // Deserialize the value based on its type
                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    // Skip this object and create a default instance
                    // We just need to ensure we read past this object properly
                    int depth = 1;
                    while (depth > 0 && reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.StartObject || reader.TokenType == JsonTokenType.StartArray)
                        {
                            depth++;
                        }
                        else if (reader.TokenType == JsonTokenType.EndObject || reader.TokenType == JsonTokenType.EndArray)
                        {
                            depth--;
                        }
                    }
                }
                else
                {
                    // For primitive values, try deserializing normally
                    value = JsonSerializer.Deserialize<TValue>(ref reader, valueOptions);
                }

                result[propertyName] = value;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error deserializing property '{propertyName}' of type {typeof(TValue).Name}: {ex.Message}");

                // Create a default instance as a fallback
                result[propertyName] = new TValue();

                // Skip to the next property
                int depth = 1;
                while (depth > 0 && reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.StartObject || reader.TokenType == JsonTokenType.StartArray)
                    {
                        depth++;
                    }
                    else if (reader.TokenType == JsonTokenType.EndObject || reader.TokenType == JsonTokenType.EndArray)
                    {
                        depth--;
                    }
                }
            }
        }

        throw new JsonException("Expected EndObject but reached end of data");
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<string, TValue> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (var kvp in value)
        {
            writer.WritePropertyName(kvp.Key);
            JsonSerializer.Serialize(writer, kvp.Value, options);
        }

        writer.WriteEndObject();
    }
}

public class DoubleConverter : JsonConverter<double>
{
    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                return reader.GetDouble();
            case JsonTokenType.String:
                var stringValue = reader.GetString();
                if (string.IsNullOrEmpty(stringValue)) return 0;
                if (double.TryParse(stringValue, out var result))
                    return result;
                return 0;
            case JsonTokenType.True:
                return 1;
            case JsonTokenType.False:
                return 0;
            case JsonTokenType.Null:
                return 0;
            default:
                throw new JsonException($"Cannot convert {reader.TokenType} to double");
        }
    }

    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}

public class AveragesDictionaryConverter : JsonConverter<Dictionary<string, Averages>>
{
    public override Dictionary<string, Averages> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected start of object");
        }

        var result = new Dictionary<string, Averages>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return result;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected property name");
            }

            string propertyName = reader.GetString();
            reader.Read();

            try
            {
                // For time ranges with hyphens like "0-15"
                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    var averages = new Averages();

                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.EndObject)
                        {
                            break;
                        }

                        if (reader.TokenType != JsonTokenType.PropertyName)
                        {
                            throw new JsonException("Expected property name in Averages object");
                        }

                        string fieldName = reader.GetString();
                        reader.Read();

                        // Parse the value as double, handling different token types
                        double value = 0;
                        if (reader.TokenType == JsonTokenType.Number)
                        {
                            value = reader.GetDouble();
                        }
                        else if (reader.TokenType == JsonTokenType.String)
                        {
                            if (double.TryParse(reader.GetString(), out double parsedValue))
                            {
                                value = parsedValue;
                            }
                        }

                        // Set the appropriate property
                        if (fieldName == "total")
                        {
                            averages.Total = value;
                        }
                        else if (fieldName == "home")
                        {
                            averages.Home = value;
                        }
                        else if (fieldName == "away")
                        {
                            averages.Away = value;
                        }
                    }

                    result[propertyName] = averages;
                }
                else
                {
                    // Skip non-object values
                    reader.Skip();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deserializing time range '{propertyName}': {ex.Message}");

                // Add default values and continue
                result[propertyName] = new Averages();

                // Skip to the end of this property if needed
                if (reader.TokenType == JsonTokenType.StartObject || reader.TokenType == JsonTokenType.StartArray)
                {
                    int depth = 1;
                    while (depth > 0 && reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.StartObject || reader.TokenType == JsonTokenType.StartArray)
                        {
                            depth++;
                        }
                        else if (reader.TokenType == JsonTokenType.EndObject || reader.TokenType == JsonTokenType.EndArray)
                        {
                            depth--;
                        }
                    }
                }
            }
        }

        throw new JsonException("Expected end of object");
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<string, Averages> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (var kvp in value)
        {
            writer.WritePropertyName(kvp.Key);
            writer.WriteStartObject();

            writer.WritePropertyName("total");
            writer.WriteNumberValue(kvp.Value.Total);

            writer.WritePropertyName("home");
            writer.WriteNumberValue(kvp.Value.Home);

            writer.WritePropertyName("away");
            writer.WriteNumberValue(kvp.Value.Away);

            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }
}