using System.Text.Json;
using fredapi.Database;
using fredapi.Model;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace fredapi.Routes;

public static class MatchOddsEndpoint
{
    public static void MapMatchOddsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/matches/{matchId}/odds", async (
                string matchId,
                [FromServices] MongoDbService mongoDbService,
                [FromServices] ILogger<Program> logger,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var collection = mongoDbService.GetCollection<EnrichedMatch>("DailyMatches");
                    var filter = Builders<EnrichedMatch>.Filter.Eq(x => x.MatchId, matchId);
                    var match = await collection
                        .FindWithDiskUse(filter)
                        .Project(x => x.BookmakerOdds)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (match == null)
                        return Results.NotFound();

                    if (string.IsNullOrEmpty(match))
                        return Results.NoContent();

                    var odds = JsonSerializer.Deserialize<JsonDocument>(match);
                    return Results.Ok(odds);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error fetching match odds for match {MatchId}", matchId);
                    return Results.Problem(
                        title: "Error fetching match odds",
                        detail: ex.Message,
                        statusCode: 500);
                }
            })
            .WithName("GetMatchOddsForIdInDatabase")
            .WithOpenApi();
    }
}