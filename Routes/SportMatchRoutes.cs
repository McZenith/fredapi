using System.Text;
using fredapi.Database;
using fredapi.SportRadarService.Background;
using MongoDB.Driver;
using MarketData = fredapi.SportRadarService.Background.ArbitrageLiveMatchBackgroundService.MarketData;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace fredapi.Routes;

// Client data models for the transformed data
public class PredictiveMatchData
{
    public int Id { get; set; }
    public string Date { get; set; }
    public string Time { get; set; }
    public string Venue { get; set; }
    public TeamData HomeTeam { get; set; }
    public TeamData AwayTeam { get; set; }
    public int PositionGap { get; set; }
    public string Favorite { get; set; }
    public int? ConfidenceScore { get; set; }
    public double? AverageGoals { get; set; }
    public double? ExpectedGoals { get; set; }
    public double? DefensiveStrength { get; set; }
    public OddsData Odds { get; set; }
    public HeadToHeadData HeadToHead { get; set; }
    public CornerStatsData CornerStats { get; set; }
    public ScoringPatternsData ScoringPatterns { get; set; }
    public List<string> ReasonsForPrediction { get; set; } = new();
}

public class TeamData
{
    public string Name { get; set; }
    public int Position { get; set; }
    public string Logo { get; set; }
    public double? AvgHomeGoals { get; set; }
    public double? AvgAwayGoals { get; set; }
    public double? AvgTotalGoals { get; set; }
    public int HomeMatchesOver15 { get; set; }
    public int AwayMatchesOver15 { get; set; }
    public int TotalHomeMatches { get; set; }
    public int TotalAwayMatches { get; set; }
    public string Form { get; set; }
    public string HomeForm { get; set; }
    public string AwayForm { get; set; }
    public int CleanSheets { get; set; }
    public int HomeCleanSheets { get; set; }
    public int AwayCleanSheets { get; set; }
    public int? ScoringFirstWinRate { get; set; }
    public int? ConcedingFirstWinRate { get; set; }
    public int? FirstHalfGoalsPercent { get; set; }
    public int? SecondHalfGoalsPercent { get; set; }
    public double? AvgCorners { get; set; }
    public int? BttsRate { get; set; }
    public int? HomeBttsRate { get; set; }
    public int? AwayBttsRate { get; set; }
    public int? LateGoalRate { get; set; }
    public Dictionary<string, double> GoalDistribution { get; set; } = new();
    public double? AgainstTopTeamsPoints { get; set; }
    public double? AgainstMidTeamsPoints { get; set; }
    public double? AgainstBottomTeamsPoints { get; set; }
}

public class OddsData
{
    public double HomeWin { get; set; }
    public double Draw { get; set; }
    public double AwayWin { get; set; }
    public double Over15Goals { get; set; }
    public double Under15Goals { get; set; }
    public double Over25Goals { get; set; }
    public double Under25Goals { get; set; }
    public double BttsYes { get; set; }
    public double BttsNo { get; set; }
}

public class HeadToHeadData
{
    public int Matches { get; set; }
    public int Wins { get; set; }
    public int Draws { get; set; }
    public int Losses { get; set; }
    public int GoalsScored { get; set; }
    public int GoalsConceded { get; set; }
    public List<RecentMatchData> RecentMatches { get; set; } = new();
}

public class RecentMatchData
{
    public string Date { get; set; }
    public string Result { get; set; }
}

public class CornerStatsData
{
    public double HomeAvg { get; set; }
    public double AwayAvg { get; set; }
    public double TotalAvg { get; set; }
}

public class ScoringPatternsData
{
    public int HomeFirstGoalRate { get; set; }
    public int AwayFirstGoalRate { get; set; }
    public int HomeLateGoalRate { get; set; }
    public int AwayLateGoalRate { get; set; }
}

public class PredictiveResponse
{
    public List<PredictiveMatchData> UpcomingMatches { get; set; } = new();
    public MetadataInfo Metadata { get; set; } = new();
}

public class MetadataInfo
{
    public int Total { get; set; }
    public string Date { get; set; }
    public Dictionary<string, LeagueData> LeagueData { get; set; } = new();
}

public class LeagueData
{
    public int Matches { get; set; }
    public double TotalGoals { get; set; }
    public int HomeWinRate { get; set; }
    public int DrawRate { get; set; }
    public int AwayWinRate { get; set; }
    public int BttsRate { get; set; }
}

public static class SportMatchRoutes
{
    private static readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private static readonly ConcurrentDictionary<string, (double? ExpectedGoals, int? ConfidenceScore)> _calculationCache =
        new ConcurrentDictionary<string, (double?, int?)>();

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
            var matches = await collection.Find(FilterDefinition<EnrichedSportMatch>.Empty)
                .SortByDescending(m => m.MatchTime)
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
            var filter = Builders<EnrichedSportMatch>.Filter.Eq(m => m.MatchId, matchId);
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
            // Check if we have a cached result in memory
            if (_cache.TryGetValue(cacheKey, out PredictiveResponse cachedResponse))
            {
                Console.WriteLine("Returning cached prediction data");
                return Results.Ok(cachedResponse);
            }

            var collection = mongoDbService.GetCollection<EnrichedSportMatch>("EnrichedSportMatches");

            // Get only valid matches with complete data
            var filter = Builders<EnrichedSportMatch>.Filter.Eq(m => m.IsValid, true) &
                         Builders<EnrichedSportMatch>.Filter.Ne(m => m.OriginalMatch, null) &
                         Builders<EnrichedSportMatch>.Filter.Ne(m => m.OriginalMatch.Teams, null) &
                         Builders<EnrichedSportMatch>.Filter.Ne(m => m.OriginalMatch.Teams.Home, null) &
                         Builders<EnrichedSportMatch>.Filter.Ne(m => m.OriginalMatch.Teams.Away, null) &
                         Builders<EnrichedSportMatch>.Filter.Ne(m => m.TeamTableSlice, null) &
                         Builders<EnrichedSportMatch>.Filter.Gt(m => m.TeamTableSlice.TotalRows, 0);

            Console.WriteLine("Fetching matches from database...");
            var dbFetchTimer = System.Diagnostics.Stopwatch.StartNew();
            var matches = await collection.Find(filter)
                .SortByDescending(m => m.MatchTime)
                .ToListAsync();
            dbFetchTimer.Stop();
            Console.WriteLine($"Found {matches.Count} matches in {dbFetchTimer.ElapsedMilliseconds}ms");

            // Filter out invalid matches after fetching
            var validMatches = matches.Where(m =>
                m.TeamTableSlice != null &&
                m.LastXStatsTeam1 != null &&
                m.LastXStatsTeam2 != null &&
                m.Team1LastX != null &&
                m.Team2LastX != null)
                .ToList();

            Console.WriteLine($"After filtering: {validMatches.Count} valid matches with data");

            // Transform matches efficiently - using original method 
            Console.WriteLine("Starting transformation...");
            var transformTimer = System.Diagnostics.Stopwatch.StartNew();
            var transformedMatches = new List<PredictiveMatchData>();

            foreach (var match in validMatches)
            {
                try
                {
                    var predictiveData = new PredictiveMatchData
                    {
                        Id = int.TryParse(match.MatchId, out int id) ? id : 0,
                        Date = match.MatchTime.ToString("yyyy-MM-dd"),
                        Time = match.MatchTime.ToString("HH:mm"),
                        Venue = match.OriginalMatch?.TournamentName ?? "",
                        HomeTeam = ExtractTeamData(
                            match.OriginalMatch?.Teams?.Home,
                            match.TeamTableSlice,
                            match.LastXStatsTeam1,
                            match.Team1LastX,
                            match.Team1ScoringConceding,
                            true
                        ),
                        AwayTeam = ExtractTeamData(
                            match.OriginalMatch?.Teams?.Away,
                            match.TeamTableSlice,
                            match.LastXStatsTeam2,
                            match.Team2LastX,
                            match.Team2ScoringConceding,
                            false
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

            transformTimer.Stop();
            Console.WriteLine($"Transformation completed in {transformTimer.ElapsedMilliseconds}ms");

            // Process league metadata
            Console.WriteLine("Calculating league metadata...");
            var metadataTimer = System.Diagnostics.Stopwatch.StartNew();

            var leagueMetadata = validMatches
                .GroupBy(m => m.OriginalMatch?.TournamentName ?? "Unknown Tournament")
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

            metadataTimer.Stop();
            Console.WriteLine($"Metadata calculation completed in {metadataTimer.ElapsedMilliseconds}ms");

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

            // Cache the response for 1 hour
            _cache.Set(cacheKey, response, TimeSpan.FromHours(1));

            sw.Stop();
            Console.WriteLine($"Prediction data processed in {sw.ElapsedMilliseconds}ms total");

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            sw.Stop();
            Console.WriteLine($"Error fetching prediction data in {sw.ElapsedMilliseconds}ms: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
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
            .Where(m => m.TeamVersusRecent?.Matches != null)
            .SelectMany(m => m.TeamVersusRecent.Matches
                .Where(pm => pm.Result?.Home != null && pm.Result.Away != null)
                .Select(pm => (pm.Result.Home ?? 0) + (pm.Result.Away ?? 0)))
            .ToList();

        return matchesWithStats.Any() ? matchesWithStats.Average() : 0;
    }

    private static int CalculateHomeWinRateForLeague(List<EnrichedSportMatch> matches)
    {
        if (matches == null || !matches.Any())
            return 0;

        var matchResults = matches
            .Where(m => m.TeamVersusRecent?.Matches != null)
            .SelectMany(m => m.TeamVersusRecent.Matches
                .Where(pm => pm.Result != null)
                .Select(pm => pm.Result.Winner == "home" ? 1 : 0))
            .ToList();

        return matchResults.Any() ? (int)(matchResults.Sum() * 100.0 / matchResults.Count) : 0;
    }

    private static int CalculateDrawRateForLeague(List<EnrichedSportMatch> matches)
    {
        if (matches == null || !matches.Any())
            return 0;

        var matchResults = matches
            .Where(m => m.TeamVersusRecent?.Matches != null)
            .SelectMany(m => m.TeamVersusRecent.Matches
                .Where(pm => pm.Result != null)
                .Select(pm => pm.Result.Winner == null ? 1 : 0))
            .ToList();

        return matchResults.Any() ? (int)(matchResults.Sum() * 100.0 / matchResults.Count) : 0;
    }

    private static int CalculateAwayWinRateForLeague(List<EnrichedSportMatch> matches)
    {
        if (matches == null || !matches.Any())
            return 0;

        var matchResults = matches
            .Where(m => m.TeamVersusRecent?.Matches != null)
            .SelectMany(m => m.TeamVersusRecent.Matches
                .Where(pm => pm.Result != null)
                .Select(pm => pm.Result.Winner == "away" ? 1 : 0))
            .ToList();

        return matchResults.Any() ? (int)(matchResults.Sum() * 100.0 / matchResults.Count) : 0;
    }

    private static int CalculateBttsRateForLeague(List<EnrichedSportMatch> matches)
    {
        if (matches == null || !matches.Any())
            return 0;

        var bttsMatches = matches
            .Where(m => m.TeamVersusRecent?.Matches != null)
            .SelectMany(m => m.TeamVersusRecent.Matches
                .Where(pm => pm.Result?.Home != null && pm.Result?.Away != null)
                .Select(pm => pm.Result.Home > 0 && pm.Result.Away > 0 ? 1 : 0))
            .ToList();

        return bttsMatches.Any() ? (int)(bttsMatches.Sum() * 100.0 / bttsMatches.Count) : 0;
    }

    private static PredictiveMatchData TransformToPredictiveData(EnrichedSportMatch match) =>
        new()
        {
            Id = int.TryParse(match.MatchId, out int id) ? id : 0,
            Date = match.MatchTime.ToString("yyyy-MM-dd"),
            Time = match.MatchTime.ToString("HH:mm"),
            Venue = match.OriginalMatch?.TournamentName ?? "",
            HomeTeam = ExtractTeamData(
                match.OriginalMatch?.Teams?.Home,
                match.TeamTableSlice,
                match.LastXStatsTeam1,
                match.Team1LastX,
                match.Team1ScoringConceding,
                true
            ),
            AwayTeam = ExtractTeamData(
                match.OriginalMatch?.Teams?.Away,
                match.TeamTableSlice,
                match.LastXStatsTeam2,
                match.Team2LastX,
                match.Team2ScoringConceding,
                false
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
            if (weight < 0.9 && match.Team1LastX?.Matches != null && match.Team2LastX?.Matches != null)
            {
                double homeTeamHomeGoals = 0;
                double awayTeamAwayGoals = 0;
                int homeCount = 0;
                int awayCount = 0;

                // Home team playing at home
                var homeTeamHomeMatches = match.Team1LastX.Matches
                    .Where(m => m.Teams?.Home?.Id == match.Team1LastX.Team?.Id)
                    .Take(5)
                    .ToList();

                if (homeTeamHomeMatches.Any())
                {
                    homeTeamHomeGoals = homeTeamHomeMatches
                        .Select(m => (m.Result?.Home ?? 0) + (m.Result?.Away ?? 0))
                        .Sum();
                    homeCount = homeTeamHomeMatches.Count;
                }

                // Away team playing away
                var awayTeamAwayMatches = match.Team2LastX.Matches
                    .Where(m => m.Teams?.Away?.Id == match.Team2LastX.Team?.Id)
                    .Take(5)
                    .ToList();

                if (awayTeamAwayMatches.Any())
                {
                    awayTeamAwayGoals = awayTeamAwayMatches
                        .Select(m => (m.Result?.Home ?? 0) + (m.Result?.Away ?? 0))
                        .Sum();
                    awayCount = awayTeamAwayMatches.Count;
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
                        .Count(m => m.Result?.Winner == (m.Teams?.Home?.Id == match.Team1LastX.Team?.Id ? "home" : "away"));
                    var team2Wins = team2RecentMatches
                        .Count(m => m.Result?.Winner == (m.Teams?.Home?.Id == match.Team2LastX.Team?.Id ? "home" : "away"));

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
                        .Where(m => m.Teams?.Home?.Id == match.Team1LastX.Team?.Id)
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
                (m.Teams?.Home?.Id == match.Team1LastX.Team?.Id && (m.Result?.Away ?? 0) == 0) ||
                (m.Teams?.Away?.Id == match.Team1LastX.Team?.Id && (m.Result?.Home ?? 0) == 0));

        var team2CleanSheets = match.Team2LastX.Matches
            .Take(10)
            .Count(m =>
                (m.Teams?.Home?.Id == match.Team2LastX.Team?.Id && (m.Result?.Away ?? 0) == 0) ||
                (m.Teams?.Away?.Id == match.Team2LastX.Team?.Id && (m.Result?.Home ?? 0) == 0));

        // Calculate average goals conceded per match
        var team1Matches = match.Team1LastX.Matches.Take(10).ToList();
        var team2Matches = match.Team2LastX.Matches.Take(10).ToList();

        var team1GoalsConceded = team1Matches.Sum(m =>
            m.Teams?.Home?.Id == match.Team1LastX.Team?.Id ? (m.Result?.Away ?? 0) : (m.Result?.Home ?? 0));

        var team2GoalsConceded = team2Matches.Sum(m =>
            m.Teams?.Home?.Id == match.Team2LastX.Team?.Id ? (m.Result?.Away ?? 0) : (m.Result?.Home ?? 0));

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

    private static OddsData ExtractOdds(List<MarketData> markets) =>
        new()
        {
            HomeWin = GetOddsValue(markets?.FirstOrDefault(m => m.Name == "1X2")?.Outcomes
                ?.FirstOrDefault(o => o.Desc == "Home")?.Odds),

            Draw = GetOddsValue(markets?.FirstOrDefault(m => m.Name == "1X2")?.Outcomes
                ?.FirstOrDefault(o => o.Desc == "Draw")?.Odds),

            AwayWin = GetOddsValue(markets?.FirstOrDefault(m => m.Name == "1X2")?.Outcomes
                ?.FirstOrDefault(o => o.Desc == "Away")?.Odds),

            Over15Goals = GetOddsValue(markets?.FirstOrDefault(m => m.Name == "Over/Under" && m.Specifier == "total=1.5")?.Outcomes
                ?.FirstOrDefault(o => o.Desc == "Over 1.5")?.Odds),

            Under15Goals = GetOddsValue(markets?.FirstOrDefault(m => m.Name == "Over/Under" && m.Specifier == "total=1.5")?.Outcomes
                ?.FirstOrDefault(o => o.Desc == "Under 1.5")?.Odds),

            Over25Goals = GetOddsValue(markets?.FirstOrDefault(m => m.Name == "Over/Under" && m.Specifier == "total=2.5")?.Outcomes
                ?.FirstOrDefault(o => o.Desc == "Over 2.5")?.Odds),

            Under25Goals = GetOddsValue(markets?.FirstOrDefault(m => m.Name == "Over/Under" && m.Specifier == "total=2.5")?.Outcomes
                ?.FirstOrDefault(o => o.Desc == "Under 2.5")?.Odds),

            BttsYes = GetOddsValue(markets?.FirstOrDefault(m => m.Name == "GG/NG")?.Outcomes
                ?.FirstOrDefault(o => o.Desc == "Yes")?.Odds),

            BttsNo = GetOddsValue(markets?.FirstOrDefault(m => m.Name == "GG/NG")?.Outcomes
                ?.FirstOrDefault(o => o.Desc == "No")?.Odds)
        };

    private static double GetOddsValue(string oddsString)
    {
        if (double.TryParse(oddsString, out double odds))
            return odds;
        return 2.0; // Default to even odds when missing
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
                return new string(words.Where(w => !string.IsNullOrEmpty(w)).Select(w => w[0]).Take(3).ToArray());
            return new string(words.Where(w => !string.IsNullOrEmpty(w)).Select(w => w[0]).ToArray());
        }

        // For single word names, return first 3 chars
        return teamName.Length > 3 ? teamName.Substring(0, 3).ToUpper() : teamName.ToUpper();
    }

    private static CornerStatsData ExtractCornerStats(TeamLastXExtendedModel team1LastX, TeamLastXExtendedModel team2LastX) =>
        new()
        {
            HomeAvg = CalculateHomeCornerAverage(team1LastX),
            AwayAvg = CalculateAwayCornerAverage(team2LastX),
            TotalAvg = CalculateHomeCornerAverage(team1LastX) + CalculateAwayCornerAverage(team2LastX)
        };

    private static double CalculateHomeCornerAverage(TeamLastXExtendedModel teamLastX)
    {
        if (teamLastX?.Matches == null || !teamLastX.Matches.Any())
            return 0;

        try
        {
            var homeCorners = teamLastX.Matches
                .Where(m => m?.Teams?.Home?.Id == teamLastX.Team?.Id && m.Corners != null)
                .Select(m => m.Corners.Home)
            .ToList();

            return homeCorners.Any() ? homeCorners.Average() : 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error calculating home corner average: {ex.Message}");
            return 0;
        }
    }

    private static double CalculateAwayCornerAverage(TeamLastXExtendedModel teamLastX)
    {
        if (teamLastX?.Matches == null || !teamLastX.Matches.Any())
            return 0;

        try
        {
            var awayCorners = teamLastX.Matches
                .Where(m => m?.Teams?.Away?.Id == teamLastX.Team?.Id && m.Corners != null)
                .Select(m => m.Corners.Away)
                .ToList();

            return awayCorners.Any() ? awayCorners.Average() : 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error calculating away corner average: {ex.Message}");
            return 0;
        }
    }

    private static ScoringPatternsData ExtractScoringPatterns(TeamLastXExtendedModel team1LastX, TeamLastXExtendedModel team2LastX) =>
        new()
        {
            HomeFirstGoalRate = CalculateHomeFirstGoalRate(team1LastX),
            AwayFirstGoalRate = CalculateAwayFirstGoalRate(team2LastX),
            HomeLateGoalRate = CalculateHomeLateGoalRate(team1LastX),
            AwayLateGoalRate = CalculateAwayLateGoalRate(team2LastX)
        };

    private static int CalculateHomeFirstGoalRate(TeamLastXExtendedModel teamLastX)
    {
        if (teamLastX?.Matches == null || !teamLastX.Matches.Any())
            return 0;

        try
        {
            int homeFirstCount = 0;
            int homeTotalMatches = 0;

            foreach (var match in teamLastX.Matches)
            {
                if (match?.Teams?.Home?.Id == teamLastX.Team?.Id)
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

    private static int CalculateAwayFirstGoalRate(TeamLastXExtendedModel teamLastX)
    {
        if (teamLastX?.Matches == null || !teamLastX.Matches.Any())
            return 0;

        try
        {
            int awayFirstCount = 0;
            int awayTotalMatches = 0;

            foreach (var match in teamLastX.Matches)
            {
                if (match?.Teams?.Away?.Id == teamLastX.Team?.Id)
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

    private static int CalculateHomeLateGoalRate(TeamLastXExtendedModel teamLastX)
    {
        if (teamLastX?.Matches == null || !teamLastX.Matches.Any())
            return 0;

        try
        {
            int homeLateCount = 0;
            int homeTotalMatches = 0;

            foreach (var match in teamLastX.Matches)
            {
                if (match?.Teams?.Home?.Id == teamLastX.Team?.Id)
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

    private static int CalculateAwayLateGoalRate(TeamLastXExtendedModel teamLastX)
    {
        if (teamLastX?.Matches == null || !teamLastX.Matches.Any())
            return 0;

        try
        {
            int awayLateCount = 0;
            int awayTotalMatches = 0;

            foreach (var match in teamLastX.Matches)
            {
                if (match?.Teams?.Away?.Id == teamLastX.Team?.Id)
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
        // Calculate from actual data or return null
        if (match.Team1LastX?.Matches == null || match.Team2LastX?.Matches == null)
            return 0;

        var homeTeamGoals = match.Team1LastX.Matches.Sum(m =>
            m.Teams?.Home?.Id == match.Team1LastX.Team?.Id ?
            (m.Result?.Home ?? 0) : (m.Result?.Away ?? 0));

        var awayTeamGoals = match.Team2LastX.Matches.Sum(m =>
            m.Teams?.Away?.Id == match.Team2LastX.Team?.Id ?
            (m.Result?.Away ?? 0) : (m.Result?.Home ?? 0));

        var totalMatches = match.Team1LastX.Matches.Count + match.Team2LastX.Matches.Count;

        return totalMatches > 0 ? (homeTeamGoals + awayTeamGoals) / (double)totalMatches : 0;
    }

    private static int CalculatePositionGap(TeamTableSliceModel tableSlice, string homeTeamId, string awayTeamId)
    {
        if (tableSlice?.TableRows == null || tableSlice.TableRows.Count < 2 ||
            string.IsNullOrEmpty(homeTeamId) || string.IsNullOrEmpty(awayTeamId))
            return 0;

        // Use the GetTeamPosition helper method from TeamTableSliceModel to find team positions
        var homeTeamRow = tableSlice.GetTeamPosition(homeTeamId);
        var awayTeamRow = tableSlice.GetTeamPosition(awayTeamId);

        if (homeTeamRow == null || awayTeamRow == null)
            return 0;

        return Math.Abs(homeTeamRow.Position - awayTeamRow.Position);
    }

    private static string DetermineFavorite(List<MarketData> markets)
    {
        // If no markets data, default to draw
        if (markets == null || !markets.Any())
            return "draw";

        // Look for 1X2 market to determine favorite
        var x12Market = markets.FirstOrDefault(m => m.Name == "1X2");
        if (x12Market?.Outcomes == null || !x12Market.Outcomes.Any())
            return "draw";

        var homeOdds = x12Market.Outcomes.FirstOrDefault(o => o.Desc == "Home")?.Odds;
        var awayOdds = x12Market.Outcomes.FirstOrDefault(o => o.Desc == "Away")?.Odds;

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
        SportTeam team,
        TeamTableSliceModel tableSlice,
        TeamLastXStatsModel lastXStats,
        TeamLastXExtendedModel lastXExtended,
        TeamScoringConcedingModel scoringConceding,
        bool isHome)
    {
        // Return empty team data with defaults if team is null
        if (team == null)
            return new TeamData
            {
                Name = isHome ? "Home Team" : "Away Team",
                Position = 0,
                Logo = "",
                Form = "",
                HomeForm = "",
                AwayForm = "",
                GoalDistribution = new Dictionary<string, double>()
            };

        Console.WriteLine($"Processing data for team {team.Name} (ID: {team.Id}) as {(isHome ? "home" : "away")} team");

        // Find team's position in table
        var teamPosition = 0;
        if (tableSlice?.TableRows != null && !string.IsNullOrEmpty(team.Id))
        {
            // Find the correct team row in the table using team ID and the GetTeamPosition helper
            var tableRow = tableSlice.GetTeamPosition(team.Id);
            if (tableRow != null)
            {
                teamPosition = tableRow.Position;
                Console.WriteLine($"Found position for team {team.Name}: {teamPosition}");
            }
            else
            {
                Console.WriteLine($"WARNING: Could not find position for team {team.Name} (ID: {team.Id}) in table slice");
            }
        }
        else
        {
            Console.WriteLine($"WARNING: TableSlice is null or team ID is empty for {team.Name}");
        }

        // Build logo URL using team ID
        // Use a consistent CDN URL format: <base_url>/<team_id>.png
        var logoUrl = !string.IsNullOrEmpty(team.Id)
            ? $"https://cdn.sportradar.com/ls/crest/big/{team.Id}.png"
            : "";

        // Calculate form stats
        var lastMatches = lastXExtended?.Matches ?? new List<ExtendedMatchStat>();

        // Verify we have the correct team ID in the extended match data
        var teamInExtendedData = lastXExtended?.Team?.Id;
        if (teamInExtendedData != null && team.Id != teamInExtendedData)
        {
            Console.WriteLine($"WARNING: Team ID mismatch - Expected {team.Id} but found {teamInExtendedData} in extended data");
        }

        // Create separate collections to track form for different contexts
        var overallForm = new StringBuilder();
        var homeForm = new StringBuilder();
        var awayForm = new StringBuilder();

        // Track match counts for detailed analysis
        int homeMatchesCount = 0;
        int awayMatchesCount = 0;
        int totalMatchesCount = 0;

        int homeMatchesOver15 = 0;
        int awayMatchesOver15 = 0;
        int totalHomeMatches = 0;
        int totalAwayMatches = 0;
        int cleanSheets = 0;
        int homeCleanSheets = 0;
        int awayCleanSheets = 0;
        int firstGoalWins = 0;
        int firstGoalTotal = 0;
        int concededFirstWins = 0;
        int concededFirstTotal = 0;
        int bttsCount = 0;
        int homeBttsCount = 0;
        int awayBttsCount = 0;
        int lateGoalCount = 0;
        int firstHalfGoals = 0;
        int secondHalfGoals = 0;
        int totalGoals = 0;

        double totalHomeGoalsScored = 0;
        double totalAwayGoalsScored = 0;

        int winCount = 0;
        int drawCount = 0;
        int lossCount = 0;

        var cornerSum = 0.0;
        var goalDistribution = new Dictionary<string, double>();

        Console.WriteLine($"Processing {lastMatches.Count} matches for team {team.Name} (ID: {team.Id})");

        try
        {
            // Process each match to build form strings and calculate stats
            foreach (var match in lastMatches)
            {
                if (match?.Teams == null || match.Result == null)
                {
                    Console.WriteLine("Skipping match with null teams or result");
                    continue;
                }

                // Determine if this team is home or away in this match - use direct ID comparison
                bool isTeamHome = match.Teams?.Home?.Id == team.Id;
                bool isTeamAway = match.Teams?.Away?.Id == team.Id;

                if (!isTeamHome && !isTeamAway)
                {
                    Console.WriteLine($"WARNING: Team {team.Id} not found in match (home: {match.Teams?.Home?.Id}, away: {match.Teams?.Away?.Id})");
                    continue;
                }

                totalMatchesCount++;

                // Count match for the appropriate context
                if (isTeamHome)
                    homeMatchesCount++;
                else if (isTeamAway)
                    awayMatchesCount++;

                var teamScore = isTeamHome ? match.Result?.Home : match.Result?.Away;
                var opponentScore = isTeamHome ? match.Result?.Away : match.Result?.Home;

                if (teamScore == null || opponentScore == null)
                {
                    Console.WriteLine("Skipping match with null scores");
                    continue;
                }

                // Track goals for average calculations
                if (isTeamHome)
                {
                    totalHomeMatches++;
                    totalHomeGoalsScored += teamScore.Value;
                }
                else
                {
                    totalAwayMatches++;
                    totalAwayGoalsScored += teamScore.Value;
                }

                // Form calculation - W for win, D for draw, L for loss
                char formResult;
                if (teamScore > opponentScore)
                {
                    formResult = 'W';
                    winCount++;
                }
                else if (teamScore < opponentScore)
                {
                    formResult = 'L';
                    lossCount++;
                }
                else
                {
                    formResult = 'D';
                    drawCount++;
                }

                // Add to overall form string (limit to last 5 matches)
                if (overallForm.Length < 5)
                    overallForm.Append(formResult);

                // Add to specific context form
                if (isTeamHome && homeForm.Length < 5)
                {
                    homeForm.Append(formResult);
                }
                else if (isTeamAway && awayForm.Length < 5)
                {
                    awayForm.Append(formResult);
                }

                // Count over 1.5 goals games and other stats
                if (isTeamHome)
                {
                    if ((teamScore + opponentScore) > 1)
                        homeMatchesOver15++;
                    if (opponentScore == 0)
                        homeCleanSheets++;
                    if (teamScore > 0 && opponentScore > 0)
                        homeBttsCount++;
                }
                else
                {
                    if ((teamScore + opponentScore) > 1)
                        awayMatchesOver15++;
                    if (opponentScore == 0)
                        awayCleanSheets++;
                    if (teamScore > 0 && opponentScore > 0)
                        awayBttsCount++;
                }

                // Clean sheets
                if (opponentScore == 0)
                    cleanSheets++;

                // Both teams scored
                if (teamScore > 0 && opponentScore > 0)
                    bttsCount++;

                // First goal stats - ensure we're interpreting this correctly for team
                var scoredFirst = match.FirstGoal == (isTeamHome ? "home" : "away");
                if (scoredFirst)
                {
                    firstGoalTotal++;
                    if (teamScore > opponentScore)
                        firstGoalWins++;
                }

                if (match.FirstGoal == (isTeamHome ? "away" : "home"))
                {
                    concededFirstTotal++;
                    if (teamScore > opponentScore)
                        concededFirstWins++;
                }

                // Late goals
                if (match.LastGoal == (isTeamHome ? "home" : "away"))
                    lateGoalCount++;

                // Corner stats - ensure we have valid corners data
                if (match.Corners != null)
                {
                    cornerSum += isTeamHome ? (match.Corners.Home > 0 ? match.Corners.Home : 0) : (match.Corners.Away > 0 ? match.Corners.Away : 0);
                }

                // Calculate total goals for this team to use in percentage calculations
                totalGoals += teamScore.Value;
            }

            Console.WriteLine($"Team {team.Name} stats: Total matches={totalMatchesCount} (Home={homeMatchesCount}, Away={awayMatchesCount})");
            Console.WriteLine($"Team {team.Name} outcomes: W={winCount}, D={drawCount}, L={lossCount}");
            Console.WriteLine($"Team {team.Name} form: Overall={overallForm}, HomeForm={homeForm}, AwayForm={awayForm}");

            // If we don't have specific context form, use overall form
            if (isHome && homeForm.Length == 0 && overallForm.Length > 0)
            {
                Console.WriteLine($"Home team {team.Name} has no home form data, using overall form");
                homeForm = new StringBuilder(overallForm.ToString());
            }
            else if (!isHome && awayForm.Length == 0 && overallForm.Length > 0)
            {
                Console.WriteLine($"Away team {team.Name} has no away form data, using overall form");
                awayForm = new StringBuilder(overallForm.ToString());
            }

            // If we still have no form data, check if we can use the scoring/conceding model
            if ((isHome && homeForm.Length == 0) || (!isHome && awayForm.Length == 0))
            {
                Console.WriteLine($"WARNING: No form data available for team {team.Name}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing match data for team {team.Name}: {ex.Message}");
        }

        // Calculate average goals with our processed data or fall back to scoring conceding model
        double? avgHomeGoals = null;
        double? avgAwayGoals = null;
        double? avgTotalGoals = null;

        // First try to use our calculated data
        if (totalHomeMatches > 0)
        {
            avgHomeGoals = Math.Round(totalHomeGoalsScored / totalHomeMatches, 2);
            Console.WriteLine($"Team {team.Name} calculated avg home goals: {avgHomeGoals} from {totalHomeMatches} matches");
        }

        if (totalAwayMatches > 0)
        {
            avgAwayGoals = Math.Round(totalAwayGoalsScored / totalAwayMatches, 2);
            Console.WriteLine($"Team {team.Name} calculated avg away goals: {avgAwayGoals} from {totalAwayMatches} matches");
        }

        if (totalHomeMatches + totalAwayMatches > 0)
        {
            avgTotalGoals = Math.Round((totalHomeGoalsScored + totalAwayGoalsScored) / (totalHomeMatches + totalAwayMatches), 2);
            Console.WriteLine($"Team {team.Name} calculated avg total goals: {avgTotalGoals} from {totalHomeMatches + totalAwayMatches} matches");
        }

        // Fall back to scoring conceding model if needed
        if (avgHomeGoals == null && scoringConceding?.Stats?.Scoring?.GoalsScoredAverage?.Home != null)
        {
            avgHomeGoals = scoringConceding.Stats.Scoring.GoalsScoredAverage.Home;
            Console.WriteLine($"Team {team.Name} using model avg home goals: {avgHomeGoals}");
        }

        if (avgAwayGoals == null && scoringConceding?.Stats?.Scoring?.GoalsScoredAverage?.Away != null)
        {
            avgAwayGoals = scoringConceding.Stats.Scoring.GoalsScoredAverage.Away;
            Console.WriteLine($"Team {team.Name} using model avg away goals: {avgAwayGoals}");
        }

        if (avgTotalGoals == null && scoringConceding?.Stats?.Scoring?.GoalsScoredAverage?.Total != null)
        {
            avgTotalGoals = scoringConceding.Stats.Scoring.GoalsScoredAverage.Total;
            Console.WriteLine($"Team {team.Name} using model avg total goals: {avgTotalGoals}");
        }

        // For primary form display, use context-appropriate form
        string primaryForm = isHome
            ? (homeForm.Length > 0 ? homeForm.ToString() : overallForm.ToString())
            : (awayForm.Length > 0 ? awayForm.ToString() : overallForm.ToString());

        if (string.IsNullOrEmpty(primaryForm))
        {
            Console.WriteLine($"WARNING: No form data available for team {team.Name}");
            primaryForm = "-"; // Default to indicate no data
        }

        return new TeamData
        {
            Name = team.Name ?? "Unknown Team",
            Position = teamPosition,
            Logo = logoUrl,
            AvgHomeGoals = avgHomeGoals,
            AvgAwayGoals = avgAwayGoals,
            AvgTotalGoals = avgTotalGoals,
            HomeMatchesOver15 = homeMatchesOver15,
            AwayMatchesOver15 = awayMatchesOver15,
            TotalHomeMatches = totalHomeMatches,
            TotalAwayMatches = totalAwayMatches,
            Form = primaryForm,
            HomeForm = homeForm.ToString(),
            AwayForm = awayForm.ToString(),
            CleanSheets = cleanSheets,
            HomeCleanSheets = homeCleanSheets,
            AwayCleanSheets = awayCleanSheets,
            ScoringFirstWinRate = firstGoalTotal > 0 ? (firstGoalWins * 100 / firstGoalTotal) : (int?)null,
            ConcedingFirstWinRate = concededFirstTotal > 0 ? (concededFirstWins * 100 / concededFirstTotal) : (int?)null,
            FirstHalfGoalsPercent = totalGoals > 0 ? (firstHalfGoals * 100 / totalGoals) : (int?)null,
            SecondHalfGoalsPercent = totalGoals > 0 ? (secondHalfGoals * 100 / totalGoals) : (int?)null,
            AvgCorners = lastMatches.Count > 0 ? cornerSum / lastMatches.Count : (double?)null,
            BttsRate = lastMatches.Count > 0 ? (bttsCount * 100 / lastMatches.Count) : (int?)null,
            HomeBttsRate = totalHomeMatches > 0 ? (homeBttsCount * 100 / totalHomeMatches) : (int?)null,
            AwayBttsRate = totalAwayMatches > 0 ? (awayBttsCount * 100 / totalAwayMatches) : (int?)null,
            LateGoalRate = lastMatches.Count > 0 ? (lateGoalCount * 100 / lastMatches.Count) : (int?)null,
            GoalDistribution = goalDistribution,
            AgainstTopTeamsPoints = null,
            AgainstMidTeamsPoints = null,
            AgainstBottomTeamsPoints = null
        };
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
                match.OriginalMatch.Teams.Home,
                match.TeamTableSlice,
                match.LastXStatsTeam1,
                match.Team1LastX,
                match.Team1ScoringConceding,
                true
            );

            var awayTeamData = ExtractTeamData(
                match.OriginalMatch.Teams.Away,
                match.TeamTableSlice,
                match.LastXStatsTeam2,
                match.Team2LastX,
                match.Team2ScoringConceding,
                false
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
                int homeWins = homeTeamData.Form.Count(c => c == 'W');
                int homeDraws = homeTeamData.Form.Count(c => c == 'D');
                int homeLosses = homeTeamData.Form.Count(c => c == 'L');

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
                int awayWins = awayTeamData.Form.Count(c => c == 'W');
                int awayDraws = awayTeamData.Form.Count(c => c == 'D');
                int awayLosses = awayTeamData.Form.Count(c => c == 'L');

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
                    .OrderBy(r =>
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
}