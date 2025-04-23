using fredapi.Utils;

namespace fredapi.Routes;

public static class PredictionResultsRoutes
{
    public static RouteGroupBuilder MapPredictionResultsRoutes(this RouteGroupBuilder group)
    {
        group.MapGet("/predictiondata", GetPredictionResults)
            .WithName("GetPredictionResults")
            .WithDescription("Get prediction results for recently completed matches")
            .WithOpenApi();
            
        return group;
    }
    
    private static async Task<IResult> GetPredictionResults(PredictionResultsService predictionResultsService)
    {
        try
        {
            var results = await predictionResultsService.GetPredictionResultsAsync();
            return Results.Ok(results);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                title: "Error getting prediction results",
                statusCode: 500);
        }
    }
}