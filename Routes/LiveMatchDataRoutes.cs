using System.Text;
using fredapi.Utils;

namespace fredapi.Routes;

public static class LiveMatchDataRoutes
{
    public static RouteGroupBuilder MapLiveMatchDataRoutes(this RouteGroupBuilder group)
    {
        group.MapGet("/livematch/{id:int}/snapshots", GetMatchSnapshots)
            .WithName("GetMatchSnapshots")
            .WithDescription("Get all snapshots for a specific match")
            .WithOpenApi();

        group.MapGet("/livematch/{id:int}/csv", ExportMatchDataToCsv)
            .WithName("ExportMatchDataToCsv")
            .WithDescription("Export match data as a CSV timeline")
            .WithOpenApi();

        group.MapGet("/livematch/{id:int}/combined", ExportCombinedDataForMatch)
            .WithName("ExportCombinedDataForMatch")
            .WithDescription("Export combined pre-match, mid-game, and final data for a match")
            .WithOpenApi();

        group.MapGet("/livedataset", ExportFullDataset)
            .WithName("ExportFullDataset")
            .WithDescription("Export the complete dataset for all matches")
            .WithOpenApi();

        group.MapGet("/livematches", GetAllMatchesWithSnapshots)
            .WithName("GetAllMatchesWithSnapshots")
            .WithDescription("Get all matches with their snapshots within a date range")
            .WithOpenApi();

        return group;
    }

    private static async Task<IResult> GetMatchSnapshots(int id, PredictionEnrichedMatchService predictionEnrichedMatchService)
    {
        try
        {
            var snapshots = await predictionEnrichedMatchService.GetMatchSnapshotsAsync(id);
            return Results.Ok(snapshots);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                title: $"Error getting match snapshots for match {id}",
                statusCode: 500);
        }
    }

    private static async Task<IResult> ExportMatchDataToCsv(int id, PredictionEnrichedMatchService predictionEnrichedMatchService)
    {
        try
        {
            var csv = await predictionEnrichedMatchService.ExportMatchDataToCsvAsync(id);
            
            // Return as CSV file
            byte[] bytes = Encoding.UTF8.GetBytes(csv);
            return Results.File(bytes, "text/csv", $"match_{id}_timeline.csv");
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                title: $"Error exporting match data to CSV for match {id}",
                statusCode: 500);
        }
    }

    private static async Task<IResult> ExportCombinedDataForMatch(int id, PredictionEnrichedMatchService predictionEnrichedMatchService)
    {
        try
        {
            var csv = await predictionEnrichedMatchService.ExportCombinedDatasetForMatchAsync(id);
            
            // Return as CSV file
            byte[] bytes = Encoding.UTF8.GetBytes(csv);
            return Results.File(bytes, "text/csv", $"match_{id}_combined.csv");
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                title: $"Error exporting combined match data for match {id}",
                statusCode: 500);
        }
    }

    private static async Task<IResult> ExportFullDataset(PredictionEnrichedMatchService predictionEnrichedMatchService)
    {
        try
        {
            var csv = await predictionEnrichedMatchService.ExportAllMatchesDatasetAsync();
            
            // Return as CSV file
            byte[] bytes = Encoding.UTF8.GetBytes(csv);
            return Results.File(bytes, "text/csv", "matches_dataset.csv");
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                title: "Error exporting full match dataset",
                statusCode: 500);
        }
    }

    private static async Task<IResult> GetAllMatchesWithSnapshots(
        HttpContext context,
        PredictionEnrichedMatchService predictionEnrichedMatchService)
    {
        try
        {
            // Extract query parameters
            DateTime? startDate = null;
            DateTime? endDate = null;
            
            if (context.Request.Query.TryGetValue("startDate", out var startDateValue) && 
                DateTime.TryParse(startDateValue, out var parsedStartDate))
            {
                startDate = parsedStartDate;
            }
            else
            {
                startDate = DateTime.UtcNow.AddDays(-7);
            }
            
            if (context.Request.Query.TryGetValue("endDate", out var endDateValue) && 
                DateTime.TryParse(endDateValue, out var parsedEndDate))
            {
                endDate = parsedEndDate;
            }
            else
            {
                endDate = DateTime.UtcNow;
            }
            
            var snapshots = await predictionEnrichedMatchService.GetAllMatchSnapshotsAsync(startDate, endDate);
            
            // Group by match ID
            var matchGroups = snapshots.GroupBy(s => s.MatchId)
                .Select(g => new
                {
                    MatchId = g.Key,
                    Snapshots = g.OrderBy(s => s.Timestamp).ToList()
                })
                .ToList();
                
            return Results.Ok(matchGroups);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                title: "Error getting match snapshots",
                statusCode: 500);
        }
    }
}