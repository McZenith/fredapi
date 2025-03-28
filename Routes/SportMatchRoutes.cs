using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using fredapi.Database;
using fredapi.SportRadarService.Background;
using fredapi.SportRadarService.Background.ArbitrageLiveMatchBackgroundService;
using fredapi.SportRadarService.Background.UpcomingArbitrageBackgroundService;
using MongoDB.Driver;
using MarketData = fredapi.SportRadarService.Background.ArbitrageLiveMatchBackgroundService.MarketData;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using fredapi.Database;
using fredapi.SportRadarService.Background;
using fredapi.SportRadarService.Background.ArbitrageLiveMatchBackgroundService;
using fredapi.SportRadarService.Background.UpcomingArbitrageBackgroundService;
using MongoDB.Driver;
using Microsoft.Extensions.Caching.Memory;
using TeamTableSliceModel = fredapi.SportRadarService.Background.TeamTableSliceModel;
using RulesInfo = fredapi.SportRadarService.Background.RulesInfo;

namespace fredapi.Routes;

// Type aliases to fix missing types
using TeamLastXExtended = fredapi.SportRadarService.Background.TeamLastXExtendedModel;
using TeamLastXStats = fredapi.SportRadarService.Background.TeamLastXStatsModel;
using ExtendedMatchStat = fredapi.SportRadarService.Background.ExtendedMatchStat;

// Client data models for the transformed data
public class PredictiveMatchData
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("date")]
    public string Date { get; set; }

    [JsonPropertyName("time")]
    public string Time { get; set; }

    [JsonPropertyName("venue")]
    public string Venue { get; set; }

    [JsonPropertyName("homeTeam")]
    public TeamData HomeTeam { get; set; }

    [JsonPropertyName("awayTeam")]
    public TeamData AwayTeam { get; set; }

    [JsonPropertyName("positionGap")]
    public int PositionGap { get; set; }

    [JsonPropertyName("favorite")]
    public string Favorite { get; set; }

    [JsonPropertyName("confidenceScore")]
    public int? ConfidenceScore { get; set; }

    [JsonPropertyName("averageGoals")]
    public double? AverageGoals { get; set; }

    [JsonPropertyName("expectedGoals")]
    public double? ExpectedGoals { get; set; }

    [JsonPropertyName("defensiveStrength")]
    public double? DefensiveStrength { get; set; }

    [JsonPropertyName("odds")]
    public OddsData Odds { get; set; }

    [JsonPropertyName("headToHead")]
    public HeadToHeadData HeadToHead { get; set; }

    [JsonPropertyName("cornerStats")]
    public CornerStatsData CornerStats { get; set; }

    [JsonPropertyName("scoringPatterns")]
    public ScoringPatternsData ScoringPatterns { get; set; }

    [JsonPropertyName("reasonsForPrediction")]
    public List<string> ReasonsForPrediction { get; set; } = new();
}

public class TeamData
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("position")]
    public int Position { get; set; }

    [JsonPropertyName("logo")]
    public string Logo { get; set; }

    [JsonPropertyName("avgHomeGoals")]
    public double? AvgHomeGoals { get; set; }

    [JsonPropertyName("avgAwayGoals")]
    public double? AvgAwayGoals { get; set; }

    [JsonPropertyName("avgTotalGoals")]
    public double? AvgTotalGoals { get; set; }

    [JsonPropertyName("homeMatchesOver15")]
    public int HomeMatchesOver15 { get; set; }

    [JsonPropertyName("awayMatchesOver15")]
    public int AwayMatchesOver15 { get; set; }

    [JsonPropertyName("totalHomeMatches")]
    public int TotalHomeMatches { get; set; }

    [JsonPropertyName("totalAwayMatches")]
    public int TotalAwayMatches { get; set; }

    [JsonPropertyName("form")]
    public string Form { get; set; }

    [JsonPropertyName("homeForm")]
    public string HomeForm { get; set; }

    [JsonPropertyName("awayForm")]
    public string AwayForm { get; set; }

    [JsonPropertyName("cleanSheets")]
    public int CleanSheets { get; set; }

    [JsonPropertyName("homeCleanSheets")]
    public int HomeCleanSheets { get; set; }

    [JsonPropertyName("awayCleanSheets")]
    public int AwayCleanSheets { get; set; }

    [JsonPropertyName("scoringFirstWinRate")]
    public int? ScoringFirstWinRate { get; set; }

    [JsonPropertyName("concedingFirstWinRate")]
    public int? ConcedingFirstWinRate { get; set; }

    [JsonPropertyName("firstHalfGoalsPercent")]
    public int? FirstHalfGoalsPercent { get; set; }

    [JsonPropertyName("secondHalfGoalsPercent")]
    public int? SecondHalfGoalsPercent { get; set; }

    [JsonPropertyName("avgCorners")]
    public double? AvgCorners { get; set; }

    [JsonPropertyName("bttsRate")]
    public int? BttsRate { get; set; }

    [JsonPropertyName("homeBttsRate")]
    public int? HomeBttsRate { get; set; }

    [JsonPropertyName("awayBttsRate")]
    public int? AwayBttsRate { get; set; }

    [JsonPropertyName("lateGoalRate")]
    public int? LateGoalRate { get; set; }

    [JsonPropertyName("goalDistribution")]
    public Dictionary<string, double> GoalDistribution { get; set; } = new();

    [JsonPropertyName("againstTopTeamsPoints")]
    public double? AgainstTopTeamsPoints { get; set; }

    [JsonPropertyName("againstMidTeamsPoints")]
    public double? AgainstMidTeamsPoints { get; set; }

    [JsonPropertyName("againstBottomTeamsPoints")]
    public double? AgainstBottomTeamsPoints { get; set; }

    [JsonPropertyName("isHomeTeam")]
    public bool IsHomeTeam { get; set; }

    [JsonPropertyName("formStrength")]
    public double FormStrength { get; set; }

    [JsonPropertyName("formRating")]
    public double FormRating { get; set; }

    [JsonPropertyName("winPercentage")]
    public double WinPercentage { get; set; }

    [JsonPropertyName("homeWinPercentage")]
    public double HomeWinPercentage { get; set; }

    [JsonPropertyName("awayWinPercentage")]
    public double AwayWinPercentage { get; set; }

    [JsonPropertyName("cleanSheetPercentage")]
    public double CleanSheetPercentage { get; set; }

    [JsonPropertyName("averageGoalsScored")]
    public double AverageGoalsScored { get; set; }

    [JsonPropertyName("averageGoalsConceded")]
    public double AverageGoalsConceded { get; set; }

    [JsonPropertyName("homeAverageGoalsScored")]
    public double HomeAverageGoalsScored { get; set; }

    [JsonPropertyName("homeAverageGoalsConceded")]
    public double HomeAverageGoalsConceded { get; set; }

    [JsonPropertyName("awayAverageGoalsScored")]
    public double AwayAverageGoalsScored { get; set; }

    [JsonPropertyName("awayAverageGoalsConceded")]
    public double AwayAverageGoalsConceded { get; set; }

    [JsonPropertyName("goalsScoredAverage")]
    public double GoalsScoredAverage { get; set; }

    [JsonPropertyName("goalsConcededAverage")]
    public double GoalsConcededAverage { get; set; }

    [JsonPropertyName("averageCorners")]
    public double AverageCorners { get; set; }

    [JsonPropertyName("avgOdds")]
    public double AvgOdds { get; set; }

    [JsonPropertyName("leagueAvgGoals")]
    public double LeagueAvgGoals { get; set; }

    [JsonPropertyName("possession")]
    public double Possession { get; set; }

    [JsonPropertyName("opponentName")]
    public string OpponentName { get; set; }

    [JsonPropertyName("totalHomeWins")]
    public int TotalHomeWins { get; set; }

    [JsonPropertyName("totalAwayWins")]
    public int TotalAwayWins { get; set; }

    [JsonPropertyName("totalHomeDraws")]
    public int TotalHomeDraws { get; set; }

    [JsonPropertyName("totalAwayDraws")]
    public int TotalAwayDraws { get; set; }

    [JsonPropertyName("totalHomeLosses")]
    public int TotalHomeLosses { get; set; }

    [JsonPropertyName("totalAwayLosses")]
    public int TotalAwayLosses { get; set; }
}

public class OddsData
{
    [JsonPropertyName("homeWin")]
    public double HomeWin { get; set; }

    [JsonPropertyName("draw")]
    public double Draw { get; set; }

    [JsonPropertyName("awayWin")]
    public double AwayWin { get; set; }

    [JsonPropertyName("over15Goals")]
    public double Over15Goals { get; set; }

    [JsonPropertyName("under15Goals")]
    public double Under15Goals { get; set; }

    [JsonPropertyName("over25Goals")]
    public double Over25Goals { get; set; }

    [JsonPropertyName("under25Goals")]
    public double Under25Goals { get; set; }

    [JsonPropertyName("bttsYes")]
    public double BttsYes { get; set; }

    [JsonPropertyName("bttsNo")]
    public double BttsNo { get; set; }
}

public class HeadToHeadData
{
    [JsonPropertyName("matches")]
    public int Matches { get; set; }

    [JsonPropertyName("wins")]
    public int Wins { get; set; }

    [JsonPropertyName("draws")]
    public int Draws { get; set; }

    [JsonPropertyName("losses")]
    public int Losses { get; set; }

    [JsonPropertyName("goalsScored")]
    public int GoalsScored { get; set; }

    [JsonPropertyName("goalsConceded")]
    public int GoalsConceded { get; set; }

    [JsonPropertyName("recentMatches")]
    public List<RecentMatchData> RecentMatches { get; set; } = new();
}

public class RecentMatchData
{
    [JsonPropertyName("date")]
    public string Date { get; set; }

    [JsonPropertyName("result")]
    public string Result { get; set; }
}

public class CornerStatsData
{
    [JsonPropertyName("homeAvg")]
    public double HomeAvg { get; set; }

    [JsonPropertyName("awayAvg")]
    public double AwayAvg { get; set; }

    [JsonPropertyName("totalAvg")]
    public double TotalAvg { get; set; }
}

public class ScoringPatternsData
{
    [JsonPropertyName("homeFirstGoalRate")]
    public int HomeFirstGoalRate { get; set; }

    [JsonPropertyName("awayFirstGoalRate")]
    public int AwayFirstGoalRate { get; set; }

    [JsonPropertyName("homeLateGoalRate")]
    public int HomeLateGoalRate { get; set; }

    [JsonPropertyName("awayLateGoalRate")]
    public int AwayLateGoalRate { get; set; }
}

public class PredictiveResponse
{
    [JsonPropertyName("upcomingMatches")]
    public List<PredictiveMatchData> UpcomingMatches { get; set; } = new();

    [JsonPropertyName("metadata")]
    public MetadataInfo Metadata { get; set; } = new();
}

public class MetadataInfo
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("date")]
    public string Date { get; set; }

    [JsonPropertyName("leagueData")]
    public Dictionary<string, LeagueData> LeagueData { get; set; } = new();
}

public class LeagueData
{
    [JsonPropertyName("matches")]
    public int Matches { get; set; }

    [JsonPropertyName("totalGoals")]
    public double TotalGoals { get; set; }

    [JsonPropertyName("homeWinRate")]
    public int HomeWinRate { get; set; }

    [JsonPropertyName("drawRate")]
    public int DrawRate { get; set; }

    [JsonPropertyName("awayWinRate")]
    public int AwayWinRate { get; set; }

    [JsonPropertyName("bttsRate")]
    public int BttsRate { get; set; }
}

public static class SportMatchRoutes
{
    private static readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private static readonly ConcurrentDictionary<string, (double? ExpectedGoals, int? ConfidenceScore)> _calculationCache =
        new();

    public static RouteGroupBuilder MapSportMatchRoutes(this RouteGroupBuilder group)
    {
        group.MapGet("/sportmatches", GetEnrichedMatches)
            .WithName("GetEnrichedMatches")
            .WithDescription("Get all enriched sport matches with additional stats")
            .WithOpenApi();

        group.MapGet("/sportmatches/{matchId}", GetEnrichedMatchById)
            .WithName("GetEnrichedMatchById")
            .WithDescription("Get an enriched sport match by its ID")
            .WithOpenApi();

        group.MapGet("/prediction-data", GetPredictionData)
            .WithName("GetPredictionData")
            .WithDescription("Get enriched sports match data transformed for prediction UI")
            .WithOpenApi();

        return group;
    }

    private static async Task<IResult> GetEnrichedMatches(MongoDbService mongoDbService)
    {
        try
        {
            var collection = mongoDbService.GetCollection<EnrichedSportMatch>("EnrichedSportMatches");

            // Create find options with allowDiskUse to handle large sorts
            var findOptions = new FindOptions
            {
                AllowDiskUse = true,
                MaxTime = TimeSpan.FromSeconds(60)
            };

            var matches = await collection.Find(
                    FilterDefinition<EnrichedSportMatch>.Empty,
                    findOptions)
                .SortByDescending(static m => m.MatchTime)
                .ToListAsync();

            return Results.Ok(matches);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                title: "Error fetching enriched sport matches",
                statusCode: 500);
        }
    }

    private static async Task<IResult> GetEnrichedMatchById(string matchId, MongoDbService mongoDbService)
    {
        try
        {
            var collection = mongoDbService.GetCollection<EnrichedSportMatch>("EnrichedSportMatches");
            var filter = Builders<EnrichedSportMatch>.Filter.Eq(static m => m.MatchId, matchId);
            var match = await collection.Find(filter).FirstOrDefaultAsync();

            if (match == null)
            {
                return Results.NotFound($"Enriched sport match with ID {matchId} not found");
            }

            return Results.Ok(match);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                title: "Error fetching enriched sport match",
                statusCode: 500);
        }
    }

    private static async Task<IResult> GetPredictionData(MongoDbService mongoDbService)
    {
        var cacheKey = $"prediction_data_{DateTime.UtcNow:yyyy-MM-dd}";
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Clear cache to force fresh data
            _cache.Remove(cacheKey);
            Console.WriteLine("Fetching fresh prediction data");

            var collection = mongoDbService.GetCollection<EnrichedSportMatch>("EnrichedSportMatches");

            // Get a count of all matches
            var totalCount = await collection.CountDocumentsAsync(FilterDefinition<EnrichedSportMatch>.Empty);
            Console.WriteLine($"Total matches in database: {totalCount}");

            // Use minimal filtering to get all matches with AllowDiskUse option
            var findOptions = new FindOptions
            {
                AllowDiskUse = true,
                MaxTime = TimeSpan.FromSeconds(60)
            };

            var matches = await collection.Find(
                    FilterDefinition<EnrichedSportMatch>.Empty,
                    findOptions)
                .SortByDescending(m => m.MatchTime)
                .ToListAsync();

            Console.WriteLine($"Found {matches.Count} total matches");

            // Filter matches with basic required data
            var validMatches = matches.Where(m =>
                m.OriginalMatch != null &&
                m.OriginalMatch.Teams != null &&
                m.OriginalMatch.Teams.Home != null &&
                m.OriginalMatch.Teams.Away != null
            ).ToList();

            Console.WriteLine($"Found {validMatches.Count} matches with basic team data");

            // Transform matches
            Console.WriteLine("Starting transformation...");
            var transformedMatches = new List<PredictiveMatchData>();

            foreach (var match in validMatches)
            {
                try
                {
                    var homeTeamName = match.OriginalMatch.Teams.Home.Name ?? "Home Team";
                    var awayTeamName = match.OriginalMatch.Teams.Away.Name ?? "Away Team";

                    var predictiveData = new PredictiveMatchData
                    {
                        Id = int.TryParse(match.MatchId, out int id) ? id : 0,
                        Date = match.MatchTime.ToString("yyyy-MM-dd"),
                        Time = match.MatchTime.ToString("HH:mm"),
                        Venue = match.OriginalMatch?.TournamentName ?? "",
                        HomeTeam = ExtractTeamData(
                            match.OriginalMatch?.Teams?.Home?.Id,
                            homeTeamName,
                            match.Team1LastX,
                            match.LastXStatsTeam1,
                            true,
                            GetOddsValue(match.Markets?.FirstOrDefault(m => m.Name == "1X2")?.Outcomes?.FirstOrDefault(o => o.Desc == "Home")?.Odds),
                            CalculateAverageGoals(match),
                            GetTeamPositionFromTable(match.TeamTableSlice, match.OriginalMatch?.Teams?.Home?.Id),
                            awayTeamName,
                            0,
                            match.OriginalMatch
                        ),
                        AwayTeam = ExtractTeamData(
                            match.OriginalMatch?.Teams?.Away?.Id,
                            awayTeamName,
                            match.Team2LastX,
                            match.LastXStatsTeam2,
                            false,
                            GetOddsValue(match.Markets?.FirstOrDefault(m => m.Name == "1X2")?.Outcomes?.FirstOrDefault(o => o.Desc == "Away")?.Odds),
                            CalculateAverageGoals(match),
                            GetTeamPositionFromTable(match.TeamTableSlice, match.OriginalMatch?.Teams?.Away?.Id),
                            homeTeamName,
                            0,
                            match.OriginalMatch
                        ),
                        PositionGap = CalculatePositionGap(match.TeamTableSlice, match.OriginalMatch?.Teams?.Home?.Id, match.OriginalMatch?.Teams?.Away?.Id),
                        Favorite = DetermineFavorite(match.Markets),
                        ConfidenceScore = CalculateConfidenceScore(match),
                        AverageGoals = CalculateAverageGoals(match),
                        ExpectedGoals = CalculateExpectedGoals(match),
                        DefensiveStrength = CalculateDefensiveStrength(match),
                        Odds = ExtractOdds(match.Markets),
                        HeadToHead = ExtractHeadToHead(match.TeamVersusRecent, match.OriginalMatch?.Teams?.Home, match.OriginalMatch?.Teams?.Away),
                        CornerStats = ExtractCornerStats(match.Team1LastX, match.Team2LastX),
                        ScoringPatterns = ExtractScoringPatterns(match.Team1LastX, match.Team2LastX),
                        ReasonsForPrediction = GeneratePredictionReasons(match)
                    };
                    transformedMatches.Add(predictiveData);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error transforming match {match.MatchId}: {ex.Message}");
                }
            }

            Console.WriteLine($"Successfully transformed {transformedMatches.Count} matches");

            // Create metadata
            var leagueMetadata = validMatches
                .GroupBy(m => m.OriginalMatch?.TournamentName ?? "Unknown")
                .ToDictionary(
                    g => g.Key,
                    g => new LeagueData
                    {
                        Matches = g.Count(),
                        TotalGoals = CalculateAverageGoalsForLeague(g.ToList()),
                        HomeWinRate = CalculateHomeWinRateForLeague(g.ToList()),
                        DrawRate = CalculateDrawRateForLeague(g.ToList()),
                        AwayWinRate = CalculateAwayWinRateForLeague(g.ToList()),
                        BttsRate = CalculateBttsRateForLeague(g.ToList())
                    }
                );

            var response = new PredictiveResponse
            {
                UpcomingMatches = transformedMatches,
                Metadata = new MetadataInfo
                {
                    Total = transformedMatches.Count,
                    Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                    LeagueData = leagueMetadata
                }
            };

            // Cache the response
            _cache.Set(cacheKey, response, TimeSpan.FromHours(1));

            sw.Stop();
            Console.WriteLine($"Prediction data processed in {sw.ElapsedMilliseconds}ms total");

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            sw.Stop();
            Console.WriteLine($"Error fetching prediction data: {ex.Message}");
            return Results.Problem(
                detail: ex.Message,
                title: "Error fetching prediction data",
                statusCode: 500);
        }
    }

    private static double CalculateAverageGoalsForLeague(List<EnrichedSportMatch> matches)
    {
        if (matches == null || !matches.Any())
            return 0;

        // Use LINQ for better performance and cleaner code
        var matchesWithStats = matches
            .Where(static m => m.TeamVersusRecent?.Matches != null)
            .SelectMany(static m => m.TeamVersusRecent.Matches
                .Where(static pm => pm.Result?.Home != null && pm.Result.Away != null)
                .Select(static pm => (pm.Result.Home ?? 0) + (pm.Result.Away ?? 0)))
            .ToList();

        return matchesWithStats.Any() ? matchesWithStats.Average() : 0;
    }

    private static int CalculateHomeWinRateForLeague(List<EnrichedSportMatch> matches)
    {
        if (matches == null || !matches.Any())
            return 0;

        var matchResults = matches
            .Where(static m => m.TeamVersusRecent?.Matches != null)
            .SelectMany(static m => m.TeamVersusRecent.Matches
                .Where(static pm => pm.Result != null)
                .Select(static pm => pm.Result.Winner == "home" ? 1 : 0))
            .ToList();

        return matchResults.Any() ? (int)(matchResults.Sum() * 100.0 / matchResults.Count) : 0;
    }

    private static int CalculateDrawRateForLeague(List<EnrichedSportMatch> matches)
    {
        if (matches == null || !matches.Any())
            return 0;

        var matchResults = matches
            .Where(static m => m.TeamVersusRecent?.Matches != null)
            .SelectMany(static m => m.TeamVersusRecent.Matches
                .Where(static pm => pm.Result != null)
                .Select(static pm => pm.Result.Winner == null ? 1 : 0))
            .ToList();

        return matchResults.Any() ? (int)(matchResults.Sum() * 100.0 / matchResults.Count) : 0;
    }

    private static int CalculateAwayWinRateForLeague(List<EnrichedSportMatch> matches)
    {
        if (matches == null || !matches.Any())
            return 0;

        var matchResults = matches
            .Where(static m => m.TeamVersusRecent?.Matches != null)
            .SelectMany(static m => m.TeamVersusRecent.Matches
                .Where(static pm => pm.Result != null)
                .Select(static pm => pm.Result.Winner == "away" ? 1 : 0))
            .ToList();

        return matchResults.Any() ? (int)(matchResults.Sum() * 100.0 / matchResults.Count) : 0;
    }

    private static int CalculateBttsRateForLeague(List<EnrichedSportMatch> matches)
    {
        if (matches == null || !matches.Any())
            return 0;

        var bttsMatches = matches
            .Where(static m => m.TeamVersusRecent?.Matches != null)
            .SelectMany(static m => m.TeamVersusRecent.Matches
                .Where(static pm => pm.Result?.Home != null && pm.Result?.Away != null)
                .Select(static pm => pm.Result.Home > 0 && pm.Result.Away > 0 ? 1 : 0))
            .ToList();

        return bttsMatches.Any() ? (int)(bttsMatches.Sum() * 100.0 / bttsMatches.Count) : 0;
    }

    private static PredictiveMatchData TransformToPredictiveData(EnrichedSportMatch match)
    {
        return new()
        {
            Id = int.TryParse(match.MatchId, out int id) ? id : 0,
            Date = match.MatchTime.ToString("yyyy-MM-dd"),
            Time = match.MatchTime.ToString("HH:mm"),
            Venue = match.OriginalMatch?.TournamentName ?? "",
            HomeTeam = ExtractTeamData(
                match.OriginalMatch?.Teams?.Home?.Id,
                match.OriginalMatch?.Teams?.Home?.Name ?? "Home Team",
                match.Team1LastX,
                match.LastXStatsTeam1,
                true,
                GetOddsValue(match.Markets?.FirstOrDefault(static m => m.Name == "1X2")?.Outcomes?.FirstOrDefault(static o => o.Desc == "Home")?.Odds),
                CalculateAverageGoals(match),
                GetTeamPositionFromTable(match.TeamTableSlice, match.OriginalMatch?.Teams?.Home?.Id),
                match.OriginalMatch?.Teams?.Away?.Name ?? "Away Team",
                0, // Using default value instead of match.LastXStatsTeam1?.Possession?.Total ?? 0
                match.OriginalMatch
            ),
            AwayTeam = ExtractTeamData(
                match.OriginalMatch?.Teams?.Away?.Id,
                match.OriginalMatch?.Teams?.Away?.Name ?? "Away Team",
                match.Team2LastX,
                match.LastXStatsTeam2,
                false,
                GetOddsValue(match.Markets?.FirstOrDefault(static m => m.Name == "1X2")?.Outcomes?.FirstOrDefault(static o => o.Desc == "Away")?.Odds),
                CalculateAverageGoals(match),
                GetTeamPositionFromTable(match.TeamTableSlice, match.OriginalMatch?.Teams?.Away?.Id),
                match.OriginalMatch?.Teams?.Home?.Name ?? "Home Team",
                0, // Default possession value
                match.OriginalMatch
            ),
            PositionGap = CalculatePositionGap(match.TeamTableSlice, match.OriginalMatch?.Teams?.Home?.Id, match.OriginalMatch?.Teams?.Away?.Id),
            Favorite = DetermineFavorite(match.Markets),
            ConfidenceScore = CalculateConfidenceScore(match),
            AverageGoals = CalculateAverageGoals(match),
            ExpectedGoals = CalculateExpectedGoals(match),
            DefensiveStrength = CalculateDefensiveStrength(match),
            Odds = ExtractOdds(match.Markets),
            HeadToHead = ExtractHeadToHead(match.TeamVersusRecent, match.OriginalMatch?.Teams?.Home, match.OriginalMatch?.Teams?.Away),
            CornerStats = ExtractCornerStats(match.Team1LastX, match.Team2LastX),
            ScoringPatterns = ExtractScoringPatterns(match.Team1LastX, match.Team2LastX),
            ReasonsForPrediction = GeneratePredictionReasons(match)
        };
    }

    private static int CalculateConfidenceScore(EnrichedSportMatch match)
    {
        // If we're missing critical data, return 0
        if (match == null)
            return 0;

        int totalWeight = 0;
        int totalScore = 0;

        try
        {
            Console.WriteLine($"Calculating confidence score for match {match.MatchId}");

            // Factor 1: Odds - weight: 40%
            if (match.Markets != null && match.Markets.Any())
            {
                var homeOdds = GetOddsValue(match.Markets?.FirstOrDefault(m => m.Name == "1X2")?.Outcomes
                    ?.FirstOrDefault(o => o.Desc == "Home")?.Odds);
                var awayOdds = GetOddsValue(match.Markets?.FirstOrDefault(m => m.Name == "1X2")?.Outcomes
                    ?.FirstOrDefault(o => o.Desc == "Away")?.Odds);
                var drawOdds = GetOddsValue(match.Markets?.FirstOrDefault(m => m.Name == "1X2")?.Outcomes
                    ?.FirstOrDefault(o => o.Desc == "Draw")?.Odds);

                if (homeOdds > 0 && awayOdds > 0)
                {
                    // Determine confidence based on odds disparity
                    var bestOdds = Math.Min(homeOdds, Math.Min(awayOdds, drawOdds));
                    var worstOdds = Math.Max(homeOdds, Math.Max(awayOdds, drawOdds));

                    // Large disparity indicates higher confidence
                    var oddsRatio = bestOdds / worstOdds;
                    var oddsConfidence = (int)(100 * (1 - oddsRatio)); // Higher difference = higher confidence
                    totalScore += oddsConfidence * 40;
                    totalWeight += 40;
                    Console.WriteLine($"Odds confidence: {oddsConfidence}% (ratio: {oddsRatio}, H: {homeOdds}, D: {drawOdds}, A: {awayOdds})");
                }
                else
                {
                    Console.WriteLine("Incomplete odds data, skipping odds factor");
                }
            }
            else
            {
                Console.WriteLine("No market data available for confidence calculation");
            }

            // Factor 2: Head-to-head analysis - weight: 20%
            if (match.TeamVersusRecent?.Matches != null && match.TeamVersusRecent.Matches.Any() &&
                match.OriginalMatch?.Teams?.Home != null && match.OriginalMatch?.Teams?.Away != null)
            {
                var h2h = ExtractHeadToHead(match.TeamVersusRecent, match.OriginalMatch.Teams.Home, match.OriginalMatch.Teams.Away);
                if (h2h.Matches > 0)
                {
                    // Calculate dominance ratio - how one-sided the matchup has been
                    var winRatio = (double)Math.Max(h2h.Wins, h2h.Losses) / h2h.Matches;
                    var h2hConfidence = (int)(winRatio * 100); // Scale 0-100

                    // Adjust based on sample size - more matches = more confidence
                    var matchCountAdjustment = Math.Min(1.0, h2h.Matches / 5.0); // Cap at 5 matches
                    h2hConfidence = (int)(h2hConfidence * matchCountAdjustment);

                    totalScore += h2hConfidence * 20;
                    totalWeight += 20;
                    Console.WriteLine($"H2H confidence: {h2hConfidence}% (matches: {h2h.Matches}, win ratio: {winRatio})");
                }
                else
                {
                    Console.WriteLine("No H2H matches available");
                }
            }
            else
            {
                Console.WriteLine("No H2H data available for confidence calculation");
            }

            // Factor 3: Recent form - weight: 30%
            if (match.Team1LastX?.Matches != null && match.Team1LastX.Matches.Any() &&
                match.Team2LastX?.Matches != null && match.Team2LastX.Matches.Any())
            {
                // Focus on most recent matches
                var team1RecentMatches = match.Team1LastX.Matches.Take(5).ToList();
                var team2RecentMatches = match.Team2LastX.Matches.Take(5).ToList();

                if (team1RecentMatches.Any() && team2RecentMatches.Any())
                {
                    // Count wins for each team in their recent matches
                    var team1Wins = team1RecentMatches
                        .Count(m => m.Result?.Winner == (m.Teams?.Home?.Id == match.Team1LastX.Team?.Id.ToString() ? "home" : "away"));
                    var team2Wins = team2RecentMatches
                        .Count(m => m.Result?.Winner == (m.Teams?.Home?.Id == match.Team2LastX.Team?.Id.ToString() ? "home" : "away"));

                    // Calculate win percentages
                    var team1WinRate = team1Wins * 100 / team1RecentMatches.Count;
                    var team2WinRate = team2Wins * 100 / team2RecentMatches.Count;

                    // Form disparity indicates higher confidence
                    var formDifference = Math.Abs(team1WinRate - team2WinRate);
                    var formConfidence = (int)Math.Min(formDifference, 100); // Cap at 100

                    totalScore += formConfidence * 30;
                    totalWeight += 30;
                    Console.WriteLine($"Form confidence: {formConfidence}% (team1: {team1WinRate}%, team2: {team2WinRate}%)");
                }
                else
                {
                    Console.WriteLine("Insufficient recent matches for form analysis");
                }
            }
            else
            {
                Console.WriteLine("No team form data available for confidence calculation");
            }

            // Factor 4: Position gap - weight: 10%
            if (match.TeamTableSlice?.TableRows != null &&
                match.OriginalMatch?.Teams?.Home?.Id != null &&
                match.OriginalMatch?.Teams?.Away?.Id != null)
            {
                var positionGap = CalculatePositionGap(match.TeamTableSlice, match.OriginalMatch.Teams.Home.Id, match.OriginalMatch.Teams.Away.Id);

                // Larger position gaps indicate higher confidence
                var positionConfidence = Math.Min(positionGap * 5, 100); // Scale to 0-100, cap at 100

                totalScore += positionConfidence * 10;
                totalWeight += 10;
                Console.WriteLine($"Position gap confidence: {positionConfidence}% (gap: {positionGap})");
            }
            else
            {
                Console.WriteLine("No position data available for confidence calculation");
            }

            // Factor 5: Home advantage - weight: 10% 
            // Only include if we don't have enough other factors
            if (totalWeight < 70 && match.OriginalMatch?.Teams?.Home != null)
            {
                // Historical home advantage is worth ~10% confidence
                int homeAdvantageConfidence = 65; // Base home advantage confidence

                // Adjust based on home team's home form if available
                if (match.Team1LastX?.Matches != null && match.Team1LastX.Matches.Any())
                {
                    var homeTeamHomeMatches = match.Team1LastX.Matches
                        .Where(m =>
                        {
                            if (m?.Teams?.Home?.Id == null) return false;
                            return m.Teams.Home.Id == match.Team1LastX.Team?.Id.ToString();
                        })
                        .Take(5)
                        .ToList();

                    if (homeTeamHomeMatches.Any())
                    {
                        var homeWins = homeTeamHomeMatches.Count(m => m.Result?.Winner == "home");
                        var homeWinRate = homeWins * 100 / homeTeamHomeMatches.Count;

                        // Adjust confidence based on home win rate
                        homeAdvantageConfidence = (homeAdvantageConfidence + homeWinRate) / 2;
                    }
                }

                totalScore += homeAdvantageConfidence * 10;
                totalWeight += 10;
                Console.WriteLine($"Home advantage confidence: {homeAdvantageConfidence}%");
            }

            // Calculate final confidence score
            if (totalWeight == 0)
            {
                Console.WriteLine("No confidence factors available, returning 0");
                return 0;
            }

            var totalConfidence = totalScore / totalWeight;
            Console.WriteLine($"Final confidence score: {totalConfidence}% (total: {totalScore}, weight: {totalWeight})");
            return Math.Max(0, Math.Min(100, totalConfidence)); // Ensure value is between 0-100
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error calculating confidence score: {ex.Message}");
            return 0;
        }
    }

    private static double CalculateDefensiveStrength(EnrichedSportMatch match)
    {
        if (match.Team1LastX?.Matches == null || !match.Team1LastX.Matches.Any() ||
            match.Team2LastX?.Matches == null || !match.Team2LastX.Matches.Any())
        {
            return 0;
        }

        // Calculate defensive strength as a ratio representing how hard it is to score against these teams
        // Lower value = stronger defense (fewer goals conceded)

        // Count clean sheets for both teams
        var team1CleanSheets = match.Team1LastX.Matches
            .Take(10)
            .Count(m =>
                (m.Teams?.Home?.Id == match.Team1LastX.Team?.Id.ToString() && (m.Result?.Away ?? 0) == 0) ||
                (m.Teams?.Away?.Id == match.Team1LastX.Team?.Id.ToString() && (m.Result?.Home ?? 0) == 0));

        var team2CleanSheets = match.Team2LastX.Matches
            .Take(10)
            .Count(m =>
                (m.Teams?.Home?.Id == match.Team2LastX.Team?.Id.ToString() && (m.Result?.Away ?? 0) == 0) ||
                (m.Teams?.Away?.Id == match.Team2LastX.Team?.Id.ToString() && (m.Result?.Home ?? 0) == 0));

        // Calculate average goals conceded per match
        var team1Matches = match.Team1LastX.Matches.Take(10).ToList();
        var team2Matches = match.Team2LastX.Matches.Take(10).ToList();

        var team1GoalsConceded = team1Matches.Sum(m =>
            m.Teams?.Home?.Id == match.Team1LastX.Team?.Id.ToString() ? (m.Result?.Away ?? 0) : (m.Result?.Home ?? 0));

        var team2GoalsConceded = team2Matches.Sum(m =>
            m.Teams?.Home?.Id == match.Team2LastX.Team?.Id.ToString() ? (m.Result?.Away ?? 0) : (m.Result?.Home ?? 0));

        var team1AvgConceded = team1Matches.Count > 0 ? (double)team1GoalsConceded / team1Matches.Count : 0;
        var team2AvgConceded = team2Matches.Count > 0 ? (double)team2GoalsConceded / team2Matches.Count : 0;

        // Combine clean sheets and goals conceded for an overall defensive strength index
        // 1.0 is average, lower is better defense
        var team1DefensiveStrength = team1AvgConceded / (1 + (team1CleanSheets * 0.2));
        var team2DefensiveStrength = team2AvgConceded / (1 + (team2CleanSheets * 0.2));

        // Average of both teams' defensive strength
        var matchDefensiveStrength = (team1DefensiveStrength + team2DefensiveStrength) / 2;

        // Normalize to a scale where 1.0 is average
        return Math.Round(matchDefensiveStrength, 2);
    }

    private static OddsData ExtractOdds(List<MarketData> markets)
    {
        return new()
        {
            HomeWin = GetOddsValue(markets?.FirstOrDefault(static m => m.Name == "1X2")?.Outcomes
                ?.FirstOrDefault(static o => o.Desc == "Home")?.Odds),

            Draw = GetOddsValue(markets?.FirstOrDefault(static m => m.Name == "1X2")?.Outcomes
                ?.FirstOrDefault(static o => o.Desc == "Draw")?.Odds),

            AwayWin = GetOddsValue(markets?.FirstOrDefault(static m => m.Name == "1X2")?.Outcomes
                ?.FirstOrDefault(static o => o.Desc == "Away")?.Odds),

            Over15Goals = GetOddsValue(markets?.FirstOrDefault(static m => m.Name == "Over/Under" && m.Specifier == "total=1.5")?.Outcomes
                ?.FirstOrDefault(static o => o.Desc == "Over 1.5")?.Odds),

            Under15Goals = GetOddsValue(markets?.FirstOrDefault(static m => m.Name == "Over/Under" && m.Specifier == "total=1.5")?.Outcomes
                ?.FirstOrDefault(static o => o.Desc == "Under 1.5")?.Odds),

            Over25Goals = GetOddsValue(markets?.FirstOrDefault(static m => m.Name == "Over/Under" && m.Specifier == "total=2.5")?.Outcomes
                ?.FirstOrDefault(static o => o.Desc == "Over 2.5")?.Odds),

            Under25Goals = GetOddsValue(markets?.FirstOrDefault(static m => m.Name == "Over/Under" && m.Specifier == "total=2.5")?.Outcomes
                ?.FirstOrDefault(static o => o.Desc == "Under 2.5")?.Odds),

            BttsYes = GetOddsValue(markets?.FirstOrDefault(static m => m.Name == "GG/NG")?.Outcomes
                ?.FirstOrDefault(static o => o.Desc == "Yes")?.Odds),

            BttsNo = GetOddsValue(markets?.FirstOrDefault(static m => m.Name == "GG/NG")?.Outcomes
                ?.FirstOrDefault(static o => o.Desc == "No")?.Odds)
        };
    }

    private static double GetOddsValue(string oddsString)
    {
        if (string.IsNullOrEmpty(oddsString) || !double.TryParse(oddsString, out double odds))
            return 2.0; // Default to even odds when missing or invalid
        return odds;
    }

    private static HeadToHeadData ExtractHeadToHead(
        TeamVersusRecentModel teamVersus,
        SportTeam homeTeam,
        SportTeam awayTeam)
    {
        var h2h = new HeadToHeadData
        {
            Matches = 0,
            Wins = 0,
            Draws = 0,
            Losses = 0,
            GoalsScored = 0,
            GoalsConceded = 0,
            RecentMatches = new List<RecentMatchData>()
        };

        // Early return if any required data is missing
        if (teamVersus?.Matches == null || homeTeam == null || awayTeam == null)
            return h2h;

        h2h.Matches = teamVersus.Matches.Count;

        var homeWins = 0;
        var draws = 0;
        var homeLosses = 0;
        var homeGoalsScored = 0;
        var homeGoalsConceded = 0;

        // Get team abbreviations for formatting
        string homeAbbr = GetTeamAbbreviation(homeTeam.Name);
        string awayAbbr = GetTeamAbbreviation(awayTeam.Name);

        try
        {
            foreach (var match in teamVersus.Matches)
            {
                // Skip invalid matches
                if (match?.Teams == null || match.Result == null)
                    continue;

                bool isHomeTeamPlayingHome = match.Teams?.Home?.Id == homeTeam.Id;
                var homeTeamScore = isHomeTeamPlayingHome ? match.Result?.Home : match.Result?.Away;
                var awayTeamScore = isHomeTeamPlayingHome ? match.Result?.Away : match.Result?.Home;

                if (homeTeamScore == null || awayTeamScore == null)
                    continue;

                if (match.Result?.Winner == (isHomeTeamPlayingHome ? "home" : "away"))
                    homeWins++;
                else if (match.Result?.Winner == null)
                    draws++;
                else
                    homeLosses++;

                homeGoalsScored += homeTeamScore ?? 0;
                homeGoalsConceded += awayTeamScore ?? 0;

                // Add recent match results with team abbreviations
                if (h2h.RecentMatches.Count < 3 && match.Time != null)
                {
                    string dateStr = match.Time.Date;
                    if (DateTime.TryParse(dateStr, out DateTime dt))
                    dateStr = dt.ToString("yyyy-MM-dd");
                    else
                        dateStr = "Unknown Date";

                    string resultStr = isHomeTeamPlayingHome
                    ? $"{homeAbbr} {homeTeamScore}-{awayTeamScore} {awayAbbr}"
                    : $"{awayAbbr} {awayTeamScore}-{homeTeamScore} {homeAbbr}";

                h2h.RecentMatches.Add(new RecentMatchData
                {
                    Date = dateStr,
                    Result = resultStr
                });
            }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing head-to-head data: {ex.Message}");
        }

        h2h.Wins = homeWins;
        h2h.Draws = draws;
        h2h.Losses = homeLosses;
        h2h.GoalsScored = homeGoalsScored;
        h2h.GoalsConceded = homeGoalsConceded;

        return h2h;
    }

    private static string GetTeamAbbreviation(string teamName)
    {
        if (string.IsNullOrEmpty(teamName))
            return "UNK";

        // Extract initials from multi-word names
        if (teamName.Contains(" "))
        {
            var words = teamName.Split(' ');
            if (words.Length >= 3)
                return new string(words.Where(static w => !string.IsNullOrEmpty(w)).Select(static w => w[0]).Take(3).ToArray());
            return new string(words.Where(static w => !string.IsNullOrEmpty(w)).Select(static w => w[0]).ToArray());
        }

        // For single word names, return first 3 chars
        return teamName.Length > 3 ? teamName.Substring(0, 3).ToUpper() : teamName.ToUpper();
    }

    private static CornerStatsData ExtractCornerStats(TeamLastXExtended team1LastX, TeamLastXExtended team2LastX)
    {
        return new()
        {
            HomeAvg = team1LastX != null ? CalculateHomeCornerAverage(team1LastX) : 0,
            AwayAvg = team2LastX != null ? CalculateAwayCornerAverage(team2LastX) : 0,
            TotalAvg = (team1LastX != null ? CalculateHomeCornerAverage(team1LastX) : 0) +
                       (team2LastX != null ? CalculateAwayCornerAverage(team2LastX) : 0)
        };
    }

    private static double CalculateHomeCornerAverage(TeamLastXExtended teamLastX)
    {
        if (teamLastX?.Matches == null || !teamLastX.Matches.Any() || teamLastX.Team?.Id == null)
            return 0;

        try
        {
            string teamId = teamLastX.Team.Id.ToString();
            var homeCorners = teamLastX.Matches
                .Where(m => m?.Teams?.Home?.Id == teamId &&
                           m.Corners != null)
                .Select(m => m.Corners.Home > 0 ? m.Corners.Home : 0)
                .ToList();

            return homeCorners.Any() ? homeCorners.Average() : 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error calculating home corner average: {ex.Message}");
            return 0;
        }
    }

    private static double CalculateAwayCornerAverage(TeamLastXExtended teamLastX)
    {
        if (teamLastX?.Matches == null || !teamLastX.Matches.Any() || teamLastX.Team?.Id == null)
            return 0;

        try
        {
            string teamId = teamLastX.Team.Id.ToString();
            var awayCorners = teamLastX.Matches
                .Where(m => m?.Teams?.Away?.Id == teamId &&
                           m.Corners != null)
                .Select(m => m.Corners.Away > 0 ? m.Corners.Away : 0)
                .ToList();

            return awayCorners.Any() ? awayCorners.Average() : 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error calculating away corner average: {ex.Message}");
            return 0;
        }
    }

    private static ScoringPatternsData ExtractScoringPatterns(TeamLastXExtended team1LastX, TeamLastXExtended team2LastX)
    {
        return new()
        {
            HomeFirstGoalRate = team1LastX != null ? CalculateHomeFirstGoalRate(team1LastX) : 0,
            AwayFirstGoalRate = team2LastX != null ? CalculateAwayFirstGoalRate(team2LastX) : 0,
            HomeLateGoalRate = team1LastX != null ? CalculateHomeLateGoalRate(team1LastX) : 0,
            AwayLateGoalRate = team2LastX != null ? CalculateAwayLateGoalRate(team2LastX) : 0
        };
    }

    private static int CalculateHomeFirstGoalRate(TeamLastXExtended teamLastX)
    {
        if (teamLastX?.Matches == null || !teamLastX.Matches.Any() || teamLastX.Team?.Id == null)
            return 0;

        try
        {
            int homeFirstCount = 0;
            int homeTotalMatches = 0;
            string teamId = teamLastX.Team.Id.ToString();

            foreach (var match in teamLastX.Matches)
            {
                var homeId = match?.Teams?.Home?.Id;
                if (homeId == teamId)
                {
                    homeTotalMatches++;
                    if (match.FirstGoal == "home")
                        homeFirstCount++;
                }
            }

            return homeTotalMatches > 0 ? homeFirstCount * 100 / homeTotalMatches : 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error calculating home first goal rate: {ex.Message}");
            return 0;
        }
    }

    private static int CalculateAwayFirstGoalRate(TeamLastXExtended teamLastX)
    {
        if (teamLastX?.Matches == null || !teamLastX.Matches.Any() || teamLastX.Team?.Id == null)
            return 0;

        try
        {
            int awayFirstCount = 0;
            int awayTotalMatches = 0;
            string teamId = teamLastX.Team.Id.ToString();

            foreach (var match in teamLastX.Matches)
            {
                var awayId = match?.Teams?.Away?.Id;
                if (awayId == teamId)
                {
                    awayTotalMatches++;
                    if (match.FirstGoal == "away")
                        awayFirstCount++;
                }
            }

            return awayTotalMatches > 0 ? awayFirstCount * 100 / awayTotalMatches : 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error calculating away first goal rate: {ex.Message}");
            return 0;
        }
    }

    private static int CalculateHomeLateGoalRate(TeamLastXExtended teamLastX)
    {
        if (teamLastX?.Matches == null || !teamLastX.Matches.Any() || teamLastX.Team?.Id == null)
            return 0;

        try
        {
            int homeLateCount = 0;
            int homeTotalMatches = 0;
            string teamId = teamLastX.Team.Id.ToString();

            foreach (var match in teamLastX.Matches)
            {
                var homeId = match?.Teams?.Home?.Id;
                if (homeId == teamId)
                {
                    homeTotalMatches++;
                    if (match.LastGoal == "home")
                        homeLateCount++;
                }
            }

            return homeTotalMatches > 0 ? homeLateCount * 100 / homeTotalMatches : 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error calculating home late goal rate: {ex.Message}");
            return 0;
        }
    }

    private static int CalculateAwayLateGoalRate(TeamLastXExtended teamLastX)
    {
        if (teamLastX?.Matches == null || !teamLastX.Matches.Any() || teamLastX.Team?.Id == null)
            return 0;

        try
        {
            int awayLateCount = 0;
            int awayTotalMatches = 0;
            string teamId = teamLastX.Team.Id.ToString();

            foreach (var match in teamLastX.Matches)
            {
                var awayId = match?.Teams?.Away?.Id;
                if (awayId == teamId)
                {
                    awayTotalMatches++;
                    if (match.LastGoal == "away")
                        awayLateCount++;
                }
            }

            return awayTotalMatches > 0 ? awayLateCount * 100 / awayTotalMatches : 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error calculating away late goal rate: {ex.Message}");
            return 0;
        }
    }

    private static double CalculateAverageGoals(EnrichedSportMatch match)
    {
        if (match.Team1LastX?.Matches == null || match.Team2LastX?.Matches == null ||
            match.Team1LastX.Team?.Id == null || match.Team2LastX.Team?.Id == null)
            return 0;

        string team1Id = match.Team1LastX.Team.Id.ToString();
        string team2Id = match.Team2LastX.Team.Id.ToString();

        var homeTeamGoals = match.Team1LastX.Matches.Sum(m =>
        {
            var homeId = m?.Teams?.Home?.Id;
            var awayId = m?.Teams?.Away?.Id;

            if (homeId == team1Id)
                return m.Result?.Home ?? 0;
            if (awayId == team1Id)
                return m.Result?.Away ?? 0;
            return 0;
        });

        var awayTeamGoals = match.Team2LastX.Matches.Sum(m =>
        {
            var homeId = m?.Teams?.Home?.Id;
            var awayId = m?.Teams?.Away?.Id;

            if (awayId == team2Id)
                return m.Result?.Away ?? 0;
            if (homeId == team2Id)
                return m.Result?.Home ?? 0;
            return 0;
        });

        var totalMatches = match.Team1LastX.Matches.Count + match.Team2LastX.Matches.Count;
        return totalMatches > 0 ? (homeTeamGoals + awayTeamGoals) / (double)totalMatches : 0;
    }

    private static bool CompareTeamIds(string id1, string id2)
    {
        if (string.IsNullOrEmpty(id1) || string.IsNullOrEmpty(id2)) return false;
        return id1 == id2;
    }

    private static bool CompareTeamIds(string id1, int id2)
    {
        if (string.IsNullOrEmpty(id1)) return false;
        return int.TryParse(id1, out int parsedId) && parsedId == id2;
    }

    private static bool CompareTeamIds(int? id1, int? id2)
    {
        if (!id1.HasValue || !id2.HasValue) return false;
        return id1.Value == id2.Value;
    }

    private static bool CompareTeamIdWithString(int? id, string stringId)
    {
        if (!id.HasValue || string.IsNullOrEmpty(stringId)) return false;
        return int.TryParse(stringId, out int parsedId) && id.Value == parsedId;
    }

    private static int? ParseTeamId(string teamId)
    {
        if (string.IsNullOrEmpty(teamId)) return null;
        if (int.TryParse(teamId, out int id)) return id;
        return null;
    }

    private static int CalculateTeamWinsFromMatches(List<ExtendedMatchStat> matches, TeamLastXExtended teamData)
    {
        if (teamData?.Team?.Id == null) return 0;
        int teamId = teamData.Team.Id;

        return matches.Count(m =>
        {
            if (CompareTeamIds(m?.Teams?.Home?.Id, teamId))
                return m.Result?.Winner == "home";
            if (CompareTeamIds(m?.Teams?.Away?.Id, teamId))
                return m.Result?.Winner == "away";
            return false;
        });
    }

    private static int CalculateCleanSheets(List<ExtendedMatchStat> matches, TeamLastXExtended teamData)
    {
        if (teamData?.Team?.Id == null) return 0;
        int teamId = teamData.Team.Id;

        return matches.Count(m =>
        {
            if (CompareTeamIds(m?.Teams?.Home?.Id, teamId))
                return (m.Result?.Away ?? 0) == 0;
            if (CompareTeamIds(m?.Teams?.Away?.Id, teamId))
                return (m.Result?.Home ?? 0) == 0;
            return false;
        });
    }

    private static int CalculateGoalsConceded(List<ExtendedMatchStat> matches, TeamLastXExtended teamData)
    {
        if (teamData?.Team?.Id == null) return 0;
        int teamId = teamData.Team.Id;

        return matches.Sum(m =>
        {
            if (CompareTeamIds(m?.Teams?.Home?.Id, teamId))
                return m.Result?.Away ?? 0;
            if (CompareTeamIds(m?.Teams?.Away?.Id, teamId))
                return m.Result?.Home ?? 0;
            return 0;
        });
    }

    private static int CalculatePositionGap(fredapi.SportRadarService.Background.TeamTableSliceModel tableSlice, string homeTeamId, string awayTeamId)
    {
        if (tableSlice?.TableRows == null || tableSlice.TableRows.Count < 2 ||
            string.IsNullOrEmpty(homeTeamId) || string.IsNullOrEmpty(awayTeamId))
            return 0;

        var homeTeamRow = tableSlice.TableRows.FirstOrDefault(row =>
            row?.Team?.Id != null && int.TryParse(homeTeamId, out var parsedId) && row.Team.Id.ToString() == homeTeamId);
        var awayTeamRow = tableSlice.TableRows.FirstOrDefault(row =>
            row?.Team?.Id != null && int.TryParse(awayTeamId, out var parsedId) && row.Team.Id.ToString() == awayTeamId);

        if (homeTeamRow == null || awayTeamRow == null)
            return 0;

        return Math.Abs(homeTeamRow.Pos - awayTeamRow.Pos);
    }

    private static int GetTeamPositionFromTable(fredapi.SportRadarService.Background.TeamTableSliceModel tableSlice, string teamId)
    {
        if (tableSlice?.TableRows == null || string.IsNullOrEmpty(teamId))
            return 0;

        var teamRow = tableSlice.TableRows.FirstOrDefault(row =>
            row?.Team?.Id != null && int.TryParse(teamId, out var parsedId) && row.Team.Id.ToString() == teamId);
        return teamRow?.Pos ?? 0;
    }

    private static string DetermineFavorite(List<MarketData> markets)
    {
        // If no markets data, default to draw
        if (markets == null || !markets.Any())
            return "draw";

        // Look for 1X2 market to determine favorite
        var x12Market = markets.FirstOrDefault(static m => m.Name == "1X2");
        if (x12Market?.Outcomes == null || !x12Market.Outcomes.Any())
            return "draw";

        var homeOdds = x12Market.Outcomes.FirstOrDefault(static o => o.Desc == "Home")?.Odds;
        var awayOdds = x12Market.Outcomes.FirstOrDefault(static o => o.Desc == "Away")?.Odds;

        if (string.IsNullOrEmpty(homeOdds) || string.IsNullOrEmpty(awayOdds))
            return "draw";

        if (double.TryParse(homeOdds, out double homeOddsValue) &&
            double.TryParse(awayOdds, out double awayOddsValue))
        {
            if (homeOddsValue < awayOddsValue)
                return "home";
            else if (awayOddsValue < homeOddsValue)
                return "away";
        }

        return "draw";
    }

    private static TeamData ExtractTeamData(
        string teamId,
        string teamName,
        TeamLastXExtended teamLastX,
        TeamLastXStats teamLastXStats,
        bool isHomeTeam,
        double avgOdds,
        double leagueAvgGoals,
        int position,
        string opponentName,
        double possession,
        SportMatch originalMatch
    )
    {
        try
        {
            // Create a TeamData object with basic information that should always be available
            var team = new TeamData
            {
                Name = teamName ?? "Unknown Team",
                Position = position >= 0 ? position : 0,
                Logo = "", // We can set a default logo or leave it empty
                IsHomeTeam = isHomeTeam,
                OpponentName = opponentName ?? "Unknown Opponent",
                AvgOdds = avgOdds > 0 ? avgOdds : 2.5, // Default to a reasonable value if odds unavailable
                LeagueAvgGoals = leagueAvgGoals,
                Possession = possession
            };

            // Safe access to team.Position to avoid negative positions
            if (team.Position < 0) team.Position = 0;

            // If we have team statistics, extract additional data safely
            if (teamLastX != null && teamLastX.Matches != null && teamLastX.Matches.Any())
            {
                // Calculate win percentages
                var totalMatches = teamLastX.Matches.Count;
                var homeMatches = teamLastX.Matches.Count(m =>
                    m.Teams?.Home?.Id != null &&
                    m.Teams.Home.Id.ToString() == teamId);
                var awayMatches = teamLastX.Matches.Count(m =>
                    m.Teams?.Away?.Id != null &&
                    m.Teams.Away.Id.ToString() == teamId);

                var wins = teamLastX.Matches.Count(m =>
                    (m.Teams?.Home?.Id != null && m.Teams.Home.Id.ToString() == teamId && m.Result?.Winner == "home") ||
                    (m.Teams?.Away?.Id != null && m.Teams.Away.Id.ToString() == teamId && m.Result?.Winner == "away"));

                var homeWins = teamLastX.Matches.Count(m =>
                    m.Teams?.Home?.Id != null &&
                    m.Teams.Home.Id.ToString() == teamId &&
                    m.Result?.Winner == "home");

                var awayWins = teamLastX.Matches.Count(m =>
                    m.Teams?.Away?.Id != null &&
                    m.Teams.Away.Id.ToString() == teamId &&
                    m.Result?.Winner == "away");

                var cleanSheets = teamLastX.Matches.Count(m =>
                    (m.Teams?.Home?.Id != null && m.Teams.Home.Id.ToString() == teamId && (m.Result?.Away ?? 1) == 0) ||
                    (m.Teams?.Away?.Id != null && m.Teams.Away.Id.ToString() == teamId && (m.Result?.Home ?? 1) == 0));

                // Safely set values
                team.WinPercentage = totalMatches > 0 ? (double)wins / totalMatches * 100 : 50;
                team.HomeWinPercentage = homeMatches > 0 ? (double)homeWins / homeMatches * 100 : 50;
                team.AwayWinPercentage = awayMatches > 0 ? (double)awayWins / awayMatches * 100 : 50;
                team.CleanSheetPercentage = totalMatches > 0 ? (double)cleanSheets / totalMatches * 100 : 30;

                // Track totals
                team.TotalHomeMatches = homeMatches;
                team.TotalAwayMatches = awayMatches;
                team.TotalHomeWins = homeWins;
                team.TotalAwayWins = awayWins;
                team.CleanSheets = cleanSheets;

                // Calculate average goals
                double goalsScored = 0;
                double goalsConceded = 0;
                double homeGoalsScored = 0;
                double homeGoalsConceded = 0;
                double awayGoalsScored = 0;
                double awayGoalsConceded = 0;

                foreach (var match in teamLastX.Matches)
                {
                    if (match.Teams?.Home?.Id != null && match.Teams.Home.Id.ToString() == teamId)
                    {
                        // Team playing at home
                        goalsScored += match.Result?.Home ?? 0;
                        goalsConceded += match.Result?.Away ?? 0;
                        homeGoalsScored += match.Result?.Home ?? 0;
                        homeGoalsConceded += match.Result?.Away ?? 0;
                    }
                    else if (match.Teams?.Away?.Id != null && match.Teams.Away.Id.ToString() == teamId)
                    {
                        // Team playing away
                        goalsScored += match.Result?.Away ?? 0;
                        goalsConceded += match.Result?.Home ?? 0;
                        awayGoalsScored += match.Result?.Away ?? 0;
                        awayGoalsConceded += match.Result?.Home ?? 0;
                    }
                }

                // Calculate averages
                team.AverageGoalsScored = totalMatches > 0 ? goalsScored / totalMatches : 1.2;
                team.AverageGoalsConceded = totalMatches > 0 ? goalsConceded / totalMatches : 1.0;
                team.HomeAverageGoalsScored = homeMatches > 0 ? homeGoalsScored / homeMatches : 1.3;
                team.HomeAverageGoalsConceded = homeMatches > 0 ? homeGoalsConceded / homeMatches : 0.9;
                team.AwayAverageGoalsScored = awayMatches > 0 ? awayGoalsScored / awayMatches : 1.1;
                team.AwayAverageGoalsConceded = awayMatches > 0 ? awayGoalsConceded / awayMatches : 1.2;

                // Set values for UI display
                team.AvgHomeGoals = team.HomeAverageGoalsScored;
                team.AvgAwayGoals = team.AwayAverageGoalsScored;
                team.AvgTotalGoals = team.AverageGoalsScored;

                // Calculate form
                team.Form = CalculateForm(teamLastX.Matches, teamId);
                team.HomeForm = CalculateHomeForm(teamLastX.Matches, teamId);
                team.AwayForm = CalculateAwayForm(teamLastX.Matches, teamId);

                // Calculate form strength as a number (0-100)
                team.FormStrength = CalculateFormStrength(teamLastX.Matches, teamId);
                team.FormRating = team.FormStrength;
            }
            else
            {
                // We don't set any defaults - these will be null if no data is available
                team.WinPercentage = 0;
                team.HomeWinPercentage = 0;
                team.AwayWinPercentage = 0;
                team.CleanSheetPercentage = 0;
                team.AverageGoalsScored = 0;
                team.AverageGoalsConceded = 0;
                team.HomeAverageGoalsScored = 0;
                team.HomeAverageGoalsConceded = 0;
                team.AwayAverageGoalsScored = 0;
                team.AwayAverageGoalsConceded = 0;
                team.FormStrength = 0;
                team.FormRating = 0;
                team.Form = "";
                team.HomeForm = "";
                team.AwayForm = "";
            }

            return team;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error extracting team data for {teamName}: {ex.Message}");

            // Return minimal team data to prevent errors
            return new TeamData
            {
                Name = teamName,
                Position = position >= 0 ? position : 0,
                FormStrength = 0,
                WinPercentage = 0,
                HomeWinPercentage = 0,
                AwayWinPercentage = 0,
                AverageGoalsScored = 0,
                AverageGoalsConceded = 0,
                Form = "",
                IsHomeTeam = isHomeTeam,
                OpponentName = opponentName
            };
        }
    }

    // Method to calculate overall form string (e.g., "WDLWW")
    private static string CalculateForm(List<ExtendedMatchStat> matches, string teamId)
    {
        if (matches == null || matches.Count == 0) return ""; // Empty string if no matches

        var form = new StringBuilder();

        // Process last 5 matches in reverse (most recent first)
        foreach (var match in matches.Take(5).Reverse())
        {
            if (match.Result == null) continue;

            bool isHome = match.Teams?.Home?.Id != null && match.Teams.Home.Id.ToString() == teamId;

            if (match.Result.Winner == null)
            {
                form.Append('D'); // Draw
            }
            else if ((isHome && match.Result.Winner == "home") ||
                     (!isHome && match.Result.Winner == "away"))
            {
                form.Append('W'); // Win
            }
            else
            {
                form.Append('L'); // Loss
            }
        }

        // Return actual form without fallback to dummy data
        return form.ToString();
    }

    // Method to calculate home form string
    private static string CalculateHomeForm(List<ExtendedMatchStat> matches, string teamId)
    {
        if (matches == null || matches.Count == 0) return ""; // Empty string if no matches

        var form = new StringBuilder();
        int count = 0;

        // Filter home matches and get form from last 3
        foreach (var match in matches.Where(m =>
            m.Teams?.Home?.Id != null &&
            m.Teams.Home.Id.ToString() == teamId)
            .Take(3)
            .Reverse())
        {
            if (match.Result == null) continue;

            if (match.Result.Winner == null)
            {
                form.Append('D'); // Draw
            }
            else if (match.Result.Winner == "home")
            {
                form.Append('W'); // Win
            }
            else
            {
                form.Append('L'); // Loss
            }

            count++;
        }

        // Return actual form without fallback to dummy data
        return form.ToString();
    }

    // Method to calculate away form string
    private static string CalculateAwayForm(List<ExtendedMatchStat> matches, string teamId)
    {
        if (matches == null || matches.Count == 0) return ""; // Empty string if no matches

        var form = new StringBuilder();

        // Filter away matches and get form from last 2
        foreach (var match in matches.Where(m =>
            m.Teams?.Away?.Id != null &&
            m.Teams.Away.Id.ToString() == teamId)
            .Take(2)
            .Reverse())
        {
            if (match.Result == null) continue;

            if (match.Result.Winner == null)
            {
                form.Append('D'); // Draw
            }
            else if (match.Result.Winner == "away")
            {
                form.Append('W'); // Win
            }
            else
            {
                form.Append('L'); // Loss
            }
        }

        // Return actual form without fallback to dummy data
        return form.ToString();
    }

    // Calculate form strength (0-100) based on recent match results
    private static double CalculateFormStrength(List<ExtendedMatchStat> matches, string teamId)
    {
        if (matches == null || matches.Count == 0) return 50; // Default strength if no matches

        double totalPoints = 0;
        double maxPoints = 0;
        double weightFactor = 1.0;

        // Get last 5 matches, most recent first
        foreach (var match in matches.Take(5))
        {
            if (match.Result == null) continue;

            bool isHome = match.Teams?.Home?.Id != null && match.Teams.Home.Id.ToString() == teamId;
            maxPoints += 3 * weightFactor; // Max possible points for this match

            // Award points based on result
            if ((isHome && match.Result.Winner == "home") ||
                (!isHome && match.Result.Winner == "away"))
            {
                totalPoints += 3 * weightFactor; // Win
            }
            else if (match.Result.Winner == null)
            {
                totalPoints += 1 * weightFactor; // Draw
            }

            // Decrease weight for older matches
            weightFactor *= 0.8;
        }

        // Calculate percentage (0-100)
        double formStrength = maxPoints > 0 ? (totalPoints / maxPoints) * 100 : 50;

        // Ensure value is between 0-100
        return Math.Min(100, Math.Max(0, formStrength));
    }

    private static List<string> GeneratePredictionReasons(EnrichedSportMatch match)
    {
        var reasons = new List<string>();

        if (match?.OriginalMatch?.Teams?.Home == null || match?.OriginalMatch?.Teams?.Away == null)
            return reasons;

        var homeTeam = match.OriginalMatch.Teams.Home.Name ?? "Home Team";
        var awayTeam = match.OriginalMatch.Teams.Away.Name ?? "Away Team";

        try
        {
            Console.WriteLine($"Generating prediction reasons for match {match.MatchId}: {homeTeam} vs {awayTeam}");

            // Extract team data first so we can access all stats
            var homeTeamData = ExtractTeamData(
                match.OriginalMatch.Teams.Home.Id,
                match.OriginalMatch.Teams.Home.Name,
                match.Team1LastX,
                match.LastXStatsTeam1,
                true,
                GetOddsValue(match.Markets?.FirstOrDefault(static m => m.Name == "1X2")?.Outcomes?.FirstOrDefault(static o => o.Desc == "Home")?.Odds),
                CalculateAverageGoals(match),
                GetTeamPositionFromTable(match.TeamTableSlice, match.OriginalMatch.Teams.Home.Id),
                match.OriginalMatch.Teams.Away.Name,
                0, // Using default value instead of match.LastXStatsTeam1?.Possession?.Total ?? 0
                match.OriginalMatch
            );

            var awayTeamData = ExtractTeamData(
                match.OriginalMatch.Teams.Away.Id,
                match.OriginalMatch.Teams.Away.Name,
                match.Team2LastX,
                match.LastXStatsTeam2,
                false,
                GetOddsValue(match.Markets?.FirstOrDefault(static m => m.Name == "1X2")?.Outcomes?.FirstOrDefault(static o => o.Desc == "Away")?.Odds),
                CalculateAverageGoals(match),
                GetTeamPositionFromTable(match.TeamTableSlice, match.OriginalMatch.Teams.Away.Id),
                match.OriginalMatch.Teams.Home.Name,
                0, // Default possession value
                match.OriginalMatch
            );

            Console.WriteLine($"Team data extracted for {homeTeam} and {awayTeam}");

            // 1. Position information (high priority)
            if (homeTeamData.Position > 0 && awayTeamData.Position > 0)
            {
                var positionGap = Math.Abs(homeTeamData.Position - awayTeamData.Position);
                if (positionGap > 0)
                {
                    var higherTeam = homeTeamData.Position < awayTeamData.Position ? homeTeam : awayTeam;
                    var lowerTeam = homeTeamData.Position < awayTeamData.Position ? awayTeam : homeTeam;

                    // More detailed position analysis
                    if (positionGap >= 10)
                    {
                        reasons.Add($"Major position difference: {higherTeam} ({Math.Min(homeTeamData.Position, awayTeamData.Position)}) vs {lowerTeam} ({Math.Max(homeTeamData.Position, awayTeamData.Position)})");
                    }
                    else if (positionGap >= 5)
                    {
                        reasons.Add($"Significant position gap: {higherTeam} ({Math.Min(homeTeamData.Position, awayTeamData.Position)}) vs {lowerTeam} ({Math.Max(homeTeamData.Position, awayTeamData.Position)})");
                    }
                    else
                    {
                        reasons.Add($"Position: {higherTeam} ({Math.Min(homeTeamData.Position, awayTeamData.Position)}) vs {lowerTeam} ({Math.Max(homeTeamData.Position, awayTeamData.Position)})");
                    }
                }
            }

            // 2. Form information (high priority)
            if (!string.IsNullOrEmpty(homeTeamData.Form) && homeTeamData.Form != "-")
            {
                // Analyze form to provide more context
                int homeWins = homeTeamData.Form.Count(static c => c == 'W');
                int homeDraws = homeTeamData.Form.Count(static c => c == 'D');
                int homeLosses = homeTeamData.Form.Count(static c => c == 'L');

                // Only highlight strong or weak form
                if (homeWins >= 3 && homeTeamData.Form.Length >= 4)
                {
                    reasons.Add($"{homeTeam} in strong form: {homeTeamData.Form} ({homeWins} wins in last {homeTeamData.Form.Length})");
                }
                else if (homeLosses >= 3 && homeTeamData.Form.Length >= 4)
                {
                    reasons.Add($"{homeTeam} in poor form: {homeTeamData.Form} ({homeLosses} losses in last {homeTeamData.Form.Length})");
                }
                else
                {
                    reasons.Add($"{homeTeam} form: {homeTeamData.Form}");
                }
            }

            if (!string.IsNullOrEmpty(awayTeamData.Form) && awayTeamData.Form != "-")
            {
                // Analyze form to provide more context
                int awayWins = awayTeamData.Form.Count(static c => c == 'W');
                int awayDraws = awayTeamData.Form.Count(static c => c == 'D');
                int awayLosses = awayTeamData.Form.Count(static c => c == 'L');

                // Only highlight strong or weak form
                if (awayWins >= 3 && awayTeamData.Form.Length >= 4)
                {
                    reasons.Add($"{awayTeam} in strong form: {awayTeamData.Form} ({awayWins} wins in last {awayTeamData.Form.Length})");
                }
                else if (awayLosses >= 3 && awayTeamData.Form.Length >= 4)
                {
                    reasons.Add($"{awayTeam} in poor form: {awayTeamData.Form} ({awayLosses} losses in last {awayTeamData.Form.Length})");
                }
                else
                {
                    reasons.Add($"{awayTeam} form: {awayTeamData.Form}");
                }
            }

            // 3. Goal scoring ability (medium priority)
            if (homeTeamData.AvgHomeGoals.HasValue && awayTeamData.AvgAwayGoals.HasValue)
            {
                double totalExpectedGoals = homeTeamData.AvgHomeGoals.Value + awayTeamData.AvgAwayGoals.Value;

                if (totalExpectedGoals >= 3.0)
                {
                    reasons.Add($"High-scoring potential: {homeTeam} ({homeTeamData.AvgHomeGoals:F2} home) vs {awayTeam} ({awayTeamData.AvgAwayGoals:F2} away)");
                }
                else if (totalExpectedGoals <= 1.5)
                {
                    reasons.Add($"Low-scoring potential: {homeTeam} ({homeTeamData.AvgHomeGoals:F2} home) vs {awayTeam} ({awayTeamData.AvgAwayGoals:F2} away)");
                }
            }
            else if (homeTeamData.AvgHomeGoals.HasValue && homeTeamData.AvgHomeGoals > 2.0)
            {
                reasons.Add($"{homeTeam} strong home scoring: {homeTeamData.AvgHomeGoals:F2} goals per game");
            }
            else if (awayTeamData.AvgAwayGoals.HasValue && awayTeamData.AvgAwayGoals > 1.5)
            {
                reasons.Add($"{awayTeam} strong away scoring: {awayTeamData.AvgAwayGoals:F2} goals per game");
            }

            // 4. Head-to-Head (high priority)
            if (match.TeamVersusRecent?.Matches != null && match.TeamVersusRecent.Matches.Any())
            {
                var h2h = ExtractHeadToHead(match.TeamVersusRecent, match.OriginalMatch.Teams.Home, match.OriginalMatch.Teams.Away);
                if (h2h.Matches > 0)
                {
                    var totalMatches = h2h.Wins + h2h.Draws + h2h.Losses;
                    if (totalMatches > 0)
                    {
                        // More detailed H2H analysis with goals information
                        double avgGoals = totalMatches > 0 ? (h2h.GoalsScored + h2h.GoalsConceded) / (double)totalMatches : 0;

                        if (h2h.Wins > h2h.Losses && h2h.Wins > 1)
                        {
                            reasons.Add($"H2H: {homeTeam} dominant with {h2h.Wins} wins in {totalMatches} matches ({avgGoals:F1} goals/game)");
                        }
                        else if (h2h.Losses > h2h.Wins && h2h.Losses > 1)
                        {
                            reasons.Add($"H2H: {awayTeam} dominant with {h2h.Losses} wins in {totalMatches} matches ({avgGoals:F1} goals/game)");
                        }
                        else if (h2h.Draws > Math.Max(h2h.Wins, h2h.Losses))
                        {
                            reasons.Add($"H2H: Teams often draw with {h2h.Draws} draws in {totalMatches} matches ({avgGoals:F1} goals/game)");
                        }
                        else if (avgGoals >= 3.0)
                        {
                            reasons.Add($"H2H: High-scoring fixtures averaging {avgGoals:F1} goals per game");
                        }
                        else if (avgGoals <= 1.5 && totalMatches >= 3)
                        {
                            reasons.Add($"H2H: Low-scoring fixtures averaging {avgGoals:F1} goals per game");
                        }
                        else
                        {
                            reasons.Add($"H2H: Balanced with {h2h.Wins}-{h2h.Draws}-{h2h.Losses} record in {totalMatches} matches");
                        }
                    }
                }
            }

            // 5. Clean sheet information (medium priority)
            if (homeTeamData.HomeCleanSheets > 0 && homeTeamData.TotalHomeMatches > 0)
            {
                var cleanSheetPercentage = homeTeamData.HomeCleanSheets * 100 / homeTeamData.TotalHomeMatches;
                if (cleanSheetPercentage >= 50) // More significant threshold
                {
                    reasons.Add($"{homeTeam} strong defense: {cleanSheetPercentage}% clean sheets at home");
                }
            }

            if (awayTeamData.AwayCleanSheets > 0 && awayTeamData.TotalAwayMatches > 0)
            {
                var cleanSheetPercentage = awayTeamData.AwayCleanSheets * 100 / awayTeamData.TotalAwayMatches;
                if (cleanSheetPercentage >= 40) // Slightly lower threshold for away teams
                {
                    reasons.Add($"{awayTeam} strong defense: {cleanSheetPercentage}% clean sheets away");
                }
            }

            // 6. BTTS analysis (useful for goals markets)
            if (homeTeamData.HomeBttsRate.HasValue && awayTeamData.AwayBttsRate.HasValue)
            {
                var combinedBttsRate = (homeTeamData.HomeBttsRate.Value + awayTeamData.AwayBttsRate.Value) / 2;

                if (combinedBttsRate >= 70)
                {
                    reasons.Add($"Both teams likely to score: {combinedBttsRate}% combined BTTS rate");
                }
                else if (combinedBttsRate <= 30 && homeTeamData.TotalHomeMatches >= 3 && awayTeamData.TotalAwayMatches >= 3)
                {
                    reasons.Add($"Low BTTS potential: only {combinedBttsRate}% combined BTTS rate");
                }
            }

            // 7. Odds info (high priority)
            if (match.Markets != null && match.Markets.Any())
            {
                var favorite = DetermineFavorite(match.Markets);
                var odds = ExtractOdds(match.Markets);
                if (odds.HomeWin > 0 && odds.AwayWin > 0)
                {
                    var favoriteTeam = favorite == "home" ? homeTeam : (favorite == "away" ? awayTeam : "Draw");
                    var oddsGap = Math.Abs(odds.HomeWin - odds.AwayWin);

                    if (oddsGap > 2.0)
                    {
                        reasons.Add($"Strong favorite: {favoriteTeam} (H: {odds.HomeWin:F2}, A: {odds.AwayWin:F2})");
                    }
                    else if (oddsGap > 1.0)
                    {
                        reasons.Add($"Moderate favorite: {favoriteTeam} (H: {odds.HomeWin:F2}, A: {odds.AwayWin:F2})");
                    }
                    else if (oddsGap < 0.3)
                    {
                        reasons.Add($"Evenly matched by odds (H: {odds.HomeWin:F2}, A: {odds.AwayWin:F2})");
                    }

                    // Add goals market info if available
                    if (odds.Over25Goals > 0 && odds.Under25Goals > 0)
                    {
                        // Lower odds indicate higher probability
                        bool overFavored = odds.Over25Goals < odds.Under25Goals;
                        double goalsDiff = Math.Abs(odds.Over25Goals - odds.Under25Goals);

                        if (goalsDiff > 1.0)
                        {
                            reasons.Add($"Goals market strongly favors {(overFavored ? "Over" : "Under")} 2.5 goals");
                        }
                    }
                }
            }

            // 8. Expected goals prediction (medium priority)
            var expectedGoals = CalculateExpectedGoals(match);
            if (expectedGoals > 0)
            {
                if (expectedGoals >= 3.0)
                {
                    reasons.Add($"High scoring game likely: {expectedGoals:F1} goals expected");
                }
                else if (expectedGoals <= 1.8)
                {
                    reasons.Add($"Low scoring game likely: {expectedGoals:F1} goals expected");
                }
            }

            // 9. Scoring first importance
            if (homeTeamData.ScoringFirstWinRate.HasValue && homeTeamData.ScoringFirstWinRate.Value > 70)
            {
                reasons.Add($"{homeTeam} critical to score first: {homeTeamData.ScoringFirstWinRate}% win rate when scoring first");
            }

            if (awayTeamData.ScoringFirstWinRate.HasValue && awayTeamData.ScoringFirstWinRate.Value > 70)
            {
                reasons.Add($"{awayTeam} critical to score first: {awayTeamData.ScoringFirstWinRate}% win rate when scoring first");
            }

            // 10. League characteristics if available
            if (match.OriginalMatch?.TournamentName != null)
            {
                // This could be extended with more league-specific analysis when available
                string leagueName = match.OriginalMatch.TournamentName;
                Console.WriteLine($"Processing league characteristics for {leagueName}");
            }

            Console.WriteLine($"Generated {reasons.Count} prediction reasons");

            // Make sure we don't have too many reasons - limit and prioritize
            if (reasons.Count > 5)
            {
                reasons = reasons
                    .OrderBy(static r =>
                        r.Contains("position") || r.Contains("Position") ? 0 :
                        r.StartsWith("H2H:") ? 1 :
                        r.Contains("favorite:") || r.Contains("matched by odds") ? 2 :
                        (r.Contains("form:") || r.Contains("in strong form") || r.Contains("in poor form")) ? 3 :
                        (r.Contains("scoring") || r.Contains("goals")) ? 4 :
                        r.Contains("clean sheets") ? 5 :
                        r.Contains("score first") ? 6 : 7)
                    .Take(5)
                    .ToList();

                Console.WriteLine($"Trimmed to top 5 most relevant reasons");
            }

            // If we still don't have any reasons, add a default
            if (!reasons.Any())
            {
                Console.WriteLine("No specific reasons found, adding generic reason");
                reasons.Add("Based on recent form and match odds");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating prediction reasons: {ex.Message}");
            // Add a generic reason if we couldn't generate specific ones
            if (!reasons.Any())
                reasons.Add("Based on recent form and odds");
        }

        return reasons.Distinct().ToList();
    }

    private static double CalculateExpectedGoals(EnrichedSportMatch match)
    {
        if (match == null)
            return 0;

        double expectedGoals = 0;
        double weight = 0;

        try
        {
            Console.WriteLine($"Calculating expected goals for match {match.MatchId}");

            // Method 1: Use odds from over/under markets to estimate expected goals
            if (match.Markets != null && match.Markets.Any())
            {
                var over25Market = match.Markets.FirstOrDefault(m =>
                    m.Name == "Over/Under" && m.Specifier == "total=2.5");
                var over15Market = match.Markets.FirstOrDefault(m =>
                    m.Name == "Over/Under" && m.Specifier == "total=1.5");

                // Calculate expected goals from odds
                if (over25Market?.Outcomes != null)
                {
                    var over25Probability = over25Market.Outcomes
                        .FirstOrDefault(o => o.Desc == "Over 2.5")?.Probability;

                    if (double.TryParse(over25Probability, out double over25Prob))
                    {
                        // If probability of over 2.5 is high, expected goals is higher
                        var goalsFrom25 = 2.5 + (over25Prob * 1.5); // Scale from 2.5 to 4.0
                        expectedGoals += goalsFrom25 * 0.6; // 60% weight
                        weight += 0.6;
                        Console.WriteLine($"Using Over 2.5 odds with probability {over25Prob}, adding {goalsFrom25 * 0.6} weighted goals");
                    }
                }

                if (over15Market?.Outcomes != null)
                {
                    var over15Probability = over15Market.Outcomes
                        .FirstOrDefault(o => o.Desc == "Over 1.5")?.Probability;

                    if (double.TryParse(over15Probability, out double over15Prob))
                    {
                        // If probability of over 1.5 is high, expected goals is higher
                        var goalsFrom15 = 1.5 + (over15Prob * 1.5); // Scale from 1.5 to 3.0
                        expectedGoals += goalsFrom15 * 0.4; // 40% weight
                        weight += 0.4;
                        Console.WriteLine($"Using Over 1.5 odds with probability {over15Prob}, adding {goalsFrom15 * 0.4} weighted goals");
                    }
                }
            }
            else
            {
                Console.WriteLine("No market data available for expected goals calculation");
            }

            // Method 2: Use team statistics if available
            if (weight < 0.7 &&
                match.Team1ScoringConceding?.Stats?.Scoring?.GoalsScoredAverage != null &&
                match.Team2ScoringConceding?.Stats?.Scoring?.GoalsScoredAverage != null)
            {
                // Get team averages from the scoring model
                var team1AvgGoals = match.Team1ScoringConceding?.Stats?.Scoring?.GoalsScoredAverage?.Total ?? 0;
                var team2AvgGoals = match.Team2ScoringConceding?.Stats?.Scoring?.GoalsScoredAverage?.Total ?? 0;

                // Calculate expected goals based on team averages (combined)
                var teamAvgGoals = (team1AvgGoals + team2AvgGoals) / 2;

                // Weight based on data quality
                double teamStatsWeight = 0.4;
                expectedGoals += teamAvgGoals * teamStatsWeight;
                weight += teamStatsWeight;
                Console.WriteLine($"Using team scoring models, avg: {teamAvgGoals}, adding {teamAvgGoals * teamStatsWeight} weighted goals");
            }

            // Method 3: Use historical match data if available
            if (weight < 0.9 && match.Team1LastX?.Matches != null && match.Team2LastX?.Matches != null &&
                match.Team1LastX.Team?.Id != null && match.Team2LastX.Team?.Id != null)
            {
                double homeTeamHomeGoals = 0;
                double awayTeamAwayGoals = 0;
                int homeCount = 0;
                int awayCount = 0;

                int team1Id = match.Team1LastX.Team.Id;
                int team2Id = match.Team2LastX.Team.Id;

                // Home team playing at home
                var homeTeamHomeMatches = match.Team1LastX.Matches
                    .Where(m =>
                    {
                        if (m?.Teams?.Home?.Id == null) return false;
                        if (!int.TryParse(m.Teams.Home.Id, out var parsedId)) return false;
                        return parsedId == team1Id;
                    })
                    .Take(5)
                    .ToList();

                if (homeTeamHomeMatches.Any())
                {
                    homeTeamHomeGoals = homeTeamHomeMatches
                        .Select(m => (m.Result?.Home ?? 0) + (m.Result?.Away ?? 0))
                        .Sum();
                    homeCount = homeTeamHomeMatches.Count();
                }

                // Away team playing away
                var awayTeamAwayMatches = match.Team2LastX.Matches
                    .Where(m =>
                    {
                        if (m?.Teams?.Away?.Id == null) return false;
                        if (!int.TryParse(m.Teams.Away.Id, out var parsedId)) return false;
                        return parsedId == team2Id;
                    })
                    .Take(5)
                    .ToList();

                if (awayTeamAwayMatches.Any())
                {
                    awayTeamAwayGoals = awayTeamAwayMatches
                        .Select(m => (m.Result?.Home ?? 0) + (m.Result?.Away ?? 0))
                        .Sum();
                    awayCount = awayTeamAwayMatches.Count();
                }

                // Calculate combined average if we have data
                if (homeCount > 0 || awayCount > 0)
                {
                    double historicalAvg = 0;
                    if (homeCount > 0 && awayCount > 0)
                    {
                        // We have both home and away data
                        historicalAvg = (homeTeamHomeGoals / homeCount + awayTeamAwayGoals / awayCount) / 2;
                    }
                    else if (homeCount > 0)
                    {
                        // Only home data
                        historicalAvg = homeTeamHomeGoals / homeCount;
                    }
                    else
                    {
                        // Only away data
                        historicalAvg = awayTeamAwayGoals / awayCount;
                    }

                    // Apply appropriate weight
                    double historicalWeight = 0.5;
                    expectedGoals += historicalAvg * historicalWeight;
                    weight += historicalWeight;
                    Console.WriteLine($"Using historical match data, avg: {historicalAvg}, adding {historicalAvg * historicalWeight} weighted goals");
                }
            }

            // Method 4: Use head-to-head if available
            if (weight < 1.0 && match.TeamVersusRecent?.Matches != null && match.TeamVersusRecent.Matches.Any())
            {
                var h2hMatches = match.TeamVersusRecent.Matches
                    .Where(m => m.Result?.Home != null && m.Result?.Away != null)
                    .ToList();

                if (h2hMatches.Any())
                {
                    var totalGoals = h2hMatches.Sum(m => (m.Result.Home ?? 0) + (m.Result.Away ?? 0));
                    var h2hAvg = (double)totalGoals / h2hMatches.Count;

                    // Apply appropriate weight - higher for H2H
                    double h2hWeight = 0.6;
                    expectedGoals += h2hAvg * h2hWeight;
                    weight += h2hWeight;
                    Console.WriteLine($"Using H2H data, avg: {h2hAvg}, adding {h2hAvg * h2hWeight} weighted goals");
                }
            }

            // Method 5: Use a reasonable default if all else fails
            if (weight < 0.5)
            {
                Console.WriteLine("Insufficient data, using default expected goals of 2.5");
                return 2.5; // Average number of goals per football match as fallback
            }

            // Calculate final expected goals as weighted average
            var finalExpectedGoals = Math.Round(expectedGoals / weight, 2);
            Console.WriteLine($"Final expected goals: {finalExpectedGoals} (total: {expectedGoals}, weight: {weight})");
            return finalExpectedGoals;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error calculating expected goals: {ex.Message}");
            return 2.5; // Reasonable default
        }
    }
}

// Add extension method for TeamTableSliceModel
public static class TeamTableSliceExtensions
{
    public static TableRowInfo GetTeamPosition(this TeamTableSliceModel tableSlice, string teamId)
    {
        if (tableSlice?.TableRows == null || string.IsNullOrEmpty(teamId))
            return null;

        return tableSlice.TableRows.FirstOrDefault(row =>
            row.Team?.Id.ToString() == teamId);
    }
}

public class RulesInfo
{
    [System.Text.Json.Serialization.JsonPropertyName("_doc")]
    public string Doc { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("_id")]
    [System.Text.Json.Serialization.JsonNumberHandling(System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString)]
    public int Id { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }
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