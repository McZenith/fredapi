using System.Text.Json;
using System.Text.Json.Serialization;
using fredapi.Database;
using fredapi.SportRadarService.Background;
using MongoDB.Driver;
using MarketData = fredapi.SportRadarService.Background.ArbitrageLiveMatchBackgroundService.MarketData;
using Microsoft.Extensions.Caching.Memory;


using TeamTableSliceModel = fredapi.SportRadarService.Background.TeamTableSliceModel;
using MongoDB.Bson;
using fredapi.SportRadarService.Transformers;
using MongoDB.Bson.Serialization.Attributes;

namespace fredapi.Routes;

// Type aliases to fix missing types
using TeamLastXExtended = TeamLastXExtendedModel;
using TeamLastXStats = TeamLastXStatsModel;

// Client data models for the transformed data

public static class SportMatchRoutes
{
    private static readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private static readonly TimeSpan _defaultCacheDuration = TimeSpan.FromMinutes(5);

    public static RouteGroupBuilder MapSportMatchRoutes(this RouteGroupBuilder group)
    {
        group.MapGet("/sportmatches", (MongoDbService mongoDbService, [AsParameters] PaginationParameters pagination) =>
            GetEnrichedMatches(mongoDbService, pagination))
            .WithName("GetEnrichedMatchesNew")
            .WithDescription("Get all enriched sport matches with additional stats")
            .WithOpenApi();

        group.MapGet("/sportmatches/{matchId}", (string matchId, MongoDbService mongoDbService, [AsParameters] PaginationParameters pagination) =>
            GetEnrichedMatchById(matchId, mongoDbService, pagination))
            .WithName("GetEnrichedMatchByIdNew")
            .WithDescription("Get an enriched sport match by its ID")
            .WithOpenApi();

        // Prediction data endpoint
        group.MapGet("/prediction-data", async (MongoDbService mongoDbService, [AsParameters] PaginationParameters pagination, IServiceProvider serviceProvider) => await GetPredictionData(mongoDbService, pagination, serviceProvider))
            .WithName("GetPredictionData")
            .WithOpenApi();

        return group;
    }

    private static async Task<IResult> GetEnrichedMatches(MongoDbService mongoDbService, [AsParameters] PaginationParameters pagination)
    {
        // Validate pagination parameters
        if (pagination.Page < 1)
        {
            return Results.BadRequest(new { error = "Page number must be greater than 0" });
        }

        if (pagination.PageSize < 1 || pagination.PageSize > 100)
        {
            return Results.BadRequest(new { error = "Page size must be between 1 and 100" });
        }

        var cacheKey = $"enriched_matches_page_{pagination.Page}_size_{pagination.PageSize}";

        try
        {
            if (_cache.TryGetValue(cacheKey, out PaginatedResponse<List<EnrichedSportMatch>> cachedResponse))
            {
                return Results.Ok(cachedResponse);
            }

            var collection = mongoDbService.GetCollection<EnrichedSportMatch>("EnrichedSportMatches");

            // Get total count
            var totalItems = await collection.CountDocumentsAsync(FilterDefinition<EnrichedSportMatch>.Empty);
            var totalPages = (int)Math.Ceiling(totalItems / (double)pagination.PageSize);

            // Validate page number against total pages
            if (pagination.Page > totalPages && totalPages > 0)
            {
                return Results.BadRequest(new { error = $"Page number {pagination.Page} is greater than total pages {totalPages}" });
            }

            // Get paginated results with proper sorting
            var matches = await collection
                .FindWithDiskUse(FilterDefinition<EnrichedSportMatch>.Empty)
                .SortByDescending(m => m.MatchTime)
                .Skip((pagination.Page - 1) * pagination.PageSize)
                .Limit(pagination.PageSize)
                .ToListAsync();

            var response = new PaginatedResponse<List<EnrichedSportMatch>>
            {
                Data = matches,
                Pagination = new PaginationInfo
                {
                    CurrentPage = pagination.Page,
                    TotalPages = totalPages,
                    PageSize = pagination.PageSize,
                    TotalItems = totalItems,
                    HasNext = pagination.Page < totalPages,
                    HasPrevious = pagination.Page > 1
                }
            };

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(_defaultCacheDuration);
            _cache.Set(cacheKey, response, cacheOptions);

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                title: "Error fetching enriched sport matches",
                statusCode: 500);
        }
    }

    private static async Task<IResult> GetEnrichedMatchById(string matchId, MongoDbService mongoDbService, [AsParameters] PaginationParameters pagination)
    {
        var cacheKey = $"match_{matchId}_page_{pagination.Page}_size_{pagination.PageSize}";

        try
        {
            if (_cache.TryGetValue(cacheKey, out PaginatedResponse<EnrichedSportMatch> cachedResponse))
            {
                return Results.Ok(cachedResponse);
            }

            var collection = mongoDbService.GetCollection<EnrichedSportMatch>("EnrichedSportMatches");
            var filter = Builders<EnrichedSportMatch>.Filter.Eq(static m => m.MatchId, matchId);
            var match = await collection.FindWithDiskUse(filter).FirstOrDefaultAsync();

            if (match == null)
            {
                return Results.NotFound($"Enriched sport match with ID {matchId} not found");
            }

            var response = new PaginatedResponse<EnrichedSportMatch>
            {
                Data = match,
                Pagination = new PaginationInfo
                {
                    CurrentPage = pagination.Page,
                    TotalPages = 1,
                    PageSize = pagination.PageSize,
                    TotalItems = 1
                }
            };

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(_defaultCacheDuration);
            _cache.Set(cacheKey, response, cacheOptions);

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                title: "Error fetching enriched sport match",
                statusCode: 500);
        }
    }
    private static async Task<IResult> GetPredictionData(
        MongoDbService mongoDbService,
        [AsParameters] PaginationParameters pagination,
        IServiceProvider serviceProvider)
    {
        try
        {
            // Get SportMatchesPredictionTransformer from DI
            var transformer = serviceProvider.GetRequiredService<SportMatchesPredictionTransformer>();

            // Get matches from DB with pagination
            var mongoMatches = await mongoDbService.GetMongoEnrichedMatchesAsync(
                pagination.Page,
                pagination.PageSize,
                true);

            var totalMatchCount = await mongoDbService.GetTotalMongoMatchesCountAsync();

            // Transform matches to enriched format
            var enrichedMatches = mongoMatches
                .Select(m => m.ToEnrichedSportMatch())
                .Where(m => m != null)
                                .ToList();

            if (!enrichedMatches.Any())
            {
                return Results.NotFound("No matches found");
            }

            // Use the transformer to convert the data
            var predictionData = transformer.TransformToPredictionData(enrichedMatches);

            // Create pagination info
            var paginationInfo = new PaginationInfo
            {
                CurrentPage = pagination.Page,
                PageSize = pagination.PageSize,
                TotalItems = totalMatchCount,
                TotalPages = (int)Math.Ceiling((double)totalMatchCount / pagination.PageSize),
                HasNext = pagination.Page < (int)Math.Ceiling((double)totalMatchCount / pagination.PageSize),
                HasPrevious = pagination.Page > 1
            };

            // Set the pagination info
            predictionData.Pagination = new SportRadarService.Transformers.PaginationInfo
            {
                CurrentPage = paginationInfo.CurrentPage,
                TotalPages = paginationInfo.TotalPages,
                PageSize = paginationInfo.PageSize,
                TotalItems = (int)paginationInfo.TotalItems,
                HasNext = paginationInfo.HasNext,
                HasPrevious = paginationInfo.HasPrevious
            };

            // Clean serialization options that remove reference markers but still handle circular references
            var serializerOptions = new JsonSerializerOptions
            {
                // Use IgnoreCycles instead of ReferenceHandler.Preserve to avoid $id reference markers
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = false,
                // Add converter for handling problematic JsonElement values
                Converters = { new JsonElementInvalidHandlingConverter() }
            };

            return Results.Json(predictionData, serializerOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetPredictionData: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return Results.Problem($"Error fetching prediction data: {ex.Message}");
        }
    }
}

// Add a custom JsonConverter to handle problematic JsonElement values
public class JsonElementInvalidHandlingConverter : JsonConverter<JsonElement>
{
    public override JsonElement Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        return document.RootElement.Clone();
    }

    public override void Write(Utf8JsonWriter writer, JsonElement value, JsonSerializerOptions options)
    {
        try
        {
            // Attempt to write the JsonElement normally
            value.WriteTo(writer);
        }
        catch (InvalidOperationException)
        {
            // If we encounter an error, write a null instead
            writer.WriteNullValue();
        }
    }
}

// Add pagination models at the top of the file, after the existing models
public class PaginationParameters
{
    private const int MaxPageSize = 100;
    private int _pageSize = 10;

    [JsonPropertyName("page")]
    public int Page { get; set; } = 1;

    [JsonPropertyName("pageSize")]
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }
}

public class PaginatedResponse<T>
{
    [JsonPropertyName("data")]
    public T Data { get; set; }

    [JsonPropertyName("pagination")]
    public PaginationInfo Pagination { get; set; }
}

public class PaginationInfo
{
    [JsonPropertyName("currentPage")]
    public int CurrentPage { get; set; }

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("totalItems")]
    public long TotalItems { get; set; }

    [JsonPropertyName("hasNext")]
    public bool HasNext { get; set; }

    [JsonPropertyName("hasPrevious")]
    public bool HasPrevious { get; set; }
}

// Then let's implement the MongoDbService extensions
public static class MongoDbServiceExtensions
{
    public static async Task<List<MongoEnrichedMatch>> GetMongoEnrichedMatchesAsync(
        this MongoDbService mongoDbService,
        int page,
        int pageSize,
        bool upcomingOnly = false)
    {
        var collection = mongoDbService.GetCollection<MongoEnrichedMatch>("EnrichedSportMatches");
        var filter = upcomingOnly
            ? Builders<MongoEnrichedMatch>.Filter.Gte(m => m.MatchTime, DateTime.UtcNow.Date)
            : Builders<MongoEnrichedMatch>.Filter.Empty;

        return await collection
            .FindWithDiskUse(filter)
            .SortByDescending(m => m.MatchTime)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();
    }

    public static async Task<long> GetTotalMongoMatchesCountAsync(
        this MongoDbService mongoDbService,
        bool upcomingOnly = false)
    {
        var collection = mongoDbService.GetCollection<MongoEnrichedMatch>("EnrichedSportMatches");
        var filter = upcomingOnly
            ? Builders<MongoEnrichedMatch>.Filter.Gte(m => m.MatchTime, DateTime.UtcNow.Date)
            : Builders<MongoEnrichedMatch>.Filter.Empty;

        return await collection.CountDocumentsAsync(filter);
    }
}

// Define the MongoEnrichedMatch class to match the database schema
[BsonIgnoreExtraElements]
public class MongoEnrichedMatch
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("SeasonId")]
    public string SeasonId { get; set; }

    [BsonElement("MatchId")]
    public string MatchId { get; set; }

    [BsonElement("OriginalMatch")]
    public SportMatch OriginalMatch { get; set; }

    [BsonElement("MatchTime")]
    public DateTime MatchTime { get; set; }

    [BsonElement("Markets")]
    public List<MarketData> Markets { get; set; }

    [BsonElement("Team1LastX")]
    public TeamLastXExtended Team1LastX { get; set; }

    [BsonElement("Team2LastX")]
    public TeamLastXExtended Team2LastX { get; set; }

    [BsonElement("TeamVersusRecent")]
    public TeamVersusRecentModel TeamVersusRecent { get; set; }

    [BsonElement("TeamTableSlice")]
    public TeamTableSliceModel TeamTableSlice { get; set; }

    [BsonElement("LastXStatsTeam1")]
    public TeamLastXStats LastXStatsTeam1 { get; set; }

    [BsonElement("LastXStatsTeam2")]
    public TeamLastXStats LastXStatsTeam2 { get; set; }

    [BsonElement("CreatedAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("UpdatedAt")]
    public DateTime? UpdatedAt { get; set; }

    // Helper method to convert to EnrichedSportMatch for the transformer
    public EnrichedSportMatch ToEnrichedSportMatch()
    {
        return new EnrichedSportMatch
        {
            MatchId = this.MatchId,
            SeasonId = this.SeasonId,
            OriginalMatch = this.OriginalMatch,
            MatchTime = this.MatchTime,
            Markets = this.Markets,
            Team1LastX = this.Team1LastX,
            Team2LastX = this.Team2LastX,
            TeamVersusRecent = this.TeamVersusRecent,
            TeamTableSlice = this.TeamTableSlice,
            LastXStatsTeam1 = this.LastXStatsTeam1,
            LastXStatsTeam2 = this.LastXStatsTeam2
        };
    }
}