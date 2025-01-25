using fredapi.Database;
using fredapi.Model;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace fredapi.Routes;

public static class MatchDetailsEndpoint 
{
    public static void MapMatchDetailsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/matches/{matchId}/details", async (
                string matchId,
                [FromServices] MongoDbService mongoDbService,
                [FromServices] ILogger<Program> logger,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var collection = mongoDbService.GetCollection<EnrichedMatch>("DailyMatches");
                    var match = await collection
                        .Find(x => x.MatchId == matchId)
                        .FirstOrDefaultAsync(cancellationToken);

                    return match == null ? Results.NotFound() : Results.Ok(match);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error fetching match details for {MatchId}", matchId);
                    return Results.Problem(
                        title: "Error fetching match details",
                        detail: ex.Message,
                        statusCode: 500);
                }
            })
            .WithName("GetMatchDetails")
            .WithOpenApi();
    }
}