using System.Collections.Concurrent;
using fredapi.Database;
using fredapi.SportRadarService.Background.ArbitrageLiveMatchBackgroundService;
using fredapi.SportRadarService.Transformers;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace fredapi.Utils;

/// <summary>
/// Service to enrich live match data with prediction data and collect historical snapshots for ML.
/// </summary>
public class PredictionEnrichedMatchService
{
    private readonly ILogger<PredictionEnrichedMatchService> _logger;
    private readonly MongoDbService _mongoDbService;
    private readonly IMemoryCache _cache;
    
    // Constants for cache keys
    private const string CACHE_KEY_ENRICHED_MATCH = "prediction_enriched_match_";
    private const string CACHE_KEY_MATCH_SNAPSHOTS = "match_snapshots_";
    
    // Concurrent dictionary to track snapshots per match
    // This enables safe collection of multiple snapshots per match from different threads
    private readonly ConcurrentDictionary<int, ConcurrentBag<MatchSnapshot>> _matchSnapshots = new();
    
    // Semaphore for controlling DB access
    private static readonly SemaphoreSlim _dbSemaphore = new(1, 1);

    public PredictionEnrichedMatchService(
        ILogger<PredictionEnrichedMatchService> logger,
        MongoDbService mongoDbService,
        IMemoryCache cache)
    {
        _logger = logger;
        _mongoDbService = mongoDbService;
        _cache = cache;

        // Initialize collections if needed
        InitializeCollections().GetAwaiter().GetResult();
    }
    
    /// <summary>
    /// Create necessary collections if they don't exist
    /// </summary>
    private async Task InitializeCollections()
    {
        try
        {
            // Create time-to-live index on snapshots collection
            var snapshotsCollection = _mongoDbService.GetCollection<MatchSnapshot>("MatchSnapshots");
            var indexKeys = Builders<MatchSnapshot>.IndexKeys.Ascending(x => x.Timestamp);
            var indexOptions = new CreateIndexOptions 
            { 
                ExpireAfter = TimeSpan.FromDays(30), 
                Name = "TTL_Timestamp" 
            };
            
            await snapshotsCollection.Indexes.CreateOneAsync(
                new CreateIndexModel<MatchSnapshot>(indexKeys, indexOptions));
            
            // Create index on match ID for efficient querying
            var matchIdIndex = Builders<MatchSnapshot>.IndexKeys.Ascending(x => x.MatchId);
            await snapshotsCollection.Indexes.CreateOneAsync(
                new CreateIndexModel<MatchSnapshot>(matchIdIndex, new CreateIndexOptions { Name = "IX_MatchId" }));
            
            _logger.LogInformation("Successfully initialized collections and indexes for prediction-enriched match service");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing collections for prediction-enriched match service");
        }
    }
    
    /// <summary>
    /// Enriches live matches with prediction data for both arbitrage opportunities and all matches
    /// </summary>
    public async Task<(List<ClientMatch> enrichedArbitrageMatches, List<ClientMatch> enrichedAllMatches)> 
        EnrichMatchesWithPredictionDataAsync(
            List<Match> arbitrageMatches, 
            List<Match> allMatches)
    {
        try
        {
            // Get cached prediction data if available
            if (!_cache.TryGetValue("prediction_data", out PredictionDataResponse predictionData))
            {
                _logger.LogInformation("No prediction data available in cache");
                // Return original matches if no prediction data is available
                return (
                    arbitrageMatches.Select(m => ConvertToClientMatch(m)).ToList(),
                    allMatches.Select(m => ConvertToClientMatch(m)).ToList()
                );
            }
            
            _logger.LogInformation($"Enriching {allMatches.Count} matches with prediction data");
            
            // Create enriched versions of all matches
            var enrichedAllMatches = await EnrichMatchesAsync(allMatches, predictionData);
            
            // Create enriched versions of arbitrage matches
            var arbitrageMatchIds = arbitrageMatches.Select(m => m.Id).ToHashSet();
            var enrichedArbitrageMatches = enrichedAllMatches
                .Where(m => arbitrageMatchIds.Contains(m.Id))
                .ToList();
            
            // Take a snapshot of all matches for ML purposes
            await TakeMatchSnapshotsAsync(enrichedAllMatches);
            
            return (enrichedArbitrageMatches, enrichedAllMatches);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enriching matches with prediction data");
            // Return original matches on error
            return (
                arbitrageMatches.Select(m => ConvertToClientMatch(m)).ToList(),
                allMatches.Select(m => ConvertToClientMatch(m)).ToList()
            );
        }
    }
    
    private async Task<List<ClientMatch>> EnrichMatchesAsync(
        List<Match> matches, 
        PredictionDataResponse predictionData)
    {
        var enrichedMatches = new List<ClientMatch>(matches.Count);
        
        foreach (var match in matches)
        {
            try
            {
                // Check if we have a cached version first
                var cacheKey = $"{CACHE_KEY_ENRICHED_MATCH}{match.Id}";
                if (_cache.TryGetValue(cacheKey, out ClientMatch cachedMatch))
                {
                    // Update live data (score, time, etc.) with latest values
                    UpdateLiveData(cachedMatch, match);
                    enrichedMatches.Add(cachedMatch);
                    continue;
                }
                
                // Convert to client match
                var clientMatch = ConvertToClientMatch(match);
                
                // Find corresponding prediction data if available
                var matchPrediction = FindMatchPrediction(match, predictionData);
                if (matchPrediction != null)
                {
                    // Enrich with prediction data
                    clientMatch.PredictionData = new ClientMatchPredictionData
                    {
                        Favorite = matchPrediction.Favorite,
                        ConfidenceScore = matchPrediction.ConfidenceScore,
                        AverageGoals = matchPrediction.AverageGoals,
                        ExpectedGoals = matchPrediction.ExpectedGoals,
                        DefensiveStrength = matchPrediction.DefensiveStrength,
                        CornerStats = new ClientCornerStats
                        {
                            HomeAvg = matchPrediction.CornerStats?.HomeAvg ?? 0,
                            AwayAvg = matchPrediction.CornerStats?.AwayAvg ?? 0,
                            TotalAvg = matchPrediction.CornerStats?.TotalAvg ?? 0
                        },
                        ScoringPatterns = new ClientScoringPatterns
                        {
                            HomeFirstGoalRate = matchPrediction.ScoringPatterns?.HomeFirstGoalRate ?? 0,
                            AwayFirstGoalRate = matchPrediction.ScoringPatterns?.AwayFirstGoalRate ?? 0,
                            HomeLateGoalRate = matchPrediction.ScoringPatterns?.HomeLateGoalRate ?? 0, 
                            AwayLateGoalRate = matchPrediction.ScoringPatterns?.AwayLateGoalRate ?? 0
                        },
                        ReasonsForPrediction = matchPrediction.ReasonsForPrediction?.ToList() ?? new List<string>(),
                        HeadToHead = new ClientHeadToHeadData
                        {
                            Matches = matchPrediction.HeadToHead?.Matches ?? 0,
                            Wins = matchPrediction.HeadToHead?.Wins ?? 0,
                            Draws = matchPrediction.HeadToHead?.Draws ?? 0,
                            Losses = matchPrediction.HeadToHead?.Losses ?? 0,
                            GoalsScored = matchPrediction.HeadToHead?.GoalsScored ?? 0,
                            GoalsConceded = matchPrediction.HeadToHead?.GoalsConceded ?? 0,
                            RecentMatches = matchPrediction.HeadToHead?.RecentMatches
                                ?.Select(r => new ClientRecentMatchResult
                                {
                                    Date = r.Date,
                                    Result = r.Result
                                })
                                .ToList() ?? new List<ClientRecentMatchResult>()
                        },
                        HomeTeamData = ConvertTeamData(matchPrediction.HomeTeam),
                        AwayTeamData = ConvertTeamData(matchPrediction.AwayTeam)
                    };
                    
                    // Cache the enriched match for 5 minutes
                    _cache.Set(cacheKey, clientMatch, TimeSpan.FromMinutes(5));
                }
                
                enrichedMatches.Add(clientMatch);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error enriching match {match.Id}");
                // Add original match conversion on error
                enrichedMatches.Add(ConvertToClientMatch(match));
            }
        }
        
        return enrichedMatches;
    }

    /// <summary>
    /// Finds prediction data for a specific match
    /// </summary>
    private UpcomingMatch FindMatchPrediction(Match match, PredictionDataResponse predictionData)
    {
        if (predictionData?.Data?.UpcomingMatches == null || !predictionData.Data.UpcomingMatches.Any())
            return null;
            
        // Clean team names for better matching
        string cleanHomeTeam = CleanTeamName(match.Teams.Home.Name);
        string cleanAwayTeam = CleanTeamName(match.Teams.Away.Name);
        
        // Try to find by team names (exact match)
        var prediction = predictionData.Data.UpcomingMatches.FirstOrDefault(p => 
            CleanTeamName(p.HomeTeam.Name) == cleanHomeTeam && 
            CleanTeamName(p.AwayTeam.Name) == cleanAwayTeam);
            
        if (prediction != null)
            return prediction;
            
        // Try more lenient matching (contains)
        prediction = predictionData.Data.UpcomingMatches.FirstOrDefault(p => 
            (CleanTeamName(p.HomeTeam.Name).Contains(cleanHomeTeam) || cleanHomeTeam.Contains(CleanTeamName(p.HomeTeam.Name))) && 
            (CleanTeamName(p.AwayTeam.Name).Contains(cleanAwayTeam) || cleanAwayTeam.Contains(CleanTeamName(p.AwayTeam.Name))));
            
        return prediction;
    }
    
    private string CleanTeamName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return string.Empty;
            
        // Remove suffixes like FC, United, etc.
        var cleanName = name.Replace("FC", "").Replace("United", "").Replace("City", "");
        // Remove non-alphanumeric characters and trim
        return new string(cleanName.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)).ToArray()).Trim();
    }
    
    private ClientTeamData ConvertTeamData(TeamData teamData)
    {
        if (teamData == null)
            return null;
            
        return new ClientTeamData
        {
            Name = teamData.Name,
            Position = teamData.Position,
            Form = teamData.Form,
            HomeForm = teamData.HomeForm,
            AwayForm = teamData.AwayForm,
            AvgHomeGoals = teamData.AvgHomeGoals,
            AvgAwayGoals = teamData.AvgAwayGoals,
            AvgTotalGoals = teamData.AvgTotalGoals,
            AverageGoalsScored = teamData.AverageGoalsScored,
            AverageGoalsConceded = teamData.AverageGoalsConceded,
            CleanSheets = teamData.CleanSheets,
            HomeCleanSheets = teamData.HomeCleanSheets,
            AwayCleanSheets = teamData.AwayCleanSheets,
            ScoringFirstWinRate = teamData.ScoringFirstWinRate ?? 0,
            WinPercentage = teamData.WinPercentage,
            HomeWinPercentage = teamData.HomeWinPercentage,
            AwayWinPercentage = teamData.AwayWinPercentage
        };
    }
    
    private void UpdateLiveData(ClientMatch cachedMatch, Match liveMatch)
    {
        // Update live data fields
        cachedMatch.Score = liveMatch.Score;
        cachedMatch.Period = liveMatch.Period;
        cachedMatch.MatchStatus = liveMatch.MatchStatus;
        cachedMatch.PlayedTime = liveMatch.PlayedTime;
        cachedMatch.LastUpdated = DateTime.UtcNow;
        
        // Update match situation and details
        cachedMatch.MatchSituation = liveMatch.MatchSituation;
        cachedMatch.MatchDetails = liveMatch.MatchDetails;
    }
    
    private ClientMatch ConvertToClientMatch(Match match)
    {
        return new ClientMatch
        {
            Id = match.Id,
            SeasonId = match.SeasonId,
            Teams = new ClientTeams
            {
                Home = new ClientTeam { Id = match.Teams.Home.Id, Name = match.Teams.Home.Name },
                Away = new ClientTeam { Id = match.Teams.Away.Id, Name = match.Teams.Away.Name }
            },
            TournamentName = match.TournamentName,
            Score = match.Score,
            Period = match.Period,
            MatchStatus = match.MatchStatus,
            PlayedTime = match.PlayedTime,
            Markets = match.Markets.Select(m => new ClientMarket
            {
                Id = m.Id,
                Description = m.Description,
                Specifier = m.Specifier,
                Margin = m.Margin,
                Favourite = m.Favourite,
                ProfitPercentage = m.ProfitPercentage,
                Outcomes = m.Outcomes.Select(o => new ClientOutcome
                {
                    Id = o.Id,
                    Description = o.Description,
                    Odds = o.Odds,
                    StakePercentage = o.StakePercentage
                }).ToList()
            }).ToList(),
            LastUpdated = DateTime.UtcNow,
            MatchSituation = match.MatchSituation,
            MatchDetails = match.MatchDetails
        };
    }
    
    /// <summary>
    /// Takes snapshots of all matches for ML purposes
    /// </summary>
    private async Task TakeMatchSnapshotsAsync(List<ClientMatch> matches)
    {
        try
        {
            // Group snapshots by match ID
            var newSnapshots = new List<MatchSnapshot>();
            var timestamp = DateTime.UtcNow;
            
            foreach (var match in matches)
            {
               
                // Create snapshot
                var snapshot = new MatchSnapshot
                {
                    Id = ObjectId.GenerateNewId(),
                    MatchId = match.Id,
                    Timestamp = timestamp,
                    Score = match.Score,
                    Period = match.Period,
                    MatchStatus = match.MatchStatus,
                    PlayedTime = match.PlayedTime,
                    MatchSituation = match.MatchSituation,
                    MatchDetails = match.MatchDetails,
                    PredictionData = match.PredictionData
                };
                
                // Add to collection
                if (!_matchSnapshots.TryGetValue(match.Id, out var snapshots))
                {
                    snapshots = new ConcurrentBag<MatchSnapshot>();
                    _matchSnapshots[match.Id] = snapshots;
                }
                
                snapshots.Add(snapshot);
                newSnapshots.Add(snapshot);
            }
            
            // Only save to DB if we have snapshots
            if (newSnapshots.Count > 0)
            {
                // Save snapshots in the background to avoid blocking
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await SaveSnapshotsToDbAsync(newSnapshots);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving match snapshots to database");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error taking match snapshots");
        }
    }
    
    /// <summary>
    /// Saves snapshots to database
    /// </summary>
    private async Task SaveSnapshotsToDbAsync(List<MatchSnapshot> snapshots)
    {
        try
        {
            await _dbSemaphore.WaitAsync();
            
            // Get collection
            var collection = _mongoDbService.GetCollection<MatchSnapshot>("MatchSnapshots");
            
            // Batch by 100 snapshots for efficient writes
            foreach (var batch in snapshots.Chunk(100))
            {
                try
                {
                    await collection.InsertManyAsync(batch);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error saving batch of {batch.Length} snapshots");
                }
            }
            
            _logger.LogInformation($"Saved {snapshots.Count} match snapshots to database");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SaveSnapshotsToDbAsync");
        }
        finally
        {
            _dbSemaphore.Release();
        }
    }
    
    /// <summary>
    /// Gets all snapshots for a specific match
    /// </summary>
    public async Task<List<MatchSnapshot>> GetMatchSnapshotsAsync(int matchId)
    {
        // Check in-memory cache first
        if (_matchSnapshots.TryGetValue(matchId, out var snapshots))
        {
            return snapshots.OrderBy(s => s.Timestamp).ToList();
        }
        
        // If not in memory, check database
        try
        {
            var collection = _mongoDbService.GetCollection<MatchSnapshot>("MatchSnapshots");
            var filter = Builders<MatchSnapshot>.Filter.Eq(s => s.MatchId, matchId);
            var sort = Builders<MatchSnapshot>.Sort.Ascending(s => s.Timestamp);
            
            return await collection.Find(filter).Sort(sort).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting snapshots for match {matchId}");
            return new List<MatchSnapshot>();
        }
    }
    
    /// <summary>
    /// Gets all available snapshots for all matches
    /// </summary>
    public async Task<List<MatchSnapshot>> GetAllMatchSnapshotsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var collection = _mongoDbService.GetCollection<MatchSnapshot>("MatchSnapshots");
            var filter = Builders<MatchSnapshot>.Filter.Empty;
            
            if (startDate.HasValue)
            {
                filter = Builders<MatchSnapshot>.Filter.Gte(s => s.Timestamp, startDate.Value);
            }
            
            if (endDate.HasValue)
            {
                filter = filter & Builders<MatchSnapshot>.Filter.Lte(s => s.Timestamp, endDate.Value);
            }
            
            return await collection.Find(filter).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all match snapshots");
            return new List<MatchSnapshot>();
        }
    }
    
    /// <summary>
    /// Exports match data to CSV for machine learning
    /// </summary>
/// <summary>
    /// Exports match data to CSV for machine learning with 9 timeline snapshots
    /// </summary>
    public async Task<string> ExportMatchDataToCsvAsync(int matchId)
    {
        var snapshots = await GetMatchSnapshotsAsync(matchId);
        
        if (!snapshots.Any())
        {
            return "No data available for this match";
        }
        
        // Create CSV header
        var sb = new System.Text.StringBuilder();
        
        // Basic match info
        sb.Append("timestamp,match_id,time_segment,");
        
        // Match state
        sb.Append("score,period,match_status,played_time,");
        
        // Match statistics
        sb.Append("home_dangerous_attacks,away_dangerous_attacks,");
        sb.Append("home_safe_attacks,away_safe_attacks,");
        sb.Append("home_corner_kicks,away_corner_kicks,");
        sb.Append("home_shots_on_target,away_shots_on_target,");
        sb.Append("home_ball_safe_percentage,away_ball_safe_percentage,");
        
        // Prediction data
        sb.Append("prediction_favorite,prediction_confidence,prediction_expected_goals,");
        sb.Append("home_team_form,away_team_form,");
        sb.Append("home_team_win_pct,away_team_win_pct,");
        sb.Append("home_team_avg_goals,away_team_avg_goals");
        
        sb.AppendLine();
        
        // Get 9 snapshots across the match timeline
        var timelineSnapshots = GetTimelineSnapshots(snapshots);
        
        // Process each timeline snapshot
        for (int i = 0; i < timelineSnapshots.Count; i++)
        {
            var snapshot = timelineSnapshots[i];
            var timeSegment = $"{i * 10}-{(i + 1) * 10}"; // e.g., "0-10", "10-20", etc.
            
            // Basic match info
            sb.Append($"{snapshot.Timestamp:yyyy-MM-dd HH:mm:ss},");
            sb.Append($"{snapshot.MatchId},");
            sb.Append($"{timeSegment},");
            
            // Match state
            sb.Append($"{snapshot.Score},");
            sb.Append($"{snapshot.Period},");
            sb.Append($"{snapshot.MatchStatus},");
            sb.Append($"{snapshot.PlayedTime},");
            
            // Match statistics
            var homeDangerousAttacks = snapshot.MatchSituation?.Home?.TotalDangerousAttacks ?? 0;
            var awayDangerousAttacks = snapshot.MatchSituation?.Away?.TotalDangerousAttacks ?? 0;
            var homeSafeAttacks = snapshot.MatchSituation?.Home?.TotalSafeAttacks ?? 0;
            var awaySafeAttacks = snapshot.MatchSituation?.Away?.TotalSafeAttacks ?? 0;
            
            sb.Append($"{homeDangerousAttacks},");
            sb.Append($"{awayDangerousAttacks},");
            sb.Append($"{homeSafeAttacks},");
            sb.Append($"{awaySafeAttacks},");
            
            // Match details
            var homeCornerKicks = snapshot.MatchDetails?.Home?.CornerKicks ?? 0;
            var awayCornerKicks = snapshot.MatchDetails?.Away?.CornerKicks ?? 0;
            var homeShotsOnTarget = snapshot.MatchDetails?.Home?.ShotsOnTarget ?? 0;
            var awayShotsOnTarget = snapshot.MatchDetails?.Away?.ShotsOnTarget ?? 0;
            var homeBallSafePercentage = snapshot.MatchDetails?.Home?.BallSafePercentage ?? 0;
            var awayBallSafePercentage = snapshot.MatchDetails?.Away?.BallSafePercentage ?? 0;
            
            sb.Append($"{homeCornerKicks},");
            sb.Append($"{awayCornerKicks},");
            sb.Append($"{homeShotsOnTarget},");
            sb.Append($"{awayShotsOnTarget},");
            sb.Append($"{homeBallSafePercentage},");
            sb.Append($"{awayBallSafePercentage},");
            
            // Prediction data
            var favorite = snapshot.PredictionData?.Favorite ?? "unknown";
            var confidence = snapshot.PredictionData?.ConfidenceScore ?? 0;
            var expectedGoals = snapshot.PredictionData?.ExpectedGoals ?? 0;
            
            sb.Append($"{favorite},");
            sb.Append($"{confidence},");
            sb.Append($"{expectedGoals},");
            
            // Team data
            var homeTeamForm = snapshot.PredictionData?.HomeTeamData?.Form ?? "";
            var awayTeamForm = snapshot.PredictionData?.AwayTeamData?.Form ?? "";
            var homeTeamWinPct = snapshot.PredictionData?.HomeTeamData?.WinPercentage ?? 0;
            var awayTeamWinPct = snapshot.PredictionData?.AwayTeamData?.WinPercentage ?? 0;
            var homeTeamAvgGoals = snapshot.PredictionData?.HomeTeamData?.AvgTotalGoals ?? 0;
            var awayTeamAvgGoals = snapshot.PredictionData?.AwayTeamData?.AvgTotalGoals ?? 0;
            
            sb.Append($"{homeTeamForm},");
            sb.Append($"{awayTeamForm},");
            sb.Append($"{homeTeamWinPct},");
            sb.Append($"{awayTeamWinPct},");
            sb.Append($"{homeTeamAvgGoals},");
            sb.Append($"{awayTeamAvgGoals}");
            
            sb.AppendLine();
        }
        
        return sb.ToString();
    }    
    /// <summary>
    /// Exports a single match as a combined dataset for machine learning
    /// </summary>
    public async Task<string> ExportCombinedDatasetForMatchAsync(int matchId)
    {
        var snapshots = await GetMatchSnapshotsAsync(matchId);
        
        if (!snapshots.Any())
        {
            return "No data available for this match";
        }
        
        // Get the first and last snapshots
        var firstSnapshot = snapshots.OrderBy(s => s.Timestamp).First();
        var lastSnapshot = snapshots.OrderByDescending(s => s.Timestamp).First();
        
        // Create CSV header
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("match_id," +
                    // Pre-match data (prediction)
                    "pre_favorite,pre_confidence,pre_expected_goals," +
                    "pre_home_team_form,pre_away_team_form," +
                    "pre_home_team_win_pct,pre_away_team_win_pct," +
                    "pre_home_team_avg_goals,pre_away_team_avg_goals," +
                    "pre_home_corner_avg,pre_away_corner_avg," +
                    
                    // Mid-game data points (we'll use intervals)
                    "mid_time_elapsed," +
                    "mid_score," +
                    "mid_home_dangerous_attacks,mid_away_dangerous_attacks," +
                    "mid_home_corner_kicks,mid_away_corner_kicks," +
                    "mid_home_shots_on_target,mid_away_shots_on_target," +
                    "mid_home_ball_safe_percentage,mid_away_ball_safe_percentage," +
                    
                    // Final result data
                    "final_score," +
                    "final_home_dangerous_attacks,final_away_dangerous_attacks," +
                    "final_home_corner_kicks,final_away_corner_kicks," +
                    "final_home_shots_on_target,final_away_shots_on_target");
        
        // Find a good mid-point snapshot (around 45-60 minutes if available)
        var midSnapshot = snapshots
            .Where(s => ParsePlayedTime(s.PlayedTime) >= 45 && ParsePlayedTime(s.PlayedTime) <= 60)
            .OrderBy(s => Math.Abs(ParsePlayedTime(s.PlayedTime) - 45))
            .FirstOrDefault();
            
        // If no mid-snapshot in desired range, take middle snapshot
        if (midSnapshot == null && snapshots.Count > 2)
        {
            midSnapshot = snapshots.OrderBy(s => s.Timestamp).Skip(snapshots.Count / 2).First();
        }
        // If still no mid-snapshot, use the first snapshot (less than ideal)
        midSnapshot ??= firstSnapshot;
        
        // Build row with all data points
        sb.Append(matchId);
        
        // Pre-match prediction data
        var preFavorite = firstSnapshot.PredictionData?.Favorite ?? "unknown";
        var preConfidence = firstSnapshot.PredictionData?.ConfidenceScore ?? 0;
        var preExpectedGoals = firstSnapshot.PredictionData?.ExpectedGoals ?? 0;
        var preHomeTeamForm = firstSnapshot.PredictionData?.HomeTeamData?.Form ?? "";
        var preAwayTeamForm = firstSnapshot.PredictionData?.AwayTeamData?.Form ?? "";
        var preHomeTeamWinPct = firstSnapshot.PredictionData?.HomeTeamData?.WinPercentage ?? 0;
        var preAwayTeamWinPct = firstSnapshot.PredictionData?.AwayTeamData?.WinPercentage ?? 0;
        var preHomeTeamAvgGoals = firstSnapshot.PredictionData?.HomeTeamData?.AvgTotalGoals ?? 0;
        var preAwayTeamAvgGoals = firstSnapshot.PredictionData?.AwayTeamData?.AvgTotalGoals ?? 0;
        var preHomeCornerAvg = firstSnapshot.PredictionData?.CornerStats?.HomeAvg ?? 0;
        var preAwayCornerAvg = firstSnapshot.PredictionData?.CornerStats?.AwayAvg ?? 0;
        
        sb.Append($",{preFavorite}");
        sb.Append($",{preConfidence}");
        sb.Append($",{preExpectedGoals}");
        sb.Append($",{preHomeTeamForm}");
        sb.Append($",{preAwayTeamForm}");
        sb.Append($",{preHomeTeamWinPct}");
        sb.Append($",{preAwayTeamWinPct}");
        sb.Append($",{preHomeTeamAvgGoals}");
        sb.Append($",{preAwayTeamAvgGoals}");
        sb.Append($",{preHomeCornerAvg}");
        sb.Append($",{preAwayCornerAvg}");
        
        // Mid-game data
        var midTimeElapsed = midSnapshot.PlayedTime;
        var midScore = midSnapshot.Score;
        var midHomeDangerousAttacks = midSnapshot.MatchSituation?.Home?.TotalDangerousAttacks ?? 0;
        var midAwayDangerousAttacks = midSnapshot.MatchSituation?.Away?.TotalDangerousAttacks ?? 0;
        var midHomeCornerKicks = midSnapshot.MatchDetails?.Home?.CornerKicks ?? 0;
        var midAwayCornerKicks = midSnapshot.MatchDetails?.Away?.CornerKicks ?? 0;
        var midHomeShotsOnTarget = midSnapshot.MatchDetails?.Home?.ShotsOnTarget ?? 0;
        var midAwayShotsOnTarget = midSnapshot.MatchDetails?.Away?.ShotsOnTarget ?? 0;
        var midHomeBallSafePercentage = midSnapshot.MatchDetails?.Home?.BallSafePercentage ?? 0;
        var midAwayBallSafePercentage = midSnapshot.MatchDetails?.Away?.BallSafePercentage ?? 0;
        
        sb.Append($",{midTimeElapsed}");
        sb.Append($",{midScore}");
        sb.Append($",{midHomeDangerousAttacks}");
        sb.Append($",{midAwayDangerousAttacks}");
        sb.Append($",{midHomeCornerKicks}");
        sb.Append($",{midAwayCornerKicks}");
        sb.Append($",{midHomeShotsOnTarget}");
        sb.Append($",{midAwayShotsOnTarget}");
        sb.Append($",{midHomeBallSafePercentage}");
        sb.Append($",{midAwayBallSafePercentage}");
        
        // Final result data
        var finalScore = lastSnapshot.Score;
        var finalHomeDangerousAttacks = lastSnapshot.MatchSituation?.Home?.TotalDangerousAttacks ?? 0;
        var finalAwayDangerousAttacks = lastSnapshot.MatchSituation?.Away?.TotalDangerousAttacks ?? 0;
        var finalHomeCornerKicks = lastSnapshot.MatchDetails?.Home?.CornerKicks ?? 0;
        var finalAwayCornerKicks = lastSnapshot.MatchDetails?.Away?.CornerKicks ?? 0;
        var finalHomeShotsOnTarget = lastSnapshot.MatchDetails?.Home?.ShotsOnTarget ?? 0;
        var finalAwayShotsOnTarget = lastSnapshot.MatchDetails?.Away?.ShotsOnTarget ?? 0;
        
        sb.Append($",{finalScore}");
        sb.Append($",{finalHomeDangerousAttacks}");
        sb.Append($",{finalAwayDangerousAttacks}");
        sb.Append($",{finalHomeCornerKicks}");
        sb.Append($",{finalAwayCornerKicks}");
        sb.Append($",{finalHomeShotsOnTarget}");
        sb.Append($",{finalAwayShotsOnTarget}");
        
        sb.AppendLine();
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Exports all match data to a single consolidated ML dataset using 9-snapshot timeline
    /// </summary>
    public async Task<string> ExportAllMatchesDatasetAsync()
    {
        try
        {
            // Get all match snapshots
            var allSnapshots = await GetAllMatchSnapshotsAsync();
            
            // Group by match ID
            var matchGroups = allSnapshots.GroupBy(s => s.MatchId).ToList();
            
            if (!matchGroups.Any())
            {
                return "No data available";
            }
            
            // Create CSV header with columns for all 9 time segments
            var sb = new System.Text.StringBuilder();
            
            // Match identification
            sb.Append("match_id,home_team,away_team,status,match_date,");
            
            // Pre-match prediction data
            sb.Append("pre_favorite,pre_confidence,pre_expected_goals,");
            sb.Append("pre_home_team_form,pre_away_team_form,");
            sb.Append("pre_home_win_pct,pre_away_win_pct,");
            
            // For each of the 9 time segments
            for (int i = 0; i < 9; i++)
            {
                string segment = $"{i * 10}-{(i + 1) * 10}";
                
                sb.Append($"t{i+1}_played_time,");
                sb.Append($"t{i+1}_score,");
                sb.Append($"t{i+1}_home_dangerous_attacks,");
                sb.Append($"t{i+1}_away_dangerous_attacks,");
                sb.Append($"t{i+1}_home_shots_on_target,");
                sb.Append($"t{i+1}_away_shots_on_target,");
                sb.Append($"t{i+1}_home_corners,");
                sb.Append($"t{i+1}_away_corners,");
            }
            
            // Final outcome
            sb.Append("final_score,final_result,prediction_correct");
            
            sb.AppendLine();
            
            // Process each match
            foreach (var group in matchGroups)
            {
                var matchId = group.Key;
                var snapshots = group.OrderBy(s => s.Timestamp).ToList();
                
                // Only include matches with at least 3 snapshots
                if (snapshots.Count < 3)
                {
                    continue;
                }
                
                // Get first snapshot for pre-match data
                var firstSnapshot = snapshots.First();
                
                // Get 9 snapshots across the match timeline
                var timelineSnapshots = GetTimelineSnapshots(snapshots);
                
                // Get final snapshot for outcome
                var lastSnapshot = snapshots.Last();
                
                // Only include completed matches
                if (!(lastSnapshot.PlayedTime.Contains("90:", StringComparison.OrdinalIgnoreCase) || 
                    lastSnapshot.MatchStatus.Contains("Ended", StringComparison.OrdinalIgnoreCase) ||
                    lastSnapshot.MatchStatus.Contains("finish", StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }
                
                // Match identification
                sb.Append($"{matchId},");
                sb.Append($"\"{firstSnapshot.PredictionData?.HomeTeamData?.Name ?? "Home Team"}\",");
                sb.Append($"\"{firstSnapshot.PredictionData?.AwayTeamData?.Name ?? "Away Team"}\",");
                sb.Append($"\"{firstSnapshot.MatchStatus ?? ""}\",");
                sb.Append($"{firstSnapshot.Timestamp:yyyy-MM-dd},");
                
                // Pre-match prediction data
                sb.Append($"{firstSnapshot.PredictionData?.Favorite ?? "unknown"},");
                sb.Append($"{firstSnapshot.PredictionData?.ConfidenceScore ?? 0},");
                sb.Append($"{firstSnapshot.PredictionData?.ExpectedGoals ?? 0},");
                sb.Append($"{firstSnapshot.PredictionData?.HomeTeamData?.Form ?? ""},");
                sb.Append($"{firstSnapshot.PredictionData?.AwayTeamData?.Form ?? ""},");
                sb.Append($"{firstSnapshot.PredictionData?.HomeTeamData?.WinPercentage ?? 0},");
                sb.Append($"{firstSnapshot.PredictionData?.AwayTeamData?.WinPercentage ?? 0},");
                
                // For each of the 9 time segments
                for (int i = 0; i < 9; i++)
                {
                    var snapshot = i < timelineSnapshots.Count ? timelineSnapshots[i] : lastSnapshot;
                    
                    sb.Append($"{snapshot.PlayedTime},");
                    sb.Append($"{snapshot.Score},");
                    sb.Append($"{snapshot.MatchSituation?.Home?.TotalDangerousAttacks ?? 0},");
                    sb.Append($"{snapshot.MatchSituation?.Away?.TotalDangerousAttacks ?? 0},");
                    sb.Append($"{snapshot.MatchDetails?.Home?.ShotsOnTarget ?? 0},");
                    sb.Append($"{snapshot.MatchDetails?.Away?.ShotsOnTarget ?? 0},");
                    sb.Append($"{snapshot.MatchDetails?.Home?.CornerKicks ?? 0},");
                    sb.Append($"{snapshot.MatchDetails?.Away?.CornerKicks ?? 0},");
                }
                
                // Final outcome
                sb.Append($"{lastSnapshot.Score},");
                
                // Determine actual match outcome
                string actualOutcome = "unknown";
                var finalScore = ParseScore(lastSnapshot.Score);
                if (finalScore.HasValue)
                {
                    var (homeGoals, awayGoals) = finalScore.Value;
                    if (homeGoals > awayGoals)
                        actualOutcome = "home";
                    else if (homeGoals < awayGoals)
                        actualOutcome = "away";
                    else
                        actualOutcome = "draw";
                }
                
                sb.Append($"{actualOutcome},");
                
                // Was prediction correct?
                bool isPredictionCorrect = firstSnapshot.PredictionData?.Favorite == actualOutcome;
                sb.Append(isPredictionCorrect ? "true" : "false");
                
                sb.AppendLine();
            }
            
            return sb.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting all matches dataset");
            return $"Error exporting dataset: {ex.Message}";
        }
    }
    
    /// <summary>
    /// Gets 9 snapshots across the match timeline (0-10, 10-20, ..., 80-90)
    /// </summary>
    private List<MatchSnapshot> GetTimelineSnapshots(List<MatchSnapshot> allSnapshots)
    {
        var result = new List<MatchSnapshot>();
        
        // Define 9 time ranges (0-10, 10-20, ..., 80-90)
        var timeRanges = new List<(int Start, int End)>
        {
            (0, 10),
            (10, 20),
            (20, 30),
            (30, 40),
            (40, 50), // Includes halftime
            (50, 60),
            (60, 70),
            (70, 80),
            (80, 90)  // Plus any added time
        };
        
        // Find best snapshot for each time range
        foreach (var (start, end) in timeRanges)
        {
            // First try to find snapshots within this exact range
            var rangeSnapshots = allSnapshots
                .Where(s => {
                    var minutes = ParsePlayedTime(s.PlayedTime);
                    return minutes >= start && minutes < end;
                })
                .ToList();
                
            // If we have snapshots in this range, pick the one closest to the middle of the range
            if (rangeSnapshots.Any())
            {
                var middleTime = start + (end - start) / 2;
                var bestSnapshot = rangeSnapshots
                    .OrderBy(s => Math.Abs(ParsePlayedTime(s.PlayedTime) - middleTime))
                    .First();
                    
                result.Add(bestSnapshot);
            }
            else
            {
                // If no snapshots in this exact range, find closest one
                var middleTime = start + (end - start) / 2;
                var bestSnapshot = allSnapshots
                    .OrderBy(s => Math.Abs(ParsePlayedTime(s.PlayedTime) - middleTime))
                    .First();
                    
                result.Add(bestSnapshot);
            }
        }
        
        // Ensure we have exactly 9 snapshots
        while (result.Count < 9)
        {
            // If we don't have enough snapshots, duplicate the last one
            result.Add(result.LastOrDefault() ?? allSnapshots.LastOrDefault());
        }
        
        // If we somehow got more than 9, trim the excess
        if (result.Count > 9)
        {
            result = result.Take(9).ToList();
        }
        
        return result;
    }
    
    /// <summary>
    /// Helper method to parse score string (e.g., "2-1")
    /// </summary>
    private (int Home, int Away)? ParseScore(string scoreStr)
    {
        if (string.IsNullOrEmpty(scoreStr))
            return null;
            
        var parts = scoreStr.Split(':');
        if (parts.Length != 2)
            return null;
            
        if (!int.TryParse(parts[0], out int homeGoals) || !int.TryParse(parts[1], out int awayGoals))
            return null;
            
        return (homeGoals, awayGoals);
    }
    
    /// <summary>
    /// Parses the played time string to get minutes
    /// </summary>
    private int ParsePlayedTime(string playedTime)
    {
        if (string.IsNullOrEmpty(playedTime))
            return 0;
            
        // Format like "45:00" or "90+2:30"
        string minutesPart = playedTime.Split(':')[0];
        
        // Handle added time (e.g., "90+2")
        if (minutesPart.Contains('+'))
        {
            string[] parts = minutesPart.Split('+');
            if (int.TryParse(parts[0], out int minutes) && int.TryParse(parts[1], out int added))
            {
                return minutes + added;
            }
        }
        
        // Simple case
        if (int.TryParse(minutesPart, out int result))
        {
            return result;
        }
        
        return 0;
    }
}

/// <summary>
/// Model class for storing match snapshots for ML purposes
/// </summary>
public class MatchSnapshot
{
    [BsonId]
    public ObjectId Id { get; set; }
    
    public int MatchId { get; set; }
    
    public DateTime Timestamp { get; set; }
    
    public string Score { get; set; }
    
    public string Period { get; set; }
    
    public string MatchStatus { get; set; }
    
    public string PlayedTime { get; set; }
    
    public ClientMatchSituation MatchSituation { get; set; }
    
    public ClientMatchDetailsExtended MatchDetails { get; set; }
    
    public ClientMatchPredictionData PredictionData { get; set; }
}

/// <summary>
/// Model class for match prediction data
/// </summary>
public class ClientMatchPredictionData
{
    public string Favorite { get; set; }
    
    public int ConfidenceScore { get; set; }
    
    public double AverageGoals { get; set; }
    
    public double ExpectedGoals { get; set; }
    
    public double DefensiveStrength { get; set; }
    
    public ClientCornerStats CornerStats { get; set; }
    
    public ClientScoringPatterns ScoringPatterns { get; set; }
    
    public List<string> ReasonsForPrediction { get; set; }
    
    public ClientHeadToHeadData HeadToHead { get; set; }
    
    public ClientTeamData HomeTeamData { get; set; }
    
    public ClientTeamData AwayTeamData { get; set; }
}

public class ClientCornerStats
{
    public double HomeAvg { get; set; }
    
    public double AwayAvg { get; set; }
    
    public double TotalAvg { get; set; }
}

public class ClientScoringPatterns
{
    public double HomeFirstGoalRate { get; set; }
    
    public double AwayFirstGoalRate { get; set; }
    
    public double HomeLateGoalRate { get; set; }
    
    public double AwayLateGoalRate { get; set; }
}

public class ClientHeadToHeadData
{
    public int Matches { get; set; }
    
    public int Wins { get; set; }
    
    public int Draws { get; set; }
    
    public int Losses { get; set; }
    
    public int GoalsScored { get; set; }
    
    public int GoalsConceded { get; set; }
    
    public List<ClientRecentMatchResult> RecentMatches { get; set; }
}

public class ClientRecentMatchResult
{
    public string Date { get; set; }
    
    public string Result { get; set; }
}

public class ClientTeamData
{
    public string Name { get; set; }
    
    public int Position { get; set; }
    
    public string Form { get; set; }
    
    public string HomeForm { get; set; }
    
    public string AwayForm { get; set; }
    
    public double AvgHomeGoals { get; set; }
    
    public double AvgAwayGoals { get; set; }
    
    public double AvgTotalGoals { get; set; }
    
    public double AverageGoalsScored { get; set; }
    
    public double AverageGoalsConceded { get; set; }
    
    public int CleanSheets { get; set; }
    
    public int HomeCleanSheets { get; set; }
    
    public int AwayCleanSheets { get; set; }
    
    public double ScoringFirstWinRate { get; set; }
    
    public double WinPercentage { get; set; }
    
    public double HomeWinPercentage { get; set; }
    
    public double AwayWinPercentage { get; set; }
}