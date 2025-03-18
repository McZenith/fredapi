using System.Text;
using fredapi.Database;
using fredapi.SportRadarService.Background;
using MongoDB.Driver;
using MarketData = fredapi.SportRadarService.Background.ArbitrageLiveMatchBackgroundService.MarketData;

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
        try
        {
            var collection = mongoDbService.GetCollection<EnrichedSportMatch>("EnrichedSportMatches");

            // Get only valid matches with complete data
            var filter = Builders<EnrichedSportMatch>.Filter.Eq(m => m.IsValid, true) &
                         Builders<EnrichedSportMatch>.Filter.Ne(m => m.TeamTableSlice, null) &
                         Builders<EnrichedSportMatch>.Filter.Ne(m => m.LastXStatsTeam1, null) &
                         Builders<EnrichedSportMatch>.Filter.Ne(m => m.LastXStatsTeam2, null) &
                         Builders<EnrichedSportMatch>.Filter.Ne(m => m.TeamVersusRecent, null) &
                         Builders<EnrichedSportMatch>.Filter.Ne(m => m.Team1LastX, null) &
                         Builders<EnrichedSportMatch>.Filter.Ne(m => m.Team2LastX, null);

            var matches = await collection.Find(filter)
                .SortByDescending(m => m.MatchTime)
                .ToListAsync();

            // Group by tournament/league for metadata
            var leagueMetadata = matches.GroupBy(m => m.OriginalMatch?.TournamentName ?? "Unknown Tournament")
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
                UpcomingMatches = matches.Select(TransformToPredictiveData).ToList(),
                Metadata = new MetadataInfo
                {
                    Total = matches.Count,
                    Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                    LeagueData = leagueMetadata
                }
            };

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
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

        var matchesWithMatchStats = matches
            .Where(m => m.TeamVersusRecent?.Matches != null && m.TeamVersusRecent.Matches.Any())
            .ToList();

        if (!matchesWithMatchStats.Any())
            return 0;

        int totalGoals = 0;
        int totalMatches = 0;

        foreach (var match in matchesWithMatchStats)
        {
            foreach (var pastMatch in match.TeamVersusRecent.Matches)
            {
                if (pastMatch.Result?.Home != null && pastMatch.Result.Away != null)
                {
                    totalGoals += (pastMatch.Result.Home ?? 0) + (pastMatch.Result.Away ?? 0);
                    totalMatches++;
                }
            }
        }

        return totalMatches > 0 ? totalGoals / (double)totalMatches : 0;
    }

    private static int CalculateHomeWinRateForLeague(List<EnrichedSportMatch> matches)
    {
        if (matches == null || !matches.Any())
            return 0;

        var matchesWithHistory = matches
            .Where(m => m.TeamVersusRecent?.Matches != null && m.TeamVersusRecent.Matches.Any())
            .ToList();

        if (!matchesWithHistory.Any())
            return 0;

        int homeWins = 0;
        int totalMatches = 0;

        foreach (var match in matchesWithHistory)
        {
            foreach (var pastMatch in match.TeamVersusRecent.Matches)
            {
                if (pastMatch.Result != null)
                {
                    totalMatches++;
                    if (pastMatch.Result.Winner == "home")
                        homeWins++;
                }
            }
        }

        return totalMatches > 0 ? (int)(homeWins * 100.0 / totalMatches) : 0;
    }

    private static int CalculateDrawRateForLeague(List<EnrichedSportMatch> matches)
    {
        if (matches == null || !matches.Any())
            return 0;

        var matchesWithHistory = matches
            .Where(m => m.TeamVersusRecent?.Matches != null && m.TeamVersusRecent.Matches.Any())
            .ToList();

        if (!matchesWithHistory.Any())
            return 0;

        int draws = 0;
        int totalMatches = 0;

        foreach (var match in matchesWithHistory)
        {
            foreach (var pastMatch in match.TeamVersusRecent.Matches)
            {
                if (pastMatch.Result != null)
                {
                    totalMatches++;
                    if (pastMatch.Result.Winner == null)
                        draws++;
                }
            }
        }

        return totalMatches > 0 ? (int)(draws * 100.0 / totalMatches) : 0;
    }

    private static int CalculateAwayWinRateForLeague(List<EnrichedSportMatch> matches)
    {
        if (matches == null || !matches.Any())
            return 0;

        var matchesWithHistory = matches
            .Where(m => m.TeamVersusRecent?.Matches != null && m.TeamVersusRecent.Matches.Any())
            .ToList();

        if (!matchesWithHistory.Any())
            return 0;

        int awayWins = 0;
        int totalMatches = 0;

        foreach (var match in matchesWithHistory)
        {
            foreach (var pastMatch in match.TeamVersusRecent.Matches)
            {
                if (pastMatch.Result != null)
                {
                    totalMatches++;
                    if (pastMatch.Result.Winner == "away")
                        awayWins++;
                }
            }
        }

        return totalMatches > 0 ? (int)(awayWins * 100.0 / totalMatches) : 0;
    }

    private static int CalculateBttsRateForLeague(List<EnrichedSportMatch> matches)
    {
        if (matches == null || !matches.Any())
            return 0;

        var matchesWithHistory = matches
            .Where(m => m.TeamVersusRecent?.Matches != null && m.TeamVersusRecent.Matches.Any())
            .ToList();

        if (!matchesWithHistory.Any())
            return 0;

        int bttsMatches = 0;
        int totalMatches = 0;

        foreach (var match in matchesWithHistory)
        {
            foreach (var pastMatch in match.TeamVersusRecent.Matches)
            {
                if (pastMatch.Result?.Home != null && pastMatch.Result.Away != null)
                {
                    totalMatches++;
                    if (pastMatch.Result.Home > 0 && pastMatch.Result.Away > 0)
                        bttsMatches++;
                }
            }
        }

        return totalMatches > 0 ? (int)(bttsMatches * 100.0 / totalMatches) : 0;
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
            PositionGap = CalculatePositionGap(match.TeamTableSlice),
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

    private static TeamData ExtractTeamData(
        SportTeam team,
        TeamTableSliceModel tableSlice,
        TeamLastXStatsModel lastXStats,
        TeamLastXExtendedModel lastXExtended,
        TeamScoringConcedingModel scoringConceding,
        bool isHome)
    {
        if (team == null)
            return new TeamData();

        // Find team's position in table
        var teamPosition = 0;
        var tableRow = tableSlice?.TableRows?.FirstOrDefault(r => r.Team?.Id == team.Id);
        if (tableRow != null)
        {
            teamPosition = tableRow.Position;
        }

        // Calculate form stats
        var lastMatches = lastXExtended?.Matches ?? new List<ExtendedMatchStat>();
        var recentForm = new StringBuilder();
        var homeForm = new StringBuilder();
        var awayForm = new StringBuilder();

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

        var cornerSum = 0.0;
        var goalDistribution = new Dictionary<string, double>();

        foreach (var match in lastMatches)
        {
            // Determine if this team is home or away in this match
            bool isTeamHome = match.Teams?.Home?.Id == lastXExtended?.Team?.Id;
            var teamScore = isTeamHome ? match.Result?.Home : match.Result?.Away;
            var opponentScore = isTeamHome ? match.Result?.Away : match.Result?.Home;

            // Form calculation
            if (match.Result?.Winner == (isTeamHome ? "home" : "away"))
            {
                if (recentForm.Length < 5) recentForm.Append('W');
                if (isTeamHome && homeForm.Length < 5) homeForm.Append('W');
                else if (!isTeamHome && awayForm.Length < 5) awayForm.Append('W');
            }
            else if (match.Result?.Winner == (isTeamHome ? "away" : "home"))
            {
                if (recentForm.Length < 5) recentForm.Append('L');
                if (isTeamHome && homeForm.Length < 5) homeForm.Append('L');
                else if (!isTeamHome && awayForm.Length < 5) awayForm.Append('L');
            }
            else
            {
                if (recentForm.Length < 5) recentForm.Append('D');
                if (isTeamHome && homeForm.Length < 5) homeForm.Append('D');
                else if (!isTeamHome && awayForm.Length < 5) awayForm.Append('D');
            }

            // Count over 1.5 goals games
            if (isTeamHome)
            {
                totalHomeMatches++;
                if ((teamScore ?? 0) + (opponentScore ?? 0) > 1)
                    homeMatchesOver15++;
                if ((opponentScore ?? 0) == 0)
                    homeCleanSheets++;
                if ((teamScore ?? 0) > 0 && (opponentScore ?? 0) > 0)
                    homeBttsCount++;
            }
            else
            {
                totalAwayMatches++;
                if ((teamScore ?? 0) + (opponentScore ?? 0) > 1)
                    awayMatchesOver15++;
                if ((opponentScore ?? 0) == 0)
                    awayCleanSheets++;
                if ((teamScore ?? 0) > 0 && (opponentScore ?? 0) > 0)
                    awayBttsCount++;
            }

            // Clean sheets
            if ((opponentScore ?? 0) == 0)
                cleanSheets++;

            // Both teams scored
            if ((teamScore ?? 0) > 0 && (opponentScore ?? 0) > 0)
                bttsCount++;

            // First goal stats
            if (match.FirstGoal == (isTeamHome ? "home" : "away"))
            {
                firstGoalTotal++;
                if (match.Result?.Winner == (isTeamHome ? "home" : "away"))
                    firstGoalWins++;
            }

            if (match.FirstGoal == (isTeamHome ? "away" : "home"))
            {
                concededFirstTotal++;
                if (match.Result?.Winner == (isTeamHome ? "home" : "away"))
                    concededFirstWins++;
            }

            // Late goals
            if (match.LastGoal == (isTeamHome ? "home" : "away"))
                lateGoalCount++;

            // Corner stats
            cornerSum += isTeamHome ? (match.Corners?.Home ?? 0) : (match.Corners?.Away ?? 0);

            // Goal time distribution could be calculated here if available in the data
            // For now we'll leave the distribution empty
        }

        return new TeamData
        {
            Name = team.Name,
            Position = teamPosition,
            Logo = team.Id, // Use team ID instead of hardcoded logos
            AvgHomeGoals = scoringConceding?.Stats?.Scoring?.GoalsScoredAverage?.Home,
            AvgAwayGoals = scoringConceding?.Stats?.Scoring?.GoalsScoredAverage?.Away,
            AvgTotalGoals = scoringConceding?.Stats?.Scoring?.GoalsScoredAverage?.Total,
            HomeMatchesOver15 = homeMatchesOver15,
            AwayMatchesOver15 = awayMatchesOver15,
            TotalHomeMatches = totalHomeMatches,
            TotalAwayMatches = totalAwayMatches,
            Form = recentForm.ToString(),
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

    private static int CalculatePositionGap(TeamTableSliceModel tableSlice)
    {
        if (tableSlice?.TableRows == null || tableSlice.TableRows.Count < 2)
            return 0;

        var homePosition = 0;
        var awayPosition = 0;

        // Find home and away positions from table rows
        foreach (var row in tableSlice.TableRows)
        {
            if (row.Team?.Id == tableSlice.TableRows[0].Team?.Id)
                homePosition = row.Position;
            else if (row.Team?.Id == tableSlice.TableRows[1].Team?.Id)
                awayPosition = row.Position;
        }

        return Math.Abs(homePosition - awayPosition);
    }

    private static string DetermineFavorite(List<MarketData> markets)
    {
        // Look for 1X2 market to determine favorite
        var x12Market = markets?.FirstOrDefault(m => m.Name == "1X2");
        if (x12Market != null)
        {
            var homeOdds = x12Market.Outcomes?.FirstOrDefault(o => o.Desc == "Home")?.Odds;
            var awayOdds = x12Market.Outcomes?.FirstOrDefault(o => o.Desc == "Away")?.Odds;

            if (homeOdds != null && awayOdds != null)
            {
                double homeOddsValue = double.Parse(homeOdds);
                double awayOddsValue = double.Parse(awayOdds);

                if (homeOddsValue < awayOddsValue)
                    return "home";
                else if (awayOddsValue < homeOddsValue)
                    return "away";
            }
        }

        return "draw";
    }

    private static int? CalculateConfidenceScore(EnrichedSportMatch match)
    {
        if (match.Markets == null || !match.Markets.Any() ||
            match.TeamVersusRecent?.Matches == null || !match.TeamVersusRecent.Matches.Any() ||
            match.Team1LastX?.Matches == null || !match.Team1LastX.Matches.Any() ||
            match.Team2LastX?.Matches == null || !match.Team2LastX.Matches.Any())
        {
            return null;
        }

        // Get favorite from odds
        var homeOdds = GetOddsValue(match.Markets?.FirstOrDefault(m => m.Name == "1X2")?.Outcomes
                    ?.FirstOrDefault(o => o.Desc == "Home")?.Odds);
        var awayOdds = GetOddsValue(match.Markets?.FirstOrDefault(m => m.Name == "1X2")?.Outcomes
                    ?.FirstOrDefault(o => o.Desc == "Away")?.Odds);

        // Calculate different factors for confidence
        var oddsRatio = homeOdds > 0 && awayOdds > 0
            ? Math.Min(homeOdds, awayOdds) / Math.Max(homeOdds, awayOdds)
            : 0.5;
        var oddsConfidence = (int)(100 * (1 - oddsRatio)); // Higher difference = higher confidence

        // Head-to-head analysis
        var h2h = ExtractHeadToHead(match.TeamVersusRecent, match.OriginalMatch?.Teams?.Home, match.OriginalMatch?.Teams?.Away);
        var h2hWeight = 0;
        if (h2h.Matches > 0)
        {
            var winRatio = (double)Math.Max(h2h.Wins, h2h.Losses) / h2h.Matches;
            h2hWeight = (int)(winRatio * 25); // Max 25 points from h2h
        }

        // Recent form analysis
        var team1Form = match.Team1LastX.Matches.Take(5)
            .Count(m => m.Result?.Winner == (m.Teams?.Home?.Id == match.Team1LastX.Team?.Id ? "home" : "away"));
        var team2Form = match.Team2LastX.Matches.Take(5)
            .Count(m => m.Result?.Winner == (m.Teams?.Home?.Id == match.Team2LastX.Team?.Id ? "home" : "away"));
        var formDifference = Math.Abs(team1Form - team2Form);
        var formConfidence = formDifference * 5; // Each win difference adds 5 points

        // Position gap analysis
        var positionGap = CalculatePositionGap(match.TeamTableSlice);
        var positionConfidence = Math.Min(positionGap * 3, 20); // Max 20 points from position

        // Combine factors with weights
        var totalConfidence = (int)(oddsConfidence * 0.4 + h2hWeight * 0.2 + formConfidence * 0.3 + positionConfidence * 0.1);

        // Ensure value is between 0-100
        return Math.Max(0, Math.Min(100, totalConfidence));
    }

    private static double? CalculateExpectedGoals(EnrichedSportMatch match)
    {
        if (match.Markets == null || !match.Markets.Any() ||
            match.Team1LastX?.Matches == null || !match.Team1LastX.Matches.Any() ||
            match.Team2LastX?.Matches == null || !match.Team2LastX.Matches.Any())
        {
            return null;
        }

        // Use odds from over/under markets to estimate expected goals
        var over25Market = match.Markets.FirstOrDefault(m =>
            m.Name == "Over/Under" && m.Specifier == "total=2.5");
        var over15Market = match.Markets.FirstOrDefault(m =>
            m.Name == "Over/Under" && m.Specifier == "total=1.5");

        double expectedGoals = 0;
        double weight = 0;

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
            }
        }

        // If we couldn't get data from odds, use historical data
        if (weight < 0.5)
        {
            // Calculate average goals from recent matches
            var homeTeamHomeGoals = match.Team1LastX.Matches
                .Where(m => m.Teams?.Home?.Id == match.Team1LastX.Team?.Id)
                .Take(5)
                .Select(m => (m.Result?.Home ?? 0) + (m.Result?.Away ?? 0))
                .DefaultIfEmpty(0)
                .Average();

            var awayTeamAwayGoals = match.Team2LastX.Matches
                .Where(m => m.Teams?.Away?.Id == match.Team2LastX.Team?.Id)
                .Take(5)
                .Select(m => (m.Result?.Home ?? 0) + (m.Result?.Away ?? 0))
                .DefaultIfEmpty(0)
                .Average();

            expectedGoals = (homeTeamHomeGoals + awayTeamAwayGoals) / 2;
            weight = 1;
        }

        return weight > 0 ? Math.Round(expectedGoals, 2) : null;
    }

    private static double? CalculateDefensiveStrength(EnrichedSportMatch match)
    {
        if (match.Team1LastX?.Matches == null || !match.Team1LastX.Matches.Any() ||
            match.Team2LastX?.Matches == null || !match.Team2LastX.Matches.Any())
        {
            return null;
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
        return 0.0;
    }

    private static HeadToHeadData ExtractHeadToHead(
        TeamVersusRecentModel teamVersus,
        SportTeam homeTeam,
        SportTeam awayTeam)
    {
        var h2h = new HeadToHeadData();

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

        foreach (var match in teamVersus.Matches)
        {
            bool isHomeTeamPlayingHome = match.Teams?.Home?.Id == homeTeam.Id;
            var homeTeamScore = isHomeTeamPlayingHome ? match.Result?.Home : match.Result?.Away;
            var awayTeamScore = isHomeTeamPlayingHome ? match.Result?.Away : match.Result?.Home;

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

    private static CornerStatsData ExtractCornerStats(TeamLastXExtendedModel team1LastX, TeamLastXExtendedModel team2LastX)
    {
        var stats = new CornerStatsData();

        if (team1LastX?.Matches == null || team2LastX?.Matches == null)
            return stats;

        var homeCorners = team1LastX.Matches
            .Where(m => m.Teams?.Home?.Id == team1LastX.Team?.Id)
            .Select(m => m.Corners?.Home ?? 0)
            .ToList();

        var awayCorners = team2LastX.Matches
            .Where(m => m.Teams?.Away?.Id == team2LastX.Team?.Id)
            .Select(m => m.Corners?.Away ?? 0)
            .ToList();

        stats.HomeAvg = homeCorners.Count > 0 ? homeCorners.Average() : 0;
        stats.AwayAvg = awayCorners.Count > 0 ? awayCorners.Average() : 0;
        stats.TotalAvg = stats.HomeAvg + stats.AwayAvg;

        return stats;
    }

    private static ScoringPatternsData ExtractScoringPatterns(TeamLastXExtendedModel team1LastX, TeamLastXExtendedModel team2LastX)
    {
        var patterns = new ScoringPatternsData();

        if (team1LastX?.Matches == null || team2LastX?.Matches == null)
            return patterns;

        int homeFirstCount = 0;
        int homeTotalMatches = 0;
        int awayFirstCount = 0;
        int awayTotalMatches = 0;
        int homeLateCount = 0;
        int awayLateCount = 0;

        foreach (var match in team1LastX.Matches)
        {
            bool isHome = match.Teams?.Home?.Id == team1LastX.Team?.Id;
            if (isHome)
            {
                homeTotalMatches++;
                if (match.FirstGoal == "home")
                    homeFirstCount++;
                if (match.LastGoal == "home")
                    homeLateCount++;
            }
        }

        foreach (var match in team2LastX.Matches)
        {
            bool isAway = match.Teams?.Away?.Id == team2LastX.Team?.Id;
            if (isAway)
            {
                awayTotalMatches++;
                if (match.FirstGoal == "away")
                    awayFirstCount++;
                if (match.LastGoal == "away")
                    awayLateCount++;
            }
        }

        patterns.HomeFirstGoalRate = homeTotalMatches > 0 ? homeFirstCount * 100 / homeTotalMatches : 0;
        patterns.AwayFirstGoalRate = awayTotalMatches > 0 ? awayFirstCount * 100 / awayTotalMatches : 0;
        patterns.HomeLateGoalRate = homeTotalMatches > 0 ? homeLateCount * 100 / homeTotalMatches : 0;
        patterns.AwayLateGoalRate = awayTotalMatches > 0 ? awayLateCount * 100 / awayTotalMatches : 0;

        return patterns;
    }

    private static List<string> GeneratePredictionReasons(EnrichedSportMatch match)
    {
        var reasons = new List<string>();

        if (match.OriginalMatch?.Teams?.Home == null || match.OriginalMatch?.Teams?.Away == null)
            return reasons;

        var homeTeam = match.OriginalMatch.Teams.Home.Name;
        var awayTeam = match.OriginalMatch.Teams.Away.Name;

        // Only add reasons based on actual available data
        if (match.Team1LastX?.Matches != null && match.Team1LastX.Matches.Any())
        {
            var homeFormData = ExtractTeamData(match.OriginalMatch.Teams.Home, match.TeamTableSlice,
                match.LastXStatsTeam1, match.Team1LastX, match.Team1ScoringConceding, true);

            if (!string.IsNullOrEmpty(homeFormData.HomeForm))
                reasons.Add($"{homeTeam} home form: {homeFormData.HomeForm}");
        }

        if (match.Team2LastX?.Matches != null && match.Team2LastX.Matches.Any())
        {
            var awayFormData = ExtractTeamData(match.OriginalMatch.Teams.Away, match.TeamTableSlice,
                match.LastXStatsTeam2, match.Team2LastX, match.Team2ScoringConceding, false);

            if (!string.IsNullOrEmpty(awayFormData.AwayForm))
                reasons.Add($"{awayTeam} away form: {awayFormData.AwayForm}");
        }

        // Head-to-Head
        if (match.TeamVersusRecent?.Matches != null && match.TeamVersusRecent.Matches.Any())
        {
            var h2h = ExtractHeadToHead(match.TeamVersusRecent, match.OriginalMatch.Teams.Home, match.OriginalMatch.Teams.Away);
            reasons.Add($"H2H: {homeTeam} won {h2h.Wins} of {h2h.Matches} matches against {awayTeam}");
        }

        // Odds info if available
        var favorite = DetermineFavorite(match.Markets);
        var odds = ExtractOdds(match.Markets);
        if (odds.HomeWin > 0 && odds.AwayWin > 0)
        {
            reasons.Add($"Odds: Home {odds.HomeWin}, Away {odds.AwayWin}");
        }

        return reasons;
    }

    private static double? CalculateAverageGoals(EnrichedSportMatch match)
    {
        // Calculate from actual data or return null
        if (match.Team1LastX?.Matches == null || match.Team2LastX?.Matches == null)
            return null;

        var homeTeamGoals = match.Team1LastX.Matches.Sum(m =>
            m.Teams?.Home?.Id == match.Team1LastX.Team?.Id ?
            (m.Result?.Home ?? 0) : (m.Result?.Away ?? 0));

        var awayTeamGoals = match.Team2LastX.Matches.Sum(m =>
            m.Teams?.Away?.Id == match.Team2LastX.Team?.Id ?
            (m.Result?.Away ?? 0) : (m.Result?.Home ?? 0));

        var totalMatches = match.Team1LastX.Matches.Count + match.Team2LastX.Matches.Count;

        return totalMatches > 0 ? (homeTeamGoals + awayTeamGoals) / (double)totalMatches : null;
    }
}