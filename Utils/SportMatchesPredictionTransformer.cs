using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Globalization;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Diagnostics;
using fredapi.SportRadarService.Background;
namespace fredapi.SportRadarService.Transformers
{
    /// <summary>
    /// Production-ready transformer that converts EnrichedSportMatch objects from MongoDB 
    /// into the PredictionDataResponse format for client consumption.
    /// </summary>
    public class SportMatchesPredictionTransformer
    {
        private readonly ILogger<SportMatchesPredictionTransformer> _logger;

        public SportMatchesPredictionTransformer(ILogger<SportMatchesPredictionTransformer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Transforms a list of EnrichedSportMatch objects to the prediction data format.
        /// </summary>
        /// <param name="sportMatches">List of EnrichedSportMatch objects from the database</param>
        /// <returns>PredictionDataResponse object that can be serialized to predict-data.json format</returns>
        public PredictionDataResponse TransformToPredictionData(List<Background.EnrichedSportMatch> sportMatches)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                _logger.LogInformation($"Starting transformation of {sportMatches?.Count ?? 0} sport matches");

                if (sportMatches == null || !sportMatches.Any())
                {
                    _logger.LogWarning("No sport matches provided for transformation");
                    return CreateEmptyPredictionResponse();
                }

                // Prepare the result object
                var result = new PredictionDataResponse
                {
                    Data = new PredictionData
                    {
                        UpcomingMatches = new List<UpcomingMatch>(),
                        Metadata = new PredictionMetadata
                        {
                            Total = sportMatches.Count,
                            Date = DateTime.Now.ToString("yyyy-MM-dd"),
                            LeagueData = new Dictionary<string, LeagueMetadata>()
                        }
                    },
                    Pagination = new PaginationInfo
                    {
                        CurrentPage = 1,
                        TotalPages = sportMatches.Count > 0 ? 1 : 0,
                        PageSize = sportMatches.Count,
                        TotalItems = sportMatches.Count,
                        HasNext = false,
                        HasPrevious = false
                    }
                };

                // Process each match and transform to prediction format
                int successCount = 0;
                int skipCount = 0;
                int errorCount = 0;

                foreach (var sportMatch in sportMatches)
                {
                    try
                    {
                        if (!IsValidMatch(sportMatch))
                        {
                            _logger.LogWarning($"Skipping invalid match with ID: {sportMatch?.MatchId ?? "unknown"}");
                            skipCount++;
                            continue;
                        }

                        var upcomingMatch = TransformSingleMatch(sportMatch);
                        if (upcomingMatch != null)
                        {
                            result.Data.UpcomingMatches.Add(upcomingMatch);
                            UpdateLeagueMetadata(result.Data.Metadata, upcomingMatch);
                            successCount++;
                        }
                        else
                        {
                            _logger.LogWarning($"Failed to create upcoming match data for match {sportMatch?.MatchId}");
                            skipCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error transforming match {sportMatch?.MatchId ?? "unknown"}: {ex.Message}");
                        errorCount++;
                    }
                }

                stopwatch.Stop();
                _logger.LogInformation($"Transformation completed in {stopwatch.ElapsedMilliseconds}ms. " +
                    $"Success: {successCount}, Skipped: {skipCount}, Errors: {errorCount}");
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, $"Fatal error during match transformation: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Validates that a match contains all required fields
        /// </summary>
        private bool IsValidMatch(Background.EnrichedSportMatch sportMatch)
        {
            if (sportMatch == null)
                return false;

            if (sportMatch.OriginalMatch == null)
            {
                _logger.LogWarning($"Match {sportMatch.MatchId} has null OriginalMatch");
                return false;
            }

            if (string.IsNullOrEmpty(sportMatch.MatchId))
            {
                _logger.LogWarning("Match has null or empty MatchId");
                return false;
            }

            if (sportMatch.OriginalMatch.Teams == null)
            {
                _logger.LogWarning($"Match {sportMatch.MatchId} has null Teams");
                return false;
            }

            if (sportMatch.OriginalMatch.Teams.Home == null || sportMatch.OriginalMatch.Teams.Away == null)
            {
                _logger.LogWarning($"Match {sportMatch.MatchId} has null Home or Away team");
                return false;
            }

            if (string.IsNullOrEmpty(sportMatch.OriginalMatch.Teams.Home.Name) ||
                string.IsNullOrEmpty(sportMatch.OriginalMatch.Teams.Away.Name))
            {
                _logger.LogWarning($"Match {sportMatch.MatchId} has null or empty team names");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Creates an empty response object for when no matches are available
        /// </summary>
        private PredictionDataResponse CreateEmptyPredictionResponse()
        {
            return new PredictionDataResponse
            {
                Data = new PredictionData
                {
                    UpcomingMatches = new List<UpcomingMatch>(),
                    Metadata = new PredictionMetadata
                    {
                        Total = 0,
                        Date = DateTime.Now.ToString("yyyy-MM-dd"),
                        LeagueData = new Dictionary<string, LeagueMetadata>()
                    }
                },
                Pagination = new PaginationInfo
                {
                    CurrentPage = 1,
                    TotalPages = 0,
                    PageSize = 0,
                    TotalItems = 0,
                    HasNext = false,
                    HasPrevious = false
                }
            };
        }

        /// <summary>
        /// Transforms a single match into the UpcomingMatch format
        /// </summary>
        private UpcomingMatch TransformSingleMatch(Background.EnrichedSportMatch sportMatch)
        {
            try
            {
                // Parse match ID
                if (!int.TryParse(sportMatch.MatchId, out int matchId))
                {
                    _logger.LogWarning($"Unable to parse match ID '{sportMatch.MatchId}' as integer");
                    // Use a hash code as fallback ID
                    matchId = sportMatch.MatchId.GetHashCode();
                }

                // Extract match date and time
                DateTime matchTime = sportMatch.MatchTime;
                if (matchTime == DateTime.MinValue)
                {
                    matchTime = DateTime.Now.AddDays(1);
                    _logger.LogWarning($"Match {sportMatch.MatchId} has invalid date, using default: {matchTime}");
                }

                // Get venue (tournament name)
                string venue = !string.IsNullOrEmpty(sportMatch.OriginalMatch.TournamentName)
                    ? sportMatch.OriginalMatch.TournamentName
                    : "Unknown";

                // Extract odds information
                var oddsInfo = ExtractOddsInfo(sportMatch.OriginalMatch.Markets);

                // Create home and away team data
                var homeTeam = CreateTeamData(sportMatch, true);
                var awayTeam = CreateTeamData(sportMatch, false);

                // Create head-to-head data with additional error checking
                var headToHead = CreateHeadToHeadData(sportMatch);

                // Calculate position gap between teams
                int homePos = GetTeamPosition(sportMatch, homeTeam.Name);
                int awayPos = GetTeamPosition(sportMatch, awayTeam.Name);
                int positionGap = Math.Abs(homePos - awayPos);

                // Determine favorite team based on odds
                string favorite = DetermineFavorite(oddsInfo);

                // Calculate confidence score
                int confidenceScore = CalculateConfidenceScore(sportMatch, homeTeam, awayTeam, oddsInfo);

                // Calculate expected goals
                double expectedGoals = CalculateExpectedGoals(sportMatch, homeTeam, awayTeam);

                // Generate detailed scoring patterns
                var scoringPatterns = CreateScoringPatterns(sportMatch, homeTeam, awayTeam);

                // Create corner stats with available data
                var cornerStats = CreateCornerStats(sportMatch);

                // Generate reasons for prediction
                var reasons = GeneratePredictionReasons(sportMatch, homeTeam, awayTeam, oddsInfo, expectedGoals);

                return new UpcomingMatch
                {
                    Id = matchId,
                    Date = matchTime.ToString("yyyy-MM-dd"),
                    Time = matchTime.ToString("HH:mm"),
                    Venue = venue,
                    HomeTeam = homeTeam,
                    AwayTeam = awayTeam,
                    PositionGap = positionGap,
                    Favorite = favorite,
                    ConfidenceScore = confidenceScore,
                    AverageGoals = CalculateAverageGoals(sportMatch, homeTeam, awayTeam),
                    ExpectedGoals = Math.Round(expectedGoals, 2),
                    DefensiveStrength = CalculateDefensiveStrength(sportMatch, homeTeam, awayTeam),
                    Odds = oddsInfo,
                    HeadToHead = headToHead,
                    CornerStats = cornerStats,
                    ScoringPatterns = scoringPatterns,
                    ReasonsForPrediction = reasons
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating upcoming match data for match {sportMatch?.MatchId}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates team data with stats, form, and position information
        /// </summary>
        private TeamData CreateTeamData(Background.EnrichedSportMatch sportMatch, bool isHome)
        {
            try
            {
                var teamInfo = isHome ? sportMatch.OriginalMatch.Teams.Home : sportMatch.OriginalMatch.Teams.Away;
                if (teamInfo == null)
                {
                    throw new ArgumentNullException(nameof(teamInfo), $"Team info is null for {(isHome ? "home" : "away")} team");
                }

                var teamName = teamInfo.Name;
                if (string.IsNullOrEmpty(teamName))
                {
                    throw new ArgumentException($"{(isHome ? "Home" : "Away")} team name is null or empty");
                }

                // Get team position in the table
                int position = GetTeamPosition(sportMatch, teamName);

                // Extract team logo (left empty in example JSON)
                string logo = "";

                // Extract team form with robust handling
                string form = ExtractTeamForm(sportMatch, isHome);

                // Get team statistics
                var teamStats = isHome
                    ? sportMatch.Team1ScoringConceding?.Stats
                    : sportMatch.Team2ScoringConceding?.Stats;

                // Calculate team metrics with safety
                double avgHomeGoals = 0;
                double avgAwayGoals = 0;
                double avgTotalGoals = 0;
                double cleanSheetPercentage = 0;
                int totalHomeMatches = 0;
                int totalAwayMatches = 0;
                int cleanSheets = 0;
                int homeCleanSheets = 0;
                int awayCleanSheets = 0;
                double averageGoalsConceded = 0;
                double homeAverageGoalsConceded = 0;
                double awayAverageGoalsConceded = 0;

                // Safely extract team stats if available
                if (teamStats != null)
                {
                    // Total match counts
                    totalHomeMatches = teamStats.TotalMatches?.Home ?? 0;
                    totalAwayMatches = teamStats.TotalMatches?.Away ?? 0;

                    // Goals statistics
                    avgHomeGoals = isHome
                        ? SafeDivide(teamStats.Scoring?.GoalsScored?.Home ?? 0, teamStats.TotalMatches?.Home ?? 1)
                        : 0;

                    avgAwayGoals = !isHome
                        ? SafeDivide(teamStats.Scoring?.GoalsScored?.Away ?? 0, teamStats.TotalMatches?.Away ?? 1)
                        : 0;

                    avgTotalGoals = SafeDivide(teamStats.Scoring?.GoalsScored?.Total ?? 0, teamStats.TotalMatches?.Total ?? 1);

                    // Clean sheet stats
                    cleanSheets = teamStats.Conceding?.CleanSheets?.Total ?? 0;
                    homeCleanSheets = teamStats.Conceding?.CleanSheets?.Home ?? 0;
                    awayCleanSheets = teamStats.Conceding?.CleanSheets?.Away ?? 0;
                    cleanSheetPercentage = CalculateCleanSheetPercentage(teamStats);

                    // Goals conceded stats
                    averageGoalsConceded = teamStats.Conceding?.GoalsConcededAverage?.Total ?? 0;
                    homeAverageGoalsConceded = teamStats.Conceding?.GoalsConcededAverage?.Home ?? 0;
                    awayAverageGoalsConceded = teamStats.Conceding?.GoalsConcededAverage?.Away ?? 0;
                }

                // Calculate form strength
                double formStrength = CalculateFormStrength(form);

                // Get odds with safety
                double avgOdds = isHome
                    ? GetHomeOdds(sportMatch.OriginalMatch.Markets)
                    : GetAwayOdds(sportMatch.OriginalMatch.Markets);

                // Calculate match statistics
                int totalHomeWins = teamStats?.TotalWins?.Home ?? 0;
                int totalAwayWins = teamStats?.TotalWins?.Away ?? 0;

                // Calculate draws and losses intelligently
                int totalHomeDraws = CalculateHomeDraws(teamStats);
                int totalAwayDraws = CalculateAwayDraws(teamStats);
                int totalHomeLosses = CalculateHomeLosses(teamStats);
                int totalAwayLosses = CalculateAwayLosses(teamStats);

                // Calculate win percentages with safety
                double homeWinPercentage = SafeDivide(totalHomeWins, Math.Max(1, totalHomeMatches)) * 100;
                double awayWinPercentage = SafeDivide(totalAwayWins, Math.Max(1, totalAwayMatches)) * 100;
                double totalMatches = (totalHomeMatches + totalAwayMatches);
                double winPercentage = SafeDivide((totalHomeWins + totalAwayWins), Math.Max(1, totalMatches)) * 100;

                // Calculate BTTS (Both Teams To Score) rates
                double bttsRate = CalculateBttsRate(teamStats);
                double homeBttsRate = CalculateHomeBttsRate(teamStats);
                double awayBttsRate = CalculateAwayBttsRate(teamStats);

                // Calculate halftime/fulltime goal percentages
                double? firstHalfGoalsPercent = CalculateFirstHalfGoalsPercent(teamStats);
                double? secondHalfGoalsPercent = CalculateSecondHalfGoalsPercent(teamStats);

                // Determine scoring patterns
                double? scoringFirstWinRate = CalculateScoringFirstWinRate(sportMatch, isHome);
                double? concedingFirstWinRate = CalculateConcedingFirstWinRate(sportMatch, isHome);

                // Calculate late goal rate
                double? lateGoalRate = CalculateLateGoalRate(sportMatch, isHome);

                // Create team data structure
                return new TeamData
                {
                    Name = teamName,
                    Position = position,
                    Logo = logo,
                    AvgHomeGoals = Math.Round(avgHomeGoals, 2),
                    AvgAwayGoals = Math.Round(avgAwayGoals, 2),
                    AvgTotalGoals = Math.Round(avgTotalGoals, 2),
                    // Calculate over 1.5 goals statistics
                    HomeMatchesOver15 = CalculateHomeMatchesOver15(sportMatch, isHome),
                    AwayMatchesOver15 = CalculateAwayMatchesOver15(sportMatch, isHome),
                    TotalHomeMatches = totalHomeMatches,
                    TotalAwayMatches = totalAwayMatches,
                    Form = form,
                    HomeForm = isHome ? form : "",
                    AwayForm = !isHome ? form : "",
                    CleanSheets = cleanSheets,
                    HomeCleanSheets = homeCleanSheets,
                    AwayCleanSheets = awayCleanSheets,
                    ScoringFirstWinRate = scoringFirstWinRate,
                    ConcedingFirstWinRate = concedingFirstWinRate,
                    FirstHalfGoalsPercent = firstHalfGoalsPercent,
                    SecondHalfGoalsPercent = secondHalfGoalsPercent,
                    AvgCorners = CalculateAvgCorners(sportMatch, isHome),
                    BttsRate = bttsRate,
                    HomeBttsRate = homeBttsRate,
                    AwayBttsRate = awayBttsRate,
                    LateGoalRate = lateGoalRate,
                    GoalDistribution = ExtractGoalDistribution(teamStats),
                    AgainstTopTeamsPoints = CalculateAgainstTopTeamsPoints(sportMatch, isHome),
                    AgainstMidTeamsPoints = CalculateAgainstMidTeamsPoints(sportMatch, isHome),
                    AgainstBottomTeamsPoints = CalculateAgainstBottomTeamsPoints(sportMatch, isHome),
                    IsHomeTeam = isHome,
                    FormStrength = Math.Round(formStrength, 4),
                    FormRating = Math.Round(formStrength, 4),
                    WinPercentage = Math.Round(winPercentage, 2),
                    HomeWinPercentage = Math.Round(homeWinPercentage, 2),
                    AwayWinPercentage = Math.Round(awayWinPercentage, 2),
                    CleanSheetPercentage = Math.Round(cleanSheetPercentage, 2),
                    AverageGoalsScored = Math.Round(avgTotalGoals, 2),
                    AverageGoalsConceded = Math.Round(averageGoalsConceded, 2),
                    HomeAverageGoalsScored = Math.Round(avgHomeGoals, 2),
                    HomeAverageGoalsConceded = Math.Round(homeAverageGoalsConceded, 2),
                    AwayAverageGoalsScored = Math.Round(avgAwayGoals, 2),
                    AwayAverageGoalsConceded = Math.Round(awayAverageGoalsConceded, 2),
                    GoalsScoredAverage = Math.Round(avgTotalGoals, 2),
                    GoalsConcededAverage = Math.Round(averageGoalsConceded, 2),
                    AverageCorners = CalculateAvgCorners(sportMatch, isHome) ?? 0,
                    AvgOdds = avgOdds,
                    LeagueAvgGoals = CalculateLeagueAvgGoals(sportMatch),
                    Possession = CalculatePossession(sportMatch, isHome),
                    OpponentName = isHome
                        ? sportMatch.OriginalMatch.Teams.Away.Name
                        : sportMatch.OriginalMatch.Teams.Home.Name,
                    TotalHomeWins = totalHomeWins,
                    TotalAwayWins = totalAwayWins,
                    TotalHomeDraws = totalHomeDraws,
                    TotalAwayDraws = totalAwayDraws,
                    TotalHomeLosses = totalHomeLosses,
                    TotalAwayLosses = totalAwayLosses
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating team data for {(isHome ? "home" : "away")} team in match {sportMatch?.MatchId}: {ex.Message}");

                // Return a fallback object with minimal data to avoid null reference issues
                string teamName = isHome
                    ? sportMatch?.OriginalMatch?.Teams?.Home?.Name ?? "Unknown Home Team"
                    : sportMatch?.OriginalMatch?.Teams?.Away?.Name ?? "Unknown Away Team";

                return new TeamData
                {
                    Name = teamName,
                    Position = 0,
                    Logo = "",
                    Form = "",
                    IsHomeTeam = isHome,
                    FormStrength = 50,
                    FormRating = 50,
                    OpponentName = isHome
                        ? sportMatch?.OriginalMatch?.Teams?.Away?.Name ?? "Unknown Away Team"
                        : sportMatch?.OriginalMatch?.Teams?.Home?.Name ?? "Unknown Home Team",
                    GoalDistribution = new Dictionary<string, object>()
                };
            }
        }

        /// <summary>
        /// Gets the team's position in the league table
        /// </summary>
        private int GetTeamPosition(Background.EnrichedSportMatch sportMatch, string teamName)
        {
            if (string.IsNullOrEmpty(teamName))
                return 0;

            if (sportMatch.TeamTableSlice == null ||
                sportMatch.TeamTableSlice.TableRows == null ||
                !sportMatch.TeamTableSlice.TableRows.Any())
            {
                return 0;
            }

            // Try to find team in table rows by exact name match
            var teamRow = sportMatch.TeamTableSlice.TableRows
                .FirstOrDefault(r => StringMatches(r.Team?.Name, teamName) ||
                                    StringMatches(r.Team?.MediumName, teamName));

            if (teamRow != null)
                return teamRow.Pos;

            // Fallback to partial name matching if needed
            teamRow = sportMatch.TeamTableSlice.TableRows
                .FirstOrDefault(r => ContainsTeamName(r.Team?.Name, teamName) ||
                                    ContainsTeamName(r.Team?.MediumName, teamName));

            return teamRow?.Pos ?? 0;
        }

        /// <summary>
        /// Compares team names, handling case sensitivity and null values
        /// </summary>
        private bool StringMatches(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
                return false;

            return s1.Equals(s2, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks if one team name contains the other or vice versa
        /// </summary>
        private bool ContainsTeamName(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
                return false;

            return s1.Contains(s2, StringComparison.OrdinalIgnoreCase) ||
                   s2.Contains(s1, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Extracts team form as a string (W/D/L) from recent matches
        /// </summary>
        private string ExtractTeamForm(Background.EnrichedSportMatch sportMatch, bool isHome)
        {
            var teamLastX = isHome ? sportMatch.Team1LastX : sportMatch.Team2LastX;

            if (teamLastX == null || teamLastX.Matches == null || !teamLastX.Matches.Any())
            {
                return ""; // No form data available
            }

            // Get the team name to compare in match results
            string teamName = isHome
                ? sportMatch.OriginalMatch.Teams.Home.Name
                : sportMatch.OriginalMatch.Teams.Away.Name;

            if (string.IsNullOrEmpty(teamName))
            {
                return "";
            }

            // Get the last 5 matches (or fewer if less are available)
            var recentMatches = teamLastX.Matches
                .Where(m => m.Result != null && m.Teams != null &&
                           (m.Result.Home.HasValue || m.Result.Away.HasValue))
                .OrderByDescending(m => ParseMatchDate(m.Time))
                .Take(5)
                .ToList();

            if (!recentMatches.Any())
            {
                return "";
            }

            // Determine result of each match from perspective of this team
            var formChars = new List<char>();

            foreach (var match in recentMatches)
            {
                bool isHomeInMatch = match.Teams.Home != null &&
                    (StringMatches(match.Teams.Home.Name, teamName) ||
                     StringMatches(match.Teams.Home.MediumName, teamName) ||
                     ContainsTeamName(match.Teams.Home.Name, teamName) ||
                     ContainsTeamName(match.Teams.Home.MediumName, teamName));

                string result = GetMatchResult(match, isHomeInMatch);

                switch (result)
                {
                    case "W": formChars.Add('W'); break;
                    case "D": formChars.Add('D'); break;
                    case "L": formChars.Add('L'); break;
                    default: break; // Skip if result can't be determined
                }
            }

            return string.Join("", formChars);
        }

        /// <summary>
        /// Parses match date from various formats
        /// </summary>
        private DateTime ParseMatchDate(Background.MatchTimeInfo timeInfo)
        {
            if (timeInfo == null)
            {
                return DateTime.MinValue;
            }

            // Handle timestamp if available
            if (timeInfo.Timestamp > 0)
            {
                DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(timeInfo.Timestamp).DateTime;
                return dateTime;
            }

            // Try to parse date in common formats
            string[] dateFormats = { "dd/MM/yy", "dd/MM/yyyy", "yyyy-MM-dd", "MM/dd/yyyy" };

            if (!string.IsNullOrEmpty(timeInfo.Date))
            {
                foreach (var format in dateFormats)
                {
                    if (DateTime.TryParseExact(timeInfo.Date, format, CultureInfo.InvariantCulture,
                                             DateTimeStyles.None, out DateTime result))
                    {
                        // If we have time information, combine it with the date
                        if (!string.IsNullOrEmpty(timeInfo.Time) &&
                            TimeSpan.TryParse(timeInfo.Time, out TimeSpan timeSpan))
                        {
                            return result.Add(timeSpan);
                        }

                        return result;
                    }
                }
            }

            return DateTime.MinValue;
        }

        /// <summary>
        /// Gets the result (W/D/L) of a match from the perspective of the specified team
        /// </summary>
        private string GetMatchResult(Background.ExtendedMatchStat match, bool isHomeTeam)
        {
            if (match.Result == null)
                return "";

            // If we have a winner field, use that
            if (!string.IsNullOrEmpty(match.Result.Winner))
            {
                if (match.Result.Winner == "home" && isHomeTeam) return "W";
                if (match.Result.Winner == "away" && !isHomeTeam) return "W";
                if (match.Result.Winner == "home" && !isHomeTeam) return "L";
                if (match.Result.Winner == "away" && isHomeTeam) return "L";
                // If no clear winner is indicated, it's likely a draw
                return "D";
            }

            // If we don't have a winner field, try to determine from the score
            int? homeGoals = match.Result.Home;
            int? awayGoals = match.Result.Away;

            if (homeGoals.HasValue && awayGoals.HasValue)
            {
                if (homeGoals > awayGoals && isHomeTeam) return "W";
                if (homeGoals < awayGoals && !isHomeTeam) return "W";
                if (homeGoals > awayGoals && !isHomeTeam) return "L";
                if (homeGoals < awayGoals && isHomeTeam) return "L";
                if (homeGoals == awayGoals) return "D";
            }

            return ""; // Unable to determine result
        }

        /// <summary>
        /// Extracts odds information from markets data
        /// </summary>
        private MatchOdds ExtractOddsInfo(List<Background.ArbitrageLiveMatchBackgroundService.MarketData> markets)
        {
            // Set default values
            var oddsInfo = new MatchOdds
            {
                HomeWin = 0,
                Draw = 0,
                AwayWin = 0,
                Over15Goals = 0,
                Under15Goals = 0,
                Over25Goals = 0,
                Under25Goals = 0,
                BttsYes = 0,
                BttsNo = 0
            };

            if (markets == null || !markets.Any())
            {
                return oddsInfo;
            }

            try
            {
                // Extract 1X2 odds (primary market)
                var market1X2 = markets.FirstOrDefault(m =>
                    m.Id == "1" ||
                    m.Name?.Equals("1X2", StringComparison.OrdinalIgnoreCase) == true ||
                    m.Desc?.Equals("1X2", StringComparison.OrdinalIgnoreCase) == true);

                if (market1X2 != null && market1X2.Outcomes != null)
                {
                    // Find home win outcome
                    var homeOutcome = market1X2.Outcomes.FirstOrDefault(o =>
                        o.Id == "1" || o.Desc?.Equals("Home", StringComparison.OrdinalIgnoreCase) == true);
                    if (homeOutcome != null && double.TryParse(homeOutcome.Odds, out double homeOdds))
                    {
                        oddsInfo.HomeWin = homeOdds;
                    }

                    // Find draw outcome
                    var drawOutcome = market1X2.Outcomes.FirstOrDefault(o =>
                        o.Id == "2" || o.Desc?.Equals("Draw", StringComparison.OrdinalIgnoreCase) == true ||
                        o.Desc?.Equals("X", StringComparison.OrdinalIgnoreCase) == true);
                    if (drawOutcome != null && double.TryParse(drawOutcome.Odds, out double drawOdds))
                    {
                        oddsInfo.Draw = drawOdds;
                    }

                    // Find away win outcome
                    var awayOutcome = market1X2.Outcomes.FirstOrDefault(o =>
                        o.Id == "3" || o.Desc?.Equals("Away", StringComparison.OrdinalIgnoreCase) == true);
                    if (awayOutcome != null && double.TryParse(awayOutcome.Odds, out double awayOdds))
                    {
                        oddsInfo.AwayWin = awayOdds;
                    }
                }

                // Extract Over/Under markets
                var overUnderMarket = markets.FirstOrDefault(m =>
                    m.Id == "18" || m.Name?.Contains("Over/Under", StringComparison.OrdinalIgnoreCase) == true);

                if (overUnderMarket != null && overUnderMarket.Outcomes != null)
                {
                    // Look for over/under 1.5 goals
                    var over15Outcome = overUnderMarket.Outcomes.FirstOrDefault(o =>
                        o.Desc?.Contains("Over 1.5", StringComparison.OrdinalIgnoreCase) == true);
                    if (over15Outcome != null && double.TryParse(over15Outcome.Odds, out double over15Odds))
                    {
                        oddsInfo.Over15Goals = over15Odds;
                    }

                    var under15Outcome = overUnderMarket.Outcomes.FirstOrDefault(o =>
                        o.Desc?.Contains("Under 1.5", StringComparison.OrdinalIgnoreCase) == true);
                    if (under15Outcome != null && double.TryParse(under15Outcome.Odds, out double under15Odds))
                    {
                        oddsInfo.Under15Goals = under15Odds;
                    }

                    // Look for over/under 2.5 goals
                    var over25Outcome = overUnderMarket.Outcomes.FirstOrDefault(o =>
                        o.Desc?.Contains("Over 2.5", StringComparison.OrdinalIgnoreCase) == true);
                    if (over25Outcome != null && double.TryParse(over25Outcome.Odds, out double over25Odds))
                    {
                        oddsInfo.Over25Goals = over25Odds;
                    }

                    var under25Outcome = overUnderMarket.Outcomes.FirstOrDefault(o =>
                        o.Desc?.Contains("Under 2.5", StringComparison.OrdinalIgnoreCase) == true);
                    if (under25Outcome != null && double.TryParse(under25Outcome.Odds, out double under25Odds))
                    {
                        oddsInfo.Under25Goals = under25Odds;
                    }
                }

                // Extract BTTS (Both Teams To Score) market
                var bttsMarket = markets.FirstOrDefault(m =>
                    m.Id == "10" ||
                    m.Name?.Contains("Both Teams To Score", StringComparison.OrdinalIgnoreCase) == true ||
                    m.Desc?.Contains("Both Teams To Score", StringComparison.OrdinalIgnoreCase) == true ||
                    m.Name?.Contains("BTTS", StringComparison.OrdinalIgnoreCase) == true);

                if (bttsMarket != null && bttsMarket.Outcomes != null)
                {
                    var bttsYesOutcome = bttsMarket.Outcomes.FirstOrDefault(o =>
                        o.Desc?.Equals("Yes", StringComparison.OrdinalIgnoreCase) == true);
                    if (bttsYesOutcome != null && double.TryParse(bttsYesOutcome.Odds, out double bttsYesOdds))
                    {
                        oddsInfo.BttsYes = bttsYesOdds;
                    }

                    var bttsNoOutcome = bttsMarket.Outcomes.FirstOrDefault(o =>
                        o.Desc?.Equals("No", StringComparison.OrdinalIgnoreCase) == true);
                    if (bttsNoOutcome != null && double.TryParse(bttsNoOutcome.Odds, out double bttsNoOdds))
                    {
                        oddsInfo.BttsNo = bttsNoOdds;
                    }
                }

                // Look for Double Chance market to help calculate odds if 1X2 is incomplete
                var doubleChanceMarket = markets.FirstOrDefault(m =>
                    m.Id == "10" ||
                    m.Name?.Contains("Double Chance", StringComparison.OrdinalIgnoreCase) == true ||
                    m.Desc?.Contains("Double Chance", StringComparison.OrdinalIgnoreCase) == true);

                if (doubleChanceMarket != null && doubleChanceMarket.Outcomes != null &&
                    (oddsInfo.HomeWin == 0 || oddsInfo.Draw == 0 || oddsInfo.AwayWin == 0))
                {
                    // This is a complex calculation and would need proper implied odds extraction
                    // Just a placeholder for potential implementation
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting odds information: {Message}", ex.Message);
            }

            return oddsInfo;
        }

        /// <summary>
        /// Gets the home team odds from markets data
        /// </summary>
        private double GetHomeOdds(List<Background.ArbitrageLiveMatchBackgroundService.MarketData> markets)
        {
            if (markets == null || !markets.Any())
            {
                return 0;
            }

            try
            {
                var market1X2 = markets.FirstOrDefault(m =>
                    m.Id == "1" ||
                    m.Name?.Equals("1X2", StringComparison.OrdinalIgnoreCase) == true ||
                    m.Desc?.Equals("1X2", StringComparison.OrdinalIgnoreCase) == true);

                if (market1X2 == null || market1X2.Outcomes == null)
                {
                    return 0;
                }

                var homeOutcome = market1X2.Outcomes.FirstOrDefault(o =>
                    o.Id == "1" || o.Desc?.Equals("Home", StringComparison.OrdinalIgnoreCase) == true);

                if (homeOutcome != null && double.TryParse(homeOutcome.Odds, out double homeOdds))
                {
                    return homeOdds;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting home odds: {Message}", ex.Message);
            }

            return 0;
        }

        /// <summary>
        /// Gets the away team odds from markets data
        /// </summary>
        private double GetAwayOdds(List<Background.ArbitrageLiveMatchBackgroundService.MarketData> markets)
        {
            if (markets == null || !markets.Any())
            {
                return 0;
            }

            try
            {
                var market1X2 = markets.FirstOrDefault(m =>
                    m.Id == "1" ||
                    m.Name?.Equals("1X2", StringComparison.OrdinalIgnoreCase) == true ||
                    m.Desc?.Equals("1X2", StringComparison.OrdinalIgnoreCase) == true);

                if (market1X2 == null || market1X2.Outcomes == null)
                {
                    return 0;
                }

                var awayOutcome = market1X2.Outcomes.FirstOrDefault(o =>
                    o.Id == "3" || o.Desc?.Equals("Away", StringComparison.OrdinalIgnoreCase) == true);

                if (awayOutcome != null && double.TryParse(awayOutcome.Odds, out double awayOdds))
                {
                    return awayOdds;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting away odds: {Message}", ex.Message);
            }

            return 0;
        }

        /// <summary>
        /// Creates head-to-head data with historical match information
        /// </summary>
        private HeadToHeadData CreateHeadToHeadData(Background.EnrichedSportMatch sportMatch)
        {
            var emptyHeadToHead = new HeadToHeadData
            {
                Matches = 0,
                Wins = 0,
                Draws = 0,
                Losses = 0,
                GoalsScored = 0,
                GoalsConceded = 0,
                RecentMatches = new List<RecentMatchResult>()
            };

            if (sportMatch?.TeamVersusRecent == null || sportMatch.TeamVersusRecent.Matches == null)
            {
                return emptyHeadToHead;
            }

            try
            {
                // Get valid head-to-head matches
                var headToHeadMatches = sportMatch.TeamVersusRecent.Matches
                    .Where(m => m.Result != null && m.Teams != null &&
                              m.Teams.Home != null && m.Teams.Away != null &&
                              m.Result.Home.HasValue && m.Result.Away.HasValue)
                    .ToList();

                if (!headToHeadMatches.Any())
                {
                    return emptyHeadToHead;
                }

                // Determine which team is the home team in the upcoming match
                string homeTeamName = sportMatch.OriginalMatch.Teams.Home.Name;
                if (string.IsNullOrEmpty(homeTeamName))
                {
                    return emptyHeadToHead;
                }

                int wins = 0, draws = 0, losses = 0, goalsScored = 0, goalsConceded = 0;
                var recentResults = new List<RecentMatchResult>();

                foreach (var match in headToHeadMatches)
                {
                    bool isHomeTeam = match.Teams.Home != null &&
                        (StringMatches(match.Teams.Home.Name, homeTeamName) ||
                         StringMatches(match.Teams.Home.MediumName, homeTeamName) ||
                         ContainsTeamName(match.Teams.Home.Name, homeTeamName) ||
                         ContainsTeamName(match.Teams.Home.MediumName, homeTeamName));

                    int homeGoals = match.Result.Home ?? 0;
                    int awayGoals = match.Result.Away ?? 0;

                    if (isHomeTeam)
                    {
                        // Home team perspective
                        if (homeGoals > awayGoals) wins++;
                        else if (homeGoals < awayGoals) losses++;
                        else draws++;

                        goalsScored += homeGoals;
                        goalsConceded += awayGoals;
                    }
                    else
                    {
                        // Away team perspective
                        if (awayGoals > homeGoals) wins++;
                        else if (awayGoals < homeGoals) losses++;
                        else draws++;

                        goalsScored += awayGoals;
                        goalsConceded += homeGoals;
                    }

                    // Add to recent matches
                    DateTime matchDate = ParseMatchDate(match.Time);
                    if (matchDate != DateTime.MinValue)
                    {
                        var homeAbbr = GetTeamAbbreviation(match.Teams.Home);
                        var awayAbbr = GetTeamAbbreviation(match.Teams.Away);

                        recentResults.Add(new RecentMatchResult
                        {
                            Date = matchDate.ToString("yyyy-MM-dd"),
                            Result = $"{homeAbbr} {homeGoals}-{awayGoals} {awayAbbr}"
                        });
                    }
                }

                return new HeadToHeadData
                {
                    Matches = headToHeadMatches.Count,
                    Wins = wins,
                    Draws = draws,
                    Losses = losses,
                    GoalsScored = goalsScored,
                    GoalsConceded = goalsConceded,
                    RecentMatches = recentResults.OrderByDescending(r => r.Date).Take(5).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating head-to-head data for match {sportMatch?.MatchId}: {ex.Message}");
                return emptyHeadToHead;
            }
        }

        /// <summary>
        /// Gets a team abbreviation, with fallback to name truncation
        /// </summary>
        private string GetTeamAbbreviation(Background.MatchTeam team)
        {
            if (team == null)
                return "UNK";

            if (!string.IsNullOrEmpty(team.Abbr))
                return team.Abbr;

            if (!string.IsNullOrEmpty(team.Name))
                return team.Name.Substring(0, Math.Min(3, team.Name.Length));

            return "UNK";
        }

        /// <summary>
        /// Creates corner statistics based on team data
        /// </summary>
        private CornerStats CreateCornerStats(Background.EnrichedSportMatch sportMatch)
        {
            // Default corner stats object
            var cornerStats = new CornerStats
            {
                HomeAvg = 0,
                AwayAvg = 0,
                TotalAvg = 0
            };

            try
            {
                // In the provided data, corner statistics might not be directly available
                // We'll attempt to extract corner data from recent matches if possible

                // Check home team's recent matches for corner data
                double homeTeamCorners = ExtractAverageCorners(sportMatch.Team1LastX?.Matches);

                // Check away team's recent matches for corner data
                double awayTeamCorners = ExtractAverageCorners(sportMatch.Team2LastX?.Matches);

                // Update corner stats if we found data
                if (homeTeamCorners > 0)
                    cornerStats.HomeAvg = Math.Round(homeTeamCorners, 2);

                if (awayTeamCorners > 0)
                    cornerStats.AwayAvg = Math.Round(awayTeamCorners, 2);

                // Calculate total average
                if (homeTeamCorners > 0 || awayTeamCorners > 0)
                {
                    cornerStats.TotalAvg = Math.Round(
                        (homeTeamCorners + awayTeamCorners) /
                        (homeTeamCorners > 0 && awayTeamCorners > 0 ? 2 : 1),
                        2
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating corner stats for match {sportMatch?.MatchId}: {ex.Message}");
            }

            return cornerStats;
        }

        /// <summary>
        /// Extracts average corner count from match data
        /// </summary>
        private double ExtractAverageCorners(List<Background.ExtendedMatchStat> matches)
        {
            if (matches == null || !matches.Any())
                return 0;

            // Filter matches with corner data
            var matchesWithCorners = matches
                .Where(m => m.Corners != null && (m.Corners.Home > 0 || m.Corners.Away > 0))
                .ToList();

            if (!matchesWithCorners.Any())
                return 0;

            // Calculate average corner count
            double totalCorners = matchesWithCorners.Sum(m => m.Corners.Home + m.Corners.Away);
            return totalCorners / matchesWithCorners.Count;
        }

        /// <summary>
        /// Creates scoring patterns data for match prediction
        /// </summary>
        private ScoringPatterns CreateScoringPatterns(Background.EnrichedSportMatch sportMatch, TeamData homeTeam, TeamData awayTeam)
        {
            var scoringPatterns = new ScoringPatterns
            {
                HomeFirstGoalRate = 0,
                AwayFirstGoalRate = 0,
                HomeLateGoalRate = 0,
                AwayLateGoalRate = 0
            };

            try
            {
                // Extract first goal rates and late goal rates from team data
                scoringPatterns.HomeFirstGoalRate = CalculateFirstGoalRate(sportMatch, true);
                scoringPatterns.AwayFirstGoalRate = CalculateFirstGoalRate(sportMatch, false);
                scoringPatterns.HomeLateGoalRate = homeTeam.LateGoalRate ?? 0;
                scoringPatterns.AwayLateGoalRate = awayTeam.LateGoalRate ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating scoring patterns for match {sportMatch?.MatchId}: {ex.Message}");
            }

            return scoringPatterns;
        }

        /// <summary>
        /// Calculates the percentage of matches where a team scores first
        /// </summary>
        private double CalculateFirstGoalRate(Background.EnrichedSportMatch sportMatch, bool isHome)
        {
            var teamLastX = isHome ? sportMatch.Team1LastX : sportMatch.Team2LastX;
            if (teamLastX == null || teamLastX.Matches == null || !teamLastX.Matches.Any())
                return 0;

            string teamName = isHome
                ? sportMatch.OriginalMatch.Teams.Home.Name
                : sportMatch.OriginalMatch.Teams.Away.Name;

            // Count matches where team scored first
            int matchesWithFirstGoal = 0;
            int totalMatches = 0;

            foreach (var match in teamLastX.Matches)
            {
                if (match.FirstGoal == null)
                    continue;

                totalMatches++;

                bool isHomeInMatch = match.Teams?.Home != null &&
                    (StringMatches(match.Teams.Home.Name, teamName) ||
                     StringMatches(match.Teams.Home.MediumName, teamName) ||
                     ContainsTeamName(match.Teams.Home.Name, teamName) ||
                     ContainsTeamName(match.Teams.Home.MediumName, teamName));

                if ((isHomeInMatch && match.FirstGoal == "home") ||
                    (!isHomeInMatch && match.FirstGoal == "away"))
                {
                    matchesWithFirstGoal++;
                }
            }

            return totalMatches > 0 ? Math.Round(100.0 * matchesWithFirstGoal / totalMatches, 2) : 0;
        }

        /// <summary>
        /// Determines the favorite team based on odds
        /// </summary>
        private string DetermineFavorite(MatchOdds odds)
        {
            // Handle case where odds are not available
            if (odds.HomeWin <= 0 && odds.AwayWin <= 0 && odds.Draw <= 0)
            {
                return "unknown";
            }

            // For draw to be favorite, it should have lower odds than both home and away
            if (odds.Draw > 0 &&
                ((odds.HomeWin > 0 && odds.Draw < odds.HomeWin) || odds.HomeWin <= 0) &&
                ((odds.AwayWin > 0 && odds.Draw < odds.AwayWin) || odds.AwayWin <= 0))
            {
                return "draw";
            }

            // If no draw odds, or if home/away has lower odds
            if (odds.HomeWin > 0 && odds.AwayWin > 0)
            {
                if (odds.HomeWin < odds.AwayWin)
                    return "home";
                else
                    return "away";
            }
            else if (odds.HomeWin > 0)
            {
                return "home";
            }
            else if (odds.AwayWin > 0)
            {
                return "away";
            }

            return "unknown";
        }

        /// <summary>
        /// Calculates confidence score based on multiple factors
        /// </summary>
        private int CalculateConfidenceScore(Background.EnrichedSportMatch sportMatch, TeamData homeTeam, TeamData awayTeam, MatchOdds odds)
        {
            try
            {
                // Factors for confidence calculation
                double formDiff = Math.Abs(homeTeam.FormStrength - awayTeam.FormStrength);
                double oddsDiff = 0;

                // Calculate odds difference based on available odds
                if (odds.HomeWin > 0 && odds.AwayWin > 0)
                {
                    oddsDiff = Math.Abs(odds.HomeWin - odds.AwayWin);
                }

                // Get position gap
                double positionGap = Math.Abs(homeTeam.Position - awayTeam.Position);

                // Head-to-head advantage
                double h2hFactor = 0;
                if (sportMatch.TeamVersusRecent != null && sportMatch.TeamVersusRecent.Matches != null &&
                    sportMatch.TeamVersusRecent.Matches.Any())
                {
                    var h2h = CreateHeadToHeadData(sportMatch);
                    double winRate = SafeDivide(h2h.Wins, h2h.Matches);
                    double lossRate = SafeDivide(h2h.Losses, h2h.Matches);
                    h2hFactor = (winRate - lossRate) * 10; // -10 to +10 range
                }

                // Combine factors with weights
                double score = 50.0; // Start with neutral 50
                score += formDiff * 5; // Form difference (0-50 points)
                score += oddsDiff * 5; // Odds difference (up to ~10 points)
                score += positionGap * 0.5; // Position gap (small effect)
                score += h2hFactor; // Head-to-head advantage/disadvantage (-10 to +10)

                // Favor the home team slightly (home advantage)
                score += 2;

                // Cap at reasonable bounds
                return (int)Math.Min(Math.Max(Math.Round(score), 0), 100);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calculating confidence score for match {sportMatch?.MatchId}: {ex.Message}");
                return 50; // Return neutral confidence score
            }
        }

        /// <summary>
        /// Calculates expected goals based on team offensive and defensive strength
        /// </summary>
        private double CalculateExpectedGoals(Background.EnrichedSportMatch sportMatch, TeamData homeTeam, TeamData awayTeam)
        {
            try
            {
                // Base calculation using average goals scored and conceded
                double homeExpected = (homeTeam.HomeAverageGoalsScored + awayTeam.AwayAverageGoalsConceded) / 2;
                double awayExpected = (awayTeam.AwayAverageGoalsScored + homeTeam.HomeAverageGoalsConceded) / 2;

                // Enhance with form adjustment
                double homeForm = CalculateFormNumeric(homeTeam.Form);
                double awayForm = CalculateFormNumeric(awayTeam.Form);

                homeExpected *= (1 + (homeForm - 0.5) * 0.2); // Adjust by up to ±10%
                awayExpected *= (1 + (awayForm - 0.5) * 0.2); // Adjust by up to ±10%

                // Adjust for head-to-head history
                if (sportMatch.TeamVersusRecent != null && sportMatch.TeamVersusRecent.Matches != null &&
                    sportMatch.TeamVersusRecent.Matches.Any())
                {
                    double avgGoals = sportMatch.TeamVersusRecent.Matches
                        .Where(m => m.Result != null && m.Result.Home.HasValue && m.Result.Away.HasValue)
                        .Select(m => (m.Result.Home ?? 0) + (m.Result.Away ?? 0))
                        .DefaultIfEmpty(0)
                        .Average();

                    // Blend with head-to-head average
                    double currentExpected = homeExpected + awayExpected;
                    double blendedExpected = (currentExpected * 0.7) + (avgGoals * 0.3);

                    // Rebalance home/away proportion
                    double homeRatio = SafeDivide(homeExpected, currentExpected);
                    homeExpected = blendedExpected * homeRatio;
                    awayExpected = blendedExpected * (1 - homeRatio);
                }

                // Safety check for NaN or Infinity
                if (double.IsNaN(homeExpected) || double.IsInfinity(homeExpected))
                    homeExpected = 1.0;

                if (double.IsNaN(awayExpected) || double.IsInfinity(awayExpected))
                    awayExpected = 0.7;

                return homeExpected + awayExpected;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calculating expected goals for match {sportMatch?.MatchId}: {ex.Message}");
                return 2.5; // Fallback to typical average
            }
        }

        /// <summary>
        /// Converts form string to a numeric value (0-1 scale)
        /// </summary>
        private double CalculateFormNumeric(string form)
        {
            if (string.IsNullOrEmpty(form))
                return 0.5; // Neutral

            double score = 0;
            double weight = 1.0;
            double totalWeight = 0;

            foreach (char c in form)
            {
                switch (c)
                {
                    case 'W': score += weight; break;
                    case 'D': score += weight * 0.5; break;
                    case 'L': break; // No points for loss
                    default: continue; // Skip invalid characters
                }

                totalWeight += weight;
                weight *= 0.8; // Exponential decay for older results
            }

            return totalWeight > 0 ? score / totalWeight : 0.5;
        }

        /// <summary>
        /// Calculates average goals for both teams
        /// </summary>
        private double CalculateAverageGoals(Background.EnrichedSportMatch sportMatch, TeamData homeTeam, TeamData awayTeam)
        {
            try
            {
                double homeAvg = homeTeam.AvgTotalGoals;
                double awayAvg = awayTeam.AvgTotalGoals;

                // If we have scoring/conceding data available, use it
                if (sportMatch.Team1ScoringConceding != null && sportMatch.Team1ScoringConceding.Stats != null &&
                    sportMatch.Team2ScoringConceding != null && sportMatch.Team2ScoringConceding.Stats != null)
                {
                    double homeScored = sportMatch.Team1ScoringConceding.Stats.Scoring?.GoalsScored?.Total ?? 0;
                    double homeConceded = sportMatch.Team1ScoringConceding.Stats.Conceding?.GoalsConceded?.Total ?? 0;
                    double awayScored = sportMatch.Team2ScoringConceding.Stats.Scoring?.GoalsScored?.Total ?? 0;
                    double awayConceded = sportMatch.Team2ScoringConceding.Stats.Conceding?.GoalsConceded?.Total ?? 0;

                    double homeMatches = sportMatch.Team1ScoringConceding.Stats.TotalMatches?.Total ?? 0;
                    double awayMatches = sportMatch.Team2ScoringConceding.Stats.TotalMatches?.Total ?? 0;

                    if (homeMatches > 0 && awayMatches > 0)
                    {
                        homeAvg = (homeScored / homeMatches) + (awayConceded / awayMatches);
                        awayAvg = (awayScored / awayMatches) + (homeConceded / homeMatches);
                        return Math.Round((homeAvg + awayAvg) / 2, 2);
                    }
                }

                // Fallback calculation
                return Math.Round((homeTeam.AvgHomeGoals + awayTeam.AvgAwayGoals) / 2, 2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calculating average goals for match {sportMatch?.MatchId}: {ex.Message}");
                return 2.5; // Fallback to typical average
            }
        }

        /// <summary>
        /// Calculates defensive strength factor (1.0 is average, higher means better defense)
        /// </summary>
        private double CalculateDefensiveStrength(Background.EnrichedSportMatch sportMatch, TeamData homeTeam, TeamData awayTeam)
        {
            try
            {
                // Start with clean sheet ratio if available
                double homeDefense = homeTeam.HomeCleanSheets > 0
                    ? 1.0 + (SafeDivide(homeTeam.HomeCleanSheets, Math.Max(1, homeTeam.TotalHomeMatches)) * 0.5)
                    : 1.0;

                double awayDefense = awayTeam.AwayCleanSheets > 0
                    ? 1.0 + (SafeDivide(awayTeam.AwayCleanSheets, Math.Max(1, awayTeam.TotalAwayMatches)) * 0.5)
                    : 1.0;

                // Also factor in goals conceded average if available
                if (homeTeam.HomeAverageGoalsConceded > 0)
                {
                    double leagueAvg = CalculateLeagueAvgGoals(sportMatch);
                    if (leagueAvg == 0) leagueAvg = 2.5; // Fallback

                    double homeConcededRatio = SafeDivide(homeTeam.HomeAverageGoalsConceded, leagueAvg / 2);
                    double awayConcededRatio = SafeDivide(awayTeam.AwayAverageGoalsConceded, leagueAvg / 2);

                    // Invert the ratio (lower goals conceded = higher defensive strength)
                    homeDefense *= homeConcededRatio > 0 ? Math.Min(2.0, 1.0 / homeConcededRatio) : 1.0;
                    awayDefense *= awayConcededRatio > 0 ? Math.Min(2.0, 1.0 / awayConcededRatio) : 1.0;
                }

                // Combine and ensure reasonable range
                double combinedDefense = (homeDefense + awayDefense) / 2;
                return Math.Round(Math.Min(Math.Max(combinedDefense, 0.5), 2.0), 2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calculating defensive strength for match {sportMatch?.MatchId}: {ex.Message}");
                return 1.0; // Return average defensive strength
            }
        }

        /// <summary>
        /// Generates prediction reasons based on match data
        /// </summary>
        private List<string> GeneratePredictionReasons(Background.EnrichedSportMatch sportMatch, TeamData homeTeam, TeamData awayTeam, MatchOdds odds, double expectedGoals)
        {
            var reasons = new List<string>();

            try
            {
                // Add form-based reason
                if (!string.IsNullOrEmpty(homeTeam.Form))
                {
                    reasons.Add($"{homeTeam.Name} form: {homeTeam.Form}");
                }

                if (!string.IsNullOrEmpty(awayTeam.Form))
                {
                    reasons.Add($"{awayTeam.Name} form: {awayTeam.Form}");
                }

                // Add goal expectation reason
                string scoringPotential = expectedGoals > 2.5 ? "High" : (expectedGoals > 1.5 ? "Moderate" : "Low");
                reasons.Add($"{scoringPotential}-scoring potential: {homeTeam.Name} ({homeTeam.HomeAverageGoalsScored.ToString("0.00")} home) vs {awayTeam.Name} ({awayTeam.AwayAverageGoalsScored.ToString("0.00")} away)");

                // Add head-to-head reason if available
                var h2h = CreateHeadToHeadData(sportMatch);
                if (h2h.Matches > 0)
                {
                    double avgGoals = SafeDivide(h2h.GoalsScored + h2h.GoalsConceded, h2h.Matches);
                    string h2hScoring = avgGoals > 2.5 ? "High" : (avgGoals > 1.5 ? "Moderate" : "Low");
                    reasons.Add($"H2H: {h2hScoring}-scoring fixtures averaging {avgGoals.ToString("0.0")} goals per game");
                }

                // Add odds-based reason
                if (odds.HomeWin > 0 && odds.AwayWin > 0)
                {
                    string favorite = DetermineFavorite(odds);
                    string favoriteTeam = favorite == "home" ? homeTeam.Name :
                                         favorite == "away" ? awayTeam.Name : "Draw";

                    double oddsRatio = Math.Min(odds.HomeWin, odds.AwayWin) / Math.Max(odds.HomeWin, odds.AwayWin);
                    string favoriteStrength = oddsRatio < 0.5 ? "Strong" : (oddsRatio < 0.7 ? "Moderate" : "Slight");

                    if (favorite != "draw")
                    {
                        reasons.Add($"{favoriteStrength} favorite: {favoriteTeam} (H: {odds.HomeWin.ToString("0.00")}, A: {odds.AwayWin.ToString("0.00")})");
                    }
                    else
                    {
                        reasons.Add($"Draw likely: Tight odds (H: {odds.HomeWin.ToString("0.00")}, A: {odds.AwayWin.ToString("0.00")})");
                    }
                }

                // Add clean sheet information if relevant
                if (homeTeam.HomeCleanSheets > 0 || awayTeam.AwayCleanSheets > 0)
                {
                    if (homeTeam.HomeCleanSheets > 2)
                    {
                        reasons.Add($"Strong home defense: {homeTeam.Name} kept {homeTeam.HomeCleanSheets} clean sheets at home");
                    }

                    if (awayTeam.AwayCleanSheets > 1)
                    {
                        reasons.Add($"Good away defense: {awayTeam.Name} kept {awayTeam.AwayCleanSheets} clean sheets away");
                    }
                }

                // Add position gap reason if significant
                int positionGap = Math.Abs(homeTeam.Position - awayTeam.Position);
                if (positionGap > 5 && homeTeam.Position > 0 && awayTeam.Position > 0)
                {
                    string betterTeam = homeTeam.Position < awayTeam.Position ? homeTeam.Name : awayTeam.Name;
                    reasons.Add($"Table position gap: {positionGap} places separating teams, favoring {betterTeam}");
                }

                // Ensure we have reasonable number of reasons
                while (reasons.Count > 5)
                {
                    reasons.RemoveAt(reasons.Count - 1); // Remove the last reason
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating prediction reasons for match {sportMatch?.MatchId}: {ex.Message}");

                // Ensure we have at least one reason
                if (!reasons.Any())
                {
                    reasons.Add($"Match prediction based on team statistics");
                }
            }

            return reasons;
        }

        /// <summary>
        /// Updates metadata for the league this match belongs to
        /// </summary>
        private void UpdateLeagueMetadata(PredictionMetadata metadata, UpcomingMatch match)
        {
            string leagueName = match.Venue;
            if (string.IsNullOrEmpty(leagueName))
            {
                leagueName = "Unknown League";
            }

            if (!metadata.LeagueData.ContainsKey(leagueName))
            {
                metadata.LeagueData[leagueName] = new LeagueMetadata
                {
                    Matches = 0,
                    TotalGoals = 0,
                    HomeWinRate = 0,
                    DrawRate = 0,
                    AwayWinRate = 0,
                    BttsRate = 0
                };
            }

            // Update match count
            metadata.LeagueData[leagueName].Matches++;

            // Update expected goals
            metadata.LeagueData[leagueName].TotalGoals += (int)Math.Round(match.ExpectedGoals);

            // Update winner probabilities based on odds
            if (match.Favorite == "home")
            {
                metadata.LeagueData[leagueName].HomeWinRate =
                    (metadata.LeagueData[leagueName].HomeWinRate * (metadata.LeagueData[leagueName].Matches - 1) + 100) /
                    metadata.LeagueData[leagueName].Matches;
            }
            else if (match.Favorite == "away")
            {
                metadata.LeagueData[leagueName].AwayWinRate =
                    (metadata.LeagueData[leagueName].AwayWinRate * (metadata.LeagueData[leagueName].Matches - 1) + 100) /
                    metadata.LeagueData[leagueName].Matches;
            }
            else if (match.Favorite == "draw")
            {
                metadata.LeagueData[leagueName].DrawRate =
                    (metadata.LeagueData[leagueName].DrawRate * (metadata.LeagueData[leagueName].Matches - 1) + 100) /
                    metadata.LeagueData[leagueName].Matches;
            }

            // Update BTTS rate if available
            if (match.Odds.BttsYes > 0 && match.Odds.BttsNo > 0)
            {
                double bttsProb = 1 / match.Odds.BttsYes / (1 / match.Odds.BttsYes + 1 / match.Odds.BttsNo) * 100;
                metadata.LeagueData[leagueName].BttsRate =
                    (metadata.LeagueData[leagueName].BttsRate * (metadata.LeagueData[leagueName].Matches - 1) + bttsProb) /
                    metadata.LeagueData[leagueName].Matches;
            }

            // Round values for presentation
            metadata.LeagueData[leagueName].HomeWinRate = Math.Round(metadata.LeagueData[leagueName].HomeWinRate, 1);
            metadata.LeagueData[leagueName].DrawRate = Math.Round(metadata.LeagueData[leagueName].DrawRate, 1);
            metadata.LeagueData[leagueName].AwayWinRate = Math.Round(metadata.LeagueData[leagueName].AwayWinRate, 1);
            metadata.LeagueData[leagueName].BttsRate = Math.Round(metadata.LeagueData[leagueName].BttsRate, 1);
        }

        #region Helper Methods for Statistical Calculations

        /// <summary>
        /// Safely performs division, handling divide-by-zero
        /// </summary>
        private double SafeDivide(double numerator, double denominator)
        {
            return denominator == 0 ? 0 : numerator / denominator;
        }

        /// <summary>
        /// Calculates form strength as a numeric value (0-100 scale)
        /// </summary>
        private double CalculateFormStrength(string form)
        {
            if (string.IsNullOrEmpty(form))
            {
                return 50.0; // Neutral rating
            }

            double strength = 50.0; // Start with neutral
            double weight = 1.0;
            double totalWeight = 0;

            // Newer matches have higher weight
            for (int i = 0; i < form.Length; i++)
            {
                char result = form[i];
                double adjustment = 0;

                switch (result)
                {
                    case 'W': adjustment = 10.0; break;
                    case 'D': adjustment = 0.0; break;
                    case 'L': adjustment = -10.0; break;
                    default: continue; // Skip invalid characters
                }

                strength += adjustment * weight;
                totalWeight += weight;
                weight *= 0.8; // Decrease weight for older matches
            }

            // Normalize
            if (totalWeight > 0)
            {
                strength = 50.0 + ((strength - 50.0) / totalWeight);
            }

            return strength;
        }

        /// <summary>
        /// Calculates clean sheet percentage from team stats
        /// </summary>
        private double CalculateCleanSheetPercentage(TeamStats teamStats)
        {
            if (teamStats == null || teamStats.Conceding == null ||
                teamStats.Conceding.CleanSheets == null || teamStats.TotalMatches == null ||
                teamStats.TotalMatches.Total == 0)
            {
                return 0;
            }

            return (double)teamStats.Conceding.CleanSheets.Total / teamStats.TotalMatches.Total * 100;
        }

        /// <summary>
        /// Calculates number of home draws
        /// </summary>
        private int CalculateHomeDraws(TeamStats teamStats)
        {
            if (teamStats == null || teamStats.TotalMatches == null ||
                teamStats.TotalWins == null || teamStats.TotalMatches.Home == 0)
            {
                return 0;
            }

            // Try to use direct draw data if available
            if (teamStats.DrawTotal > 0)
            {
                // If we have home/away breakdown
                if (teamStats.DrawHome > 0)
                {
                    return teamStats.DrawHome;
                }

                // Approximate from total
                return Math.Min(teamStats.DrawTotal / 2, teamStats.TotalMatches.Home - teamStats.TotalWins.Home);
            }

            // Home draws = Total home matches - Home wins - Home losses
            // Use CalculateHomeLosses for consistency
            int homeLosses = CalculateHomeLosses(teamStats);
            return Math.Max(0, teamStats.TotalMatches.Home - teamStats.TotalWins.Home - homeLosses);
        }

        /// <summary>
        /// Calculates number of away draws
        /// </summary>
        private int CalculateAwayDraws(TeamStats teamStats)
        {
            if (teamStats == null || teamStats.TotalMatches == null ||
                teamStats.TotalWins == null || teamStats.TotalMatches.Away == 0)
            {
                return 0;
            }

            // Try to use direct draw data if available
            if (teamStats.DrawTotal > 0)
            {
                // If we have home/away breakdown
                if (teamStats.DrawAway > 0)
                {
                    return teamStats.DrawAway;
                }

                // Approximate from total
                return Math.Min(teamStats.DrawTotal / 2, teamStats.TotalMatches.Away - teamStats.TotalWins.Away);
            }

            // Away draws = Total away matches - Away wins - Away losses
            // Use CalculateAwayLosses for consistency
            int awayLosses = CalculateAwayLosses(teamStats);
            return Math.Max(0, teamStats.TotalMatches.Away - teamStats.TotalWins.Away - awayLosses);
        }

        /// <summary>
        /// Calculates number of home losses
        /// </summary>
        private int CalculateHomeLosses(TeamStats teamStats)
        {
            if (teamStats == null || teamStats.TotalMatches == null ||
                teamStats.TotalWins == null || teamStats.TotalMatches.Home == 0)
            {
                return 0;
            }

            // If we have direct loss data
            if (teamStats.LossTotal > 0)
            {
                // If we have home/away breakdown
                if (teamStats.LossHome > 0)
                {
                    return teamStats.LossHome;
                }

                // Approximate from total
                return Math.Min(teamStats.LossTotal / 2, teamStats.TotalMatches.Home - teamStats.TotalWins.Home);
            }

            // Calculate as matches minus wins minus draws
            int homeDraws = CalculateHomeDraws(teamStats);
            return Math.Max(0, teamStats.TotalMatches.Home - teamStats.TotalWins.Home - homeDraws);
        }

        /// <summary>
        /// Calculates number of away losses
        /// </summary>
        private int CalculateAwayLosses(TeamStats teamStats)
        {
            if (teamStats == null || teamStats.TotalMatches == null ||
                teamStats.TotalWins == null || teamStats.TotalMatches.Away == 0)
            {
                return 0;
            }

            // If we have direct loss data
            if (teamStats.LossTotal > 0)
            {
                // If we have home/away breakdown
                if (teamStats.LossAway > 0)
                {
                    return teamStats.LossAway;
                }

                // Approximate from total
                return Math.Min(teamStats.LossTotal / 2, teamStats.TotalMatches.Away - teamStats.TotalWins.Away);
            }

            // Calculate as matches minus wins minus draws
            int awayDraws = CalculateAwayDraws(teamStats);
            return Math.Max(0, teamStats.TotalMatches.Away - teamStats.TotalWins.Away - awayDraws);
        }

        /// <summary>
        /// Calculates BTTS (Both Teams To Score) rate from team stats
        /// </summary>
        private double CalculateBttsRate(TeamStats teamStats)
        {
            if (teamStats == null || teamStats.Scoring == null ||
                teamStats.Scoring.BothTeamsScored == null || teamStats.TotalMatches == null ||
                teamStats.TotalMatches.Total == 0)
            {
                return 0;
            }

            return SafeDivide(teamStats.Scoring.BothTeamsScored.Total, teamStats.TotalMatches.Total) * 100;
        }

        /// <summary>
        /// Calculates home BTTS rate from team stats
        /// </summary>
        private double CalculateHomeBttsRate(TeamStats teamStats)
        {
            if (teamStats == null || teamStats.Scoring == null ||
                teamStats.Scoring.BothTeamsScored == null || teamStats.TotalMatches == null ||
                teamStats.TotalMatches.Home == 0)
            {
                return 0;
            }

            return SafeDivide(teamStats.Scoring.BothTeamsScored.Home, teamStats.TotalMatches.Home) * 100;
        }

        /// <summary>
        /// Calculates away BTTS rate from team stats
        /// </summary>
        private double CalculateAwayBttsRate(TeamStats teamStats)
        {
            if (teamStats == null || teamStats.Scoring == null ||
                teamStats.Scoring.BothTeamsScored == null || teamStats.TotalMatches == null ||
                teamStats.TotalMatches.Away == 0)
            {
                return 0;
            }

            return SafeDivide(teamStats.Scoring.BothTeamsScored.Away, teamStats.TotalMatches.Away) * 100;
        }

        /// <summary>
        /// Calculates first half goals percentage
        /// </summary>
        private double? CalculateFirstHalfGoalsPercent(TeamStats teamStats)
        {
            if (teamStats == null || teamStats.Scoring == null ||
                teamStats.Scoring.GoalsScored == null || teamStats.Scoring.GoalsScored.Total == 0)
            {
                return null;
            }

            // Estimate first half goals if available
            if (teamStats.Scoring.ScoringAtHalftime != null && teamStats.Scoring.ScoringAtHalftime.Total > 0)
            {
                return SafeDivide(teamStats.Scoring.ScoringAtHalftime.Total, teamStats.Scoring.GoalsScored.Total) * 100;
            }

            return null;
        }

        /// <summary>
        /// Calculates second half goals percentage
        /// </summary>
        private double? CalculateSecondHalfGoalsPercent(TeamStats teamStats)
        {
            if (teamStats == null || teamStats.Scoring == null ||
                teamStats.Scoring.GoalsScored == null || teamStats.Scoring.GoalsScored.Total == 0)
            {
                return null;
            }

            // Estimate second half goals if first half goals are available
            if (teamStats.Scoring.ScoringAtHalftime != null && teamStats.Scoring.ScoringAtHalftime.Total >= 0)
            {
                double firstHalfGoals = teamStats.Scoring.ScoringAtHalftime.Total;
                double totalGoals = teamStats.Scoring.GoalsScored.Total;
                double secondHalfGoals = totalGoals - firstHalfGoals;

                return SafeDivide(secondHalfGoals, totalGoals) * 100;
            }

            return null;
        }

        /// <summary>
        /// Calculates home matches with over 1.5 goals
        /// </summary>
        private int CalculateHomeMatchesOver15(Background.EnrichedSportMatch sportMatch, bool isHome)
        {
            // Find the right team's matches
            var teamLastX = isHome ? sportMatch.Team1LastX : sportMatch.Team2LastX;
            if (teamLastX == null || teamLastX.Matches == null)
                return 0;

            // Count home matches with over 1.5 total goals
            int homeMatchesOver15 = 0;

            string teamName = isHome
                ? sportMatch.OriginalMatch.Teams.Home.Name
                : sportMatch.OriginalMatch.Teams.Away.Name;

            foreach (var match in teamLastX.Matches)
            {
                if (match.Result == null || !match.Result.Home.HasValue || !match.Result.Away.HasValue)
                    continue;

                bool isTeamHome = match.Teams?.Home != null &&
                    (StringMatches(match.Teams.Home.Name, teamName) ||
                     StringMatches(match.Teams.Home.MediumName, teamName) ||
                     ContainsTeamName(match.Teams.Home.Name, teamName) ||
                     ContainsTeamName(match.Teams.Home.MediumName, teamName));

                if (isTeamHome)
                {
                    int totalGoals = match.Result.Home.Value + match.Result.Away.Value;
                    if (totalGoals > 1.5)
                    {
                        homeMatchesOver15++;
                    }
                }
            }

            return homeMatchesOver15;
        }

        /// <summary>
        /// Calculates away matches with over 1.5 goals
        /// </summary>
        private int CalculateAwayMatchesOver15(Background.EnrichedSportMatch sportMatch, bool isHome)
        {
            // Find the right team's matches
            var teamLastX = isHome ? sportMatch.Team1LastX : sportMatch.Team2LastX;
            if (teamLastX == null || teamLastX.Matches == null)
                return 0;

            // Count away matches with over 1.5 total goals
            int awayMatchesOver15 = 0;

            string teamName = isHome
                ? sportMatch.OriginalMatch.Teams.Home.Name
                : sportMatch.OriginalMatch.Teams.Away.Name;

            foreach (var match in teamLastX.Matches)
            {
                if (match.Result == null || !match.Result.Home.HasValue || !match.Result.Away.HasValue)
                    continue;

                bool isTeamAway = match.Teams?.Away != null &&
                    (StringMatches(match.Teams.Away.Name, teamName) ||
                     StringMatches(match.Teams.Away.MediumName, teamName) ||
                     ContainsTeamName(match.Teams.Away.Name, teamName) ||
                     ContainsTeamName(match.Teams.Away.MediumName, teamName));

                if (isTeamAway)
                {
                    int totalGoals = match.Result.Home.Value + match.Result.Away.Value;
                    if (totalGoals > 1.5)
                    {
                        awayMatchesOver15++;
                    }
                }
            }

            return awayMatchesOver15;
        }

        /// <summary>
        /// Calculates average corners per match
        /// </summary>
        private double? CalculateAvgCorners(Background.EnrichedSportMatch sportMatch, bool isHome)
        {
            var teamLastX = isHome ? sportMatch.Team1LastX : sportMatch.Team2LastX;
            if (teamLastX == null || teamLastX.Matches == null || !teamLastX.Matches.Any())
                return null;

            int totalCorners = 0;
            int matchesWithCorners = 0;

            foreach (var match in teamLastX.Matches)
            {
                if (match.Corners != null)
                {
                    totalCorners += match.Corners.Home + match.Corners.Away;
                    matchesWithCorners++;
                }
            }

            return matchesWithCorners > 0 ? (double)totalCorners / matchesWithCorners : null;
        }

        /// <summary>
        /// Calculates scoring first win rate
        /// </summary>
        private double? CalculateScoringFirstWinRate(Background.EnrichedSportMatch sportMatch, bool isHome)
        {
            var teamLastX = isHome ? sportMatch.Team1LastX : sportMatch.Team2LastX;
            if (teamLastX == null || teamLastX.Matches == null || !teamLastX.Matches.Any())
                return null;

            string teamName = isHome
                ? sportMatch.OriginalMatch.Teams.Home.Name
                : sportMatch.OriginalMatch.Teams.Away.Name;

            int scoredFirstCount = 0;
            int scoredFirstWinCount = 0;

            foreach (var match in teamLastX.Matches)
            {
                if (match.FirstGoal == null || match.Teams == null || match.Result == null)
                    continue;

                bool isTeamHome = match.Teams.Home != null &&
                    (StringMatches(match.Teams.Home.Name, teamName) ||
                     StringMatches(match.Teams.Home.MediumName, teamName) ||
                     ContainsTeamName(match.Teams.Home.Name, teamName) ||
                     ContainsTeamName(match.Teams.Home.MediumName, teamName));

                bool scoredFirst = (isTeamHome && match.FirstGoal == "home") ||
                                  (!isTeamHome && match.FirstGoal == "away");

                if (scoredFirst)
                {
                    scoredFirstCount++;

                    bool teamWon = (isTeamHome && match.Result.Home > match.Result.Away) ||
                                  (!isTeamHome && match.Result.Away > match.Result.Home);

                    if (teamWon)
                    {
                        scoredFirstWinCount++;
                    }
                }
            }

            return scoredFirstCount > 0 ? (double)scoredFirstWinCount / scoredFirstCount * 100 : null;
        }

        /// <summary>
        /// Calculates conceding first win rate
        /// </summary>
        private double? CalculateConcedingFirstWinRate(Background.EnrichedSportMatch sportMatch, bool isHome)
        {
            var teamLastX = isHome ? sportMatch.Team1LastX : sportMatch.Team2LastX;
            if (teamLastX == null || teamLastX.Matches == null || !teamLastX.Matches.Any())
                return null;

            string teamName = isHome
                ? sportMatch.OriginalMatch.Teams.Home.Name
                : sportMatch.OriginalMatch.Teams.Away.Name;

            int concededFirstCount = 0;
            int concededFirstWinCount = 0;

            foreach (var match in teamLastX.Matches)
            {
                if (match.FirstGoal == null || match.Teams == null || match.Result == null)
                    continue;

                bool isTeamHome = match.Teams.Home != null &&
                    (StringMatches(match.Teams.Home.Name, teamName) ||
                     StringMatches(match.Teams.Home.MediumName, teamName) ||
                     ContainsTeamName(match.Teams.Home.Name, teamName) ||
                     ContainsTeamName(match.Teams.Home.MediumName, teamName));

                bool concededFirst = (isTeamHome && match.FirstGoal == "away") ||
                                    (!isTeamHome && match.FirstGoal == "home");

                if (concededFirst)
                {
                    concededFirstCount++;

                    bool teamWon = (isTeamHome && match.Result.Home > match.Result.Away) ||
                                  (!isTeamHome && match.Result.Away > match.Result.Home);

                    if (teamWon)
                    {
                        concededFirstWinCount++;
                    }
                }
            }

            return concededFirstCount > 0 ? (double)concededFirstWinCount / concededFirstCount * 100 : null;
        }

        /// <summary>
        /// Calculates late goal rate (goals after 75th minute)
        /// </summary>
        private double? CalculateLateGoalRate(Background.EnrichedSportMatch sportMatch, bool isHome)
        {
            // For this we need detailed timeline data that might not be available
            // This is an approximation - in a real-world implementation, you'd extract this from actual match timeline

            var teamLastX = isHome ? sportMatch.Team1LastX : sportMatch.Team2LastX;
            if (teamLastX == null || teamLastX.Matches == null || !teamLastX.Matches.Any())
                return null;

            // Look for the 'lastgoal' field which might indicate late goals
            int totalMatches = 0;
            int lateGoalMatches = 0;

            foreach (var match in teamLastX.Matches)
            {
                if (match.Result == null || match.Teams == null)
                    continue;

                totalMatches++;

                // If lastgoal field corresponds to the team, it may indicate a late goal
                string teamName = isHome
                    ? sportMatch.OriginalMatch.Teams.Home.Name
                    : sportMatch.OriginalMatch.Teams.Away.Name;

                bool isTeamHome = match.Teams.Home != null &&
                    (StringMatches(match.Teams.Home.Name, teamName) ||
                     StringMatches(match.Teams.Home.MediumName, teamName) ||
                     ContainsTeamName(match.Teams.Home.Name, teamName) ||
                     ContainsTeamName(match.Teams.Home.MediumName, teamName));

                if ((isTeamHome && match.LastGoal == "home") ||
                    (!isTeamHome && match.LastGoal == "away"))
                {
                    // Assume this was a late goal (simplification)
                    lateGoalMatches++;
                }
            }

            return totalMatches > 0 ? (double)lateGoalMatches / totalMatches * 100 : null;
        }

        /// <summary>
        /// Calculates the league's average goals per match
        /// </summary>
        private double CalculateLeagueAvgGoals(Background.EnrichedSportMatch sportMatch)
        {
            // Try to calculate from the table data if available
            if (sportMatch.TeamTableSlice != null &&
                sportMatch.TeamTableSlice.TableRows != null &&
                sportMatch.TeamTableSlice.TableRows.Count > 0)
            {
                try
                {
                    int totalGoalsFor = 0;
                    int totalMatches = 0;

                    foreach (var row in sportMatch.TeamTableSlice.TableRows)
                    {
                        if (row.GoalsForTotal > 0)
                        {
                            totalGoalsFor += row.GoalsForTotal;
                            totalMatches += row.Total;
                        }
                    }

                    // Each goal is counted twice (once as scored, once as conceded)
                    // So total goals = totalGoalsFor
                    if (totalMatches > 0)
                    {
                        return Math.Round((double)totalGoalsFor / totalMatches, 2);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calculating league average goals: {Message}", ex.Message);
                }
            }

            // Fallback to standard value
            return 2.5;
        }

        /// <summary>
        /// Calculates team possession (not directly available in data, using estimates)
        /// </summary>
        private double CalculatePossession(Background.EnrichedSportMatch sportMatch, bool isHome)
        {
            // Possession data is not directly available in the provided format
            // We'll estimate based on other metrics
            try
            {
                var teamStats = isHome
                    ? sportMatch.Team1ScoringConceding?.Stats
                    : sportMatch.Team2ScoringConceding?.Stats;

                if (teamStats == null)
                    return 50.0; // Default to even possession

                // Calculate possession based on goals scored/conceded ratio
                double goalsScored = teamStats.Scoring?.GoalsScored?.Total ?? 0;
                double goalsConceded = teamStats.Conceding?.GoalsConceded?.Total ?? 0;

                if (goalsScored == 0 && goalsConceded == 0)
                    return 50.0;

                double scoringRatio = SafeDivide(goalsScored, goalsScored + goalsConceded);

                // Convert to possession estimate (teams that score more tend to have more possession)
                double possession = 40 + (scoringRatio * 20); // Scale to 40-60% range

                // Add home advantage for possession
                if (isHome)
                    possession += 3;
                else
                    possession -= 3;

                // Cap within reasonable bounds
                return Math.Min(Math.Max(possession, 35), 65);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating possession: {Message}", ex.Message);
                return 50.0;
            }
        }

        /// <summary>
        /// Extracts goal distribution data from team stats
        /// </summary>
        private Dictionary<string, object> ExtractGoalDistribution(TeamStats teamStats)
        {
            var distribution = new Dictionary<string, object>();

            try
            {
                if (teamStats?.Scoring?.GoalsByMinutes != null)
                {
                    // Copy the goals by minutes data directly
                    foreach (var kvp in teamStats.Scoring.GoalsByMinutes)
                    {
                        var minuteData = new Dictionary<string, double>
                        {
                            { "total", kvp.Value.Total },
                            { "home", kvp.Value.Home },
                            { "away", kvp.Value.Away }
                        };

                        distribution[kvp.Key] = minuteData;
                    }
                }
                else if (teamStats?.AverageGoalsByMinutes != null)
                {
                    // Convert average goals by minutes to a distribution
                    foreach (var kvp in teamStats.AverageGoalsByMinutes)
                    {
                        var minuteData = new Dictionary<string, double>
                        {
                            { "total", kvp.Value.Total },
                            { "home", kvp.Value.Home },
                            { "away", kvp.Value.Away }
                        };

                        distribution[kvp.Key] = minuteData;
                    }
                }
                else
                {
                    // Create default distribution if no data available
                    distribution = CreateDefaultGoalDistribution();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting goal distribution: {Message}", ex.Message);

                // Return a default distribution in case of error
                distribution = CreateDefaultGoalDistribution();
            }

            return distribution;
        }

        /// <summary>
        /// Creates a default goal distribution when data is not available
        /// </summary>
        private Dictionary<string, object> CreateDefaultGoalDistribution()
        {
            return new Dictionary<string, object>
            {
                ["0-15"] = new Dictionary<string, double> { { "total", 0 }, { "home", 0 }, { "away", 0 } },
                ["16-30"] = new Dictionary<string, double> { { "total", 0 }, { "home", 0 }, { "away", 0 } },
                ["31-45"] = new Dictionary<string, double> { { "total", 0 }, { "home", 0 }, { "away", 0 } },
                ["46-60"] = new Dictionary<string, double> { { "total", 0 }, { "home", 0 }, { "away", 0 } },
                ["61-75"] = new Dictionary<string, double> { { "total", 0 }, { "home", 0 }, { "away", 0 } },
                ["76-90"] = new Dictionary<string, double> { { "total", 0 }, { "home", 0 }, { "away", 0 } }
            };
        }

        /// <summary>
        /// Calculates points against top teams
        /// </summary>
        private double? CalculateAgainstTopTeamsPoints(Background.EnrichedSportMatch sportMatch, bool isHome)
        {
            // This is a complex calculation that requires knowledge of which teams are considered "top"
            // Without specific data marking teams as "top", we can estimate based on league position
            try
            {
                if (sportMatch.TeamTableSlice == null ||
                    sportMatch.TeamTableSlice.TableRows == null ||
                    sportMatch.TeamTableSlice.TableRows.Count < 6)
                {
                    return null;
                }

                // Consider top 3 teams in the table as "top teams"
                var topTeamIds = sportMatch.TeamTableSlice.TableRows
                    .OrderBy(r => r.Pos)
                    .Take(3)
                    .Select(r => r.Team?.Id)
                    .Where(id => !string.IsNullOrEmpty(id))
                    .ToHashSet();

                if (topTeamIds.Count == 0)
                    return null;

                // Get team's matches
                var teamLastX = isHome ? sportMatch.Team1LastX : sportMatch.Team2LastX;
                if (teamLastX == null || teamLastX.Matches == null || !teamLastX.Matches.Any())
                    return null;

                string teamName = isHome
                    ? sportMatch.OriginalMatch.Teams.Home.Name
                    : sportMatch.OriginalMatch.Teams.Away.Name;

                int matchesVsTop = 0;
                int pointsVsTop = 0;

                foreach (var match in teamLastX.Matches)
                {
                    if (match.Result == null || match.Teams == null)
                        continue;

                    bool isTeamHome = match.Teams.Home != null &&
                        (StringMatches(match.Teams.Home.Name, teamName) ||
                         StringMatches(match.Teams.Home.MediumName, teamName));

                    // Check if opponent is a top team
                    string opponentId = isTeamHome
                        ? match.Teams.Away?.Id
                        : match.Teams.Home?.Id;

                    if (string.IsNullOrEmpty(opponentId) || !topTeamIds.Contains(opponentId))
                        continue;

                    matchesVsTop++;

                    // Calculate points
                    if (match.Result.Winner == null) // Draw
                    {
                        pointsVsTop += 1;
                    }
                    else if ((isTeamHome && match.Result.Winner == "home") ||
                             (!isTeamHome && match.Result.Winner == "away"))
                    {
                        pointsVsTop += 3;
                    }
                }

                return matchesVsTop > 0 ? Math.Round((double)pointsVsTop / matchesVsTop, 2) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating points against top teams: {Message}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Calculates points against mid-table teams
        /// </summary>
        private double? CalculateAgainstMidTeamsPoints(Background.EnrichedSportMatch sportMatch, bool isHome)
        {
            try
            {
                if (sportMatch.TeamTableSlice == null ||
                    sportMatch.TeamTableSlice.TableRows == null ||
                    sportMatch.TeamTableSlice.TableRows.Count < 10)
                {
                    return null;
                }

                // Determine what constitutes "mid-table" teams based on table size
                int tableSize = sportMatch.TeamTableSlice.TableRows.Count;
                int midStart = Math.Max(4, tableSize / 3);
                int midEnd = Math.Min(tableSize - 4, 2 * tableSize / 3);

                // Identify mid-table teams
                var midTeamIds = sportMatch.TeamTableSlice.TableRows
                    .Where(r => r.Pos >= midStart && r.Pos <= midEnd)
                    .Select(r => r.Team?.Id)
                    .Where(id => !string.IsNullOrEmpty(id))
                    .ToHashSet();

                if (midTeamIds.Count == 0)
                    return null;

                // Get team's matches
                var teamLastX = isHome ? sportMatch.Team1LastX : sportMatch.Team2LastX;
                if (teamLastX == null || teamLastX.Matches == null || !teamLastX.Matches.Any())
                    return null;

                string teamName = isHome
                    ? sportMatch.OriginalMatch.Teams.Home.Name
                    : sportMatch.OriginalMatch.Teams.Away.Name;

                int matchesVsMid = 0;
                int pointsVsMid = 0;

                foreach (var match in teamLastX.Matches)
                {
                    if (match.Result == null || match.Teams == null)
                        continue;

                    bool isTeamHome = match.Teams.Home != null &&
                        (StringMatches(match.Teams.Home.Name, teamName) ||
                         StringMatches(match.Teams.Home.MediumName, teamName));

                    // Check if opponent is a mid-table team
                    string opponentId = isTeamHome
                        ? match.Teams.Away?.Id
                        : match.Teams.Home?.Id;

                    if (string.IsNullOrEmpty(opponentId) || !midTeamIds.Contains(opponentId))
                        continue;

                    matchesVsMid++;

                    // Calculate points
                    if (match.Result.Winner == null) // Draw
                    {
                        pointsVsMid += 1;
                    }
                    else if ((isTeamHome && match.Result.Winner == "home") ||
                             (!isTeamHome && match.Result.Winner == "away"))
                    {
                        pointsVsMid += 3;
                    }
                }

                return matchesVsMid > 0 ? Math.Round((double)pointsVsMid / matchesVsMid, 2) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating points against mid-table teams: {Message}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Calculates points against bottom teams
        /// </summary>
        private double? CalculateAgainstBottomTeamsPoints(Background.EnrichedSportMatch sportMatch, bool isHome)
        {
            try
            {
                if (sportMatch.TeamTableSlice == null ||
                    sportMatch.TeamTableSlice.TableRows == null ||
                    sportMatch.TeamTableSlice.TableRows.Count < 6)
                {
                    return null;
                }

                // Consider bottom 3 teams in the table as "bottom teams"
                var bottomTeamIds = sportMatch.TeamTableSlice.TableRows
                    .OrderByDescending(r => r.Pos)
                    .Take(3)
                    .Select(r => r.Team?.Id)
                    .Where(id => !string.IsNullOrEmpty(id))
                    .ToHashSet();

                if (bottomTeamIds.Count == 0)
                    return null;

                // Get team's matches
                var teamLastX = isHome ? sportMatch.Team1LastX : sportMatch.Team2LastX;
                if (teamLastX == null || teamLastX.Matches == null || !teamLastX.Matches.Any())
                    return null;

                string teamName = isHome
                    ? sportMatch.OriginalMatch.Teams.Home.Name
                    : sportMatch.OriginalMatch.Teams.Away.Name;

                int matchesVsBottom = 0;
                int pointsVsBottom = 0;

                foreach (var match in teamLastX.Matches)
                {
                    if (match.Result == null || match.Teams == null)
                        continue;

                    bool isTeamHome = match.Teams.Home != null &&
                        (StringMatches(match.Teams.Home.Name, teamName) ||
                         StringMatches(match.Teams.Home.MediumName, teamName));

                    // Check if opponent is a bottom team
                    string opponentId = isTeamHome
                        ? match.Teams.Away?.Id
                        : match.Teams.Home?.Id;

                    if (string.IsNullOrEmpty(opponentId) || !bottomTeamIds.Contains(opponentId))
                        continue;

                    matchesVsBottom++;

                    // Calculate points
                    if (match.Result.Winner == null) // Draw
                    {
                        pointsVsBottom += 1;
                    }
                    else if ((isTeamHome && match.Result.Winner == "home") ||
                             (!isTeamHome && match.Result.Winner == "away"))
                    {
                        pointsVsBottom += 3;
                    }
                }

                return matchesVsBottom > 0 ? Math.Round((double)pointsVsBottom / matchesVsBottom, 2) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating points against bottom teams: {Message}", ex.Message);
                return null;
            }
        }

        #endregion
    }

    #region Prediction Data Models

    public class PredictionDataResponse
    {
        [JsonPropertyName("data")]
        public PredictionData Data { get; set; }

        [JsonPropertyName("pagination")]
        public PaginationInfo Pagination { get; set; }
    }

    public class PredictionData
    {
        [JsonPropertyName("upcomingMatches")]
        public List<UpcomingMatch> UpcomingMatches { get; set; }

        [JsonPropertyName("metadata")]
        public PredictionMetadata Metadata { get; set; }
    }

    public class PredictionMetadata
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("date")]
        public string Date { get; set; }

        [JsonPropertyName("leagueData")]
        public Dictionary<string, LeagueMetadata> LeagueData { get; set; }
    }

    public class LeagueMetadata
    {
        [JsonPropertyName("matches")]
        public int Matches { get; set; }

        [JsonPropertyName("totalGoals")]
        public int TotalGoals { get; set; }

        [JsonPropertyName("homeWinRate")]
        public double HomeWinRate { get; set; }

        [JsonPropertyName("drawRate")]
        public double DrawRate { get; set; }

        [JsonPropertyName("awayWinRate")]
        public double AwayWinRate { get; set; }

        [JsonPropertyName("bttsRate")]
        public double BttsRate { get; set; }
    }

    public class PaginationInfo
    {
        [JsonPropertyName("currentPage")]
        public int CurrentPage { get; set; }

        [JsonPropertyName("totalPages")]
        public int TotalPages { get; set; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }

        [JsonPropertyName("totalItems")]
        public int TotalItems { get; set; }

        [JsonPropertyName("hasNext")]
        public bool HasNext { get; set; }

        [JsonPropertyName("hasPrevious")]
        public bool HasPrevious { get; set; }
    }

    public class UpcomingMatch
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
        public int ConfidenceScore { get; set; }

        [JsonPropertyName("averageGoals")]
        public double AverageGoals { get; set; }

        [JsonPropertyName("expectedGoals")]
        public double ExpectedGoals { get; set; }

        [JsonPropertyName("defensiveStrength")]
        public double DefensiveStrength { get; set; }

        [JsonPropertyName("odds")]
        public MatchOdds Odds { get; set; }

        [JsonPropertyName("headToHead")]
        public HeadToHeadData HeadToHead { get; set; }

        [JsonPropertyName("cornerStats")]
        public CornerStats CornerStats { get; set; }

        [JsonPropertyName("scoringPatterns")]
        public ScoringPatterns ScoringPatterns { get; set; }

        [JsonPropertyName("reasonsForPrediction")]
        public List<string> ReasonsForPrediction { get; set; }
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
        public double AvgHomeGoals { get; set; }

        [JsonPropertyName("avgAwayGoals")]
        public double AvgAwayGoals { get; set; }

        [JsonPropertyName("avgTotalGoals")]
        public double AvgTotalGoals { get; set; }

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
        public double? ScoringFirstWinRate { get; set; }

        [JsonPropertyName("concedingFirstWinRate")]
        public double? ConcedingFirstWinRate { get; set; }

        [JsonPropertyName("firstHalfGoalsPercent")]
        public double? FirstHalfGoalsPercent { get; set; }

        [JsonPropertyName("secondHalfGoalsPercent")]
        public double? SecondHalfGoalsPercent { get; set; }

        [JsonPropertyName("avgCorners")]
        public double? AvgCorners { get; set; }

        [JsonPropertyName("bttsRate")]
        public double? BttsRate { get; set; }

        [JsonPropertyName("homeBttsRate")]
        public double? HomeBttsRate { get; set; }

        [JsonPropertyName("awayBttsRate")]
        public double? AwayBttsRate { get; set; }

        [JsonPropertyName("lateGoalRate")]
        public double? LateGoalRate { get; set; }

        [JsonPropertyName("goalDistribution")]
        public Dictionary<string, object> GoalDistribution { get; set; }

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

    public class MatchOdds
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
        public List<RecentMatchResult> RecentMatches { get; set; }
    }

    public class RecentMatchResult
    {
        [JsonPropertyName("date")]
        public string Date { get; set; }

        [JsonPropertyName("result")]
        public string Result { get; set; }
    }

    public class CornerStats
    {
        [JsonPropertyName("homeAvg")]
        public double HomeAvg { get; set; }

        [JsonPropertyName("awayAvg")]
        public double AwayAvg { get; set; }

        [JsonPropertyName("totalAvg")]
        public double TotalAvg { get; set; }
    }

    public class ScoringPatterns
    {
        [JsonPropertyName("homeFirstGoalRate")]
        public double HomeFirstGoalRate { get; set; }

        [JsonPropertyName("awayFirstGoalRate")]
        public double AwayFirstGoalRate { get; set; }

        [JsonPropertyName("homeLateGoalRate")]
        public double HomeLateGoalRate { get; set; }

        [JsonPropertyName("awayLateGoalRate")]
        public double AwayLateGoalRate { get; set; }
    }
}

#endregion