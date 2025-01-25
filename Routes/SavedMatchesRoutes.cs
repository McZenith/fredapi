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
                
                // Ensure index exists
                var indexKeysDefinition = Builders<EnrichedMatch>.IndexKeys.Descending(x => x.MatchId);
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
                    .Find(FilterDefinition<EnrichedMatch>.Empty, findOptions)
                    .Skip(skip)
                    .Limit(limit)
                    .SortByDescending(x => x.MatchId)
                    .ToListAsync(cancellationToken);

                var count = await collection.CountDocumentsAsync(
                    Builders<EnrichedMatch>.Filter.Empty,
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
        .WithName("GetMatches")
        .WithOpenApi();
    }
}