using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using fredapi.Database;
using fredapi.Model;

public static class MatchesEndpoints
{
    public static void MapMatchesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("matches", async (
            [FromQuery] int? limit,
            [FromQuery] int? skip,
            [FromServices] MongoDbService mongoDbService,
            [FromServices] ILogger<Program> logger,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var collection = mongoDbService.GetCollection<EnrichedMatch>("DailyMatches");
                
                // Get today's date in UTC, ensuring we match the +00:00 timezone
                var today = DateTime.UtcNow.Date;
                var tomorrow = today.AddDays(1);

                // Log the date range we're querying (for debugging)
                logger.LogInformation($"Querying matches between {today:yyyy-MM-ddTHH:mm:ss.fffzzz} and {tomorrow:yyyy-MM-ddTHH:mm:ss.fffzzz}");

                // Create filter for today's matches using UTC dates
                var filter = Builders<EnrichedMatch>.Filter.And(
                    Builders<EnrichedMatch>.Filter.Gte(x => x.MatchTime, today),
                    Builders<EnrichedMatch>.Filter.Lt(x => x.MatchTime, tomorrow)
                );

                // Create index
                var indexKeysDefinition = Builders<EnrichedMatch>.IndexKeys
                    .Ascending(x => x.MatchTime)
                    .Descending(x => x.MatchId);

                await collection.Indexes.CreateOneAsync(
                    new CreateIndexModel<EnrichedMatch>(indexKeysDefinition),
                    cancellationToken: cancellationToken);

                var findOptions = new FindOptions 
                {
                    BatchSize = 1000,
                    NoCursorTimeout = false,
                    MaxTime = TimeSpan.FromSeconds(30)
                };

                var matches = await collection
                    .Find(filter, findOptions)
                    .Skip(skip)
                    .Limit(limit)
                    .SortByDescending(x => x.MatchId)
                    .ToListAsync(cancellationToken);

                // Log sample dates for verification
                if (matches.Any())
                {
                    logger.LogInformation($"Sample match dates: {string.Join(", ", matches.Take(3).Select(m => m.MatchTime.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")))}");
                }

                var count = await collection.CountDocumentsAsync(
                    filter,
                    new CountOptions { MaxTime = TimeSpan.FromSeconds(30) },
                    cancellationToken);

                return Results.Ok(new 
                { 
                    total = count, 
                    matches,
                    queryDate = today.ToString("yyyy-MM-dd"),
                    matchCount = matches.Count
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching matches");
                return Results.Problem(
                    title: "Error fetching matches",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("GetMatchesInDatabase")
        .WithOpenApi();
    }
}