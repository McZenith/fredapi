using fredapi.Database;
using fredapi.SportRadarService.Background.UpcomingArbitrageBackgroundService;
using MongoDB.Driver;

namespace fredapi.Routes;

public static class ArbitrageRoutes
{
    public static RouteGroupBuilder MapArbitrageRoutes(this RouteGroupBuilder group)
    {
        group.MapGet("/arbitrage", GetArbitrageMatches)
            .WithName("GetArbitrageMatches")
            .WithDescription("Get all upcoming arbitrage matches")
            .WithOpenApi();

        group.MapGet("/arbitrage/enriched", GetEnrichedArbitrageMatches)
            .WithName("GetEnrichedArbitrageMatches")
            .WithDescription("Get all enriched arbitrage matches with additional stats")
            .WithOpenApi();

        group.MapGet("/arbitrage/enriched/{matchId}", GetEnrichedArbitrageMatchById)
            .WithName("GetEnrichedArbitrageMatchById")
            .WithDescription("Get an enriched arbitrage match by its ID")
            .WithOpenApi();

        return group;
    }

    private static async Task<IResult> GetArbitrageMatches(MongoDbService mongoDbService)
    {
        try
        {
            var collection = mongoDbService.GetCollection<ArbitrageMatch>("UpcomingArbitrageMatches");
            var matches = await collection.Find(FilterDefinition<ArbitrageMatch>.Empty)
                .SortByDescending(m => m.Markets.Any() ? m.Markets.Max(market => market.ProfitPercentage) : 0)
                .ToListAsync();

            return Results.Ok(matches);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                title: "Error fetching arbitrage matches",
                statusCode: 500);
        }
    }

    private static async Task<IResult> GetEnrichedArbitrageMatches(MongoDbService mongoDbService)
    {
        try
        {
            var collection = mongoDbService.GetCollection<ArbitrageMatch>("EnrichedArbitrageMatches");
            var matches = await collection.Find(FilterDefinition<ArbitrageMatch>.Empty)
                .ToListAsync();

            return Results.Ok(matches);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                title: "Error fetching enriched arbitrage matches",
                statusCode: 500);
        }
    }

    private static async Task<IResult> GetEnrichedArbitrageMatchById(string matchId, MongoDbService mongoDbService)
    {
        try
        {
            var collection = mongoDbService.GetCollection<ArbitrageMatch>("EnrichedArbitrageMatches");
            var filter = Builders<ArbitrageMatch>.Filter.Eq(m => m.MatchId, matchId);
            var match = await collection.Find(filter).FirstOrDefaultAsync();

            if (match == null)
            {
                return Results.NotFound($"Enriched arbitrage match with ID {matchId} not found");
            }

            return Results.Ok(match);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                title: "Error fetching enriched arbitrage match",
                statusCode: 500);
        }
    }
}