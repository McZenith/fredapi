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
                
                // Get today's date (start and end)
                var today = DateTime.UtcNow.Date;
                var tomorrow = today.AddDays(1);

                // Create filter for today's matches
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

                var count = await collection.CountDocumentsAsync(
                    filter,
                    new CountOptions { MaxTime = TimeSpan.FromSeconds(30) },
                    cancellationToken);

                return Results.Ok(new { total = count, matches });
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