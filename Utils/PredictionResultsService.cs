
using System.Collections.Concurrent;
using fredapi.Database;
using fredapi.Routes;
using fredapi.SignalR;
using fredapi.SportRadarService.Background.ArbitrageLiveMatchBackgroundService;
using fredapi.SportRadarService.Transformers;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Extensions.Caching.Memory;

namespace fredapi.Utils;

/// <summary>
/// Service for tracking and providing prediction results for recently completed matches
/// </summary>
public class PredictionResultsService
{
    private readonly ILogger<PredictionResultsService> _logger;
    private readonly MongoDbService _mongoDbService;
    private readonly IMemoryCache _cache;
    private readonly IHubContext<LiveMatchHub> _hubContext;
    private readonly PredictionEnrichedMatchService _predictionEnrichedMatchService;
    
    // Constants for cache keys
    private const string CACHE_KEY_PREDICTION_RESULTS = "prediction_results";
    
    public PredictionResultsService(
        ILogger<PredictionResultsService> logger,
        MongoDbService mongoDbService,
        IMemoryCache cache,
        IHubContext<LiveMatchHub> hubContext,
        PredictionEnrichedMatchService predictionEnrichedMatchService)
    {
        _logger = logger;
        _mongoDbService = mongoDbService;
        _cache = cache;
        _hubContext = hubContext;
        _predictionEnrichedMatchService = predictionEnrichedMatchService;
    }
    
    /// <summary>
    /// Processes completed matches from the last 24 hours and generates prediction results
    /// </summary>
    public async Task ProcessCompletedMatchesAsync(CancellationToken stoppingToken = default)
    {
        try
        {
            _logger.LogInformation("Processing completed matches for prediction results");
            
            // Get all match snapshots from the last 24 hours
            var endTime = DateTime.UtcNow;
            var startTime = endTime.AddHours(-24);
            
            var allSnapshots = await _predictionEnrichedMatchService.GetAllMatchSnapshotsAsync(startTime, endTime);
            
            // Filter snapshots to find completed matches
            var completedMatchGroups = allSnapshots
                .Where(s => s.MatchStatus.Contains("finish", StringComparison.OrdinalIgnoreCase) || 
                           s.MatchStatus.Contains("ended", StringComparison.OrdinalIgnoreCase))
                .GroupBy(s => s.MatchId)
                .ToList();
                
            // Process each completed match
            var predictionResults = new List<PredictionResult>();
            
            foreach (var matchGroup in completedMatchGroups)
            {
                try
                {
                    var matchId = matchGroup.Key;
                    var snapshots = matchGroup.OrderBy(s => s.Timestamp).ToList();
                    
                    // Need at least 2 snapshots (pre-match and final)
                    if (snapshots.Count < 2)
                        continue;
                        
                    // Get first (pre-match) and last (final) snapshots
                    var firstSnapshot = snapshots.First();
                    var lastSnapshot = snapshots.Last();
                    
                    // Calculate prediction accuracy
                    var predictionResult = CalculatePredictionResult(firstSnapshot, lastSnapshot);
                    if (predictionResult != null)
                    {
                        predictionResults.Add(predictionResult);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing match {matchGroup.Key} for prediction results");
                }
            }
            
            if (predictionResults.Any())
            {
                // Cache the results
                _cache.Set(CACHE_KEY_PREDICTION_RESULTS, predictionResults, TimeSpan.FromHours(24));
                
                // Send prediction results to clients
                await _hubContext.Clients.All.SendAsync("ReceivePredictionResults", 
                    new PredictionResultsResponse 
                    {
                        Results = predictionResults,
                        LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                    }, 
                    stoppingToken);
                    
                _logger.LogInformation($"Sent {predictionResults.Count} prediction results to clients");
            }
            else
            {
                _logger.LogInformation("No completed matches with predictions found in the last 24 hours");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing completed matches for prediction results");
        }
    }
    
    /// <summary>
    /// Calculates prediction accuracy for a match
    /// </summary>
    private PredictionResult CalculatePredictionResult(MatchSnapshot firstSnapshot, MatchSnapshot lastSnapshot)
    {
        try
        {
            // Extract prediction data
            var predictionData = firstSnapshot.PredictionData;
            if (predictionData == null)
            {
                return null; // No prediction data available
            }
            
            // Parse final score
            var finalScore = ParseScore(lastSnapshot.Score);
            if (finalScore == null)
            {
                return null; // Invalid score format
            }
            
            var (homeGoals, awayGoals) = finalScore.Value;
            
            // Determine actual match outcome
            string actualOutcome;
            if (homeGoals > awayGoals)
                actualOutcome = "home";
            else if (homeGoals < awayGoals)
                actualOutcome = "away";
            else
                actualOutcome = "draw";
                
            // Determine if prediction was correct
            bool isPredictionCorrect = predictionData.Favorite.Equals(actualOutcome, StringComparison.OrdinalIgnoreCase);
            
            // Determine if goals prediction was accurate
            double totalGoals = homeGoals + awayGoals;
            double expectedGoals = predictionData.ExpectedGoals;
            
            // Consider prediction accurate if within 1 goal of actual
            bool isGoalPredictionAccurate = Math.Abs(totalGoals - expectedGoals) <= 1;
            
            // Get mid-game snapshot if available
            MatchSnapshot midSnapshot = null;
            var midTimePoint = 45; // Ideally halftime
            
            // Find a snapshot close to 45 minutes
            var snapshots = _predictionEnrichedMatchService.GetMatchSnapshotsAsync(lastSnapshot.MatchId).Result
                .OrderBy(s => s.Timestamp).ToList();
                
            if (snapshots.Count > 2)
            {
                midSnapshot = snapshots
                    .Where(s => ParsePlayedTime(s.PlayedTime) >= 30 && ParsePlayedTime(s.PlayedTime) <= 60)
                    .OrderBy(s => Math.Abs(ParsePlayedTime(s.PlayedTime) - midTimePoint))
                    .FirstOrDefault();
                    
                // If no snapshot in desired range, take middle snapshot
                if (midSnapshot == null)
                {
                    midSnapshot = snapshots[snapshots.Count / 2];
                }
            }
            
            // Create the prediction result
            return new PredictionResult
            {
                MatchId = lastSnapshot.MatchId,
                HomeTeam = predictionData.HomeTeamData?.Name ?? "Home Team",
                AwayTeam = predictionData.AwayTeamData?.Name ?? "Away Team",
                FinalScore = lastSnapshot.Score,
                MatchTime = firstSnapshot.Timestamp,
                PredictedFavorite = predictionData.Favorite,
                PredictedConfidence = predictionData.ConfidenceScore,
                PredictedExpectedGoals = predictionData.ExpectedGoals,
                ActualOutcome = actualOutcome,
                IsPredictionCorrect = isPredictionCorrect,
                IsGoalPredictionAccurate = isGoalPredictionAccurate,
                
                // Include statistics if available
                LiveStats = midSnapshot != null ? new LiveStats
                {
                    PlayedTime = midSnapshot.PlayedTime,
                    Score = midSnapshot.Score,
                    HomeDangerousAttacks = midSnapshot.MatchSituation?.Home?.TotalDangerousAttacks ?? 0,
                    AwayDangerousAttacks = midSnapshot.MatchSituation?.Away?.TotalDangerousAttacks ?? 0,
                    HomeShotsOnTarget = midSnapshot.MatchDetails?.Home?.ShotsOnTarget ?? 0,
                    AwayShotsOnTarget = midSnapshot.MatchDetails?.Away?.ShotsOnTarget ?? 0,
                    HomeCornerKicks = midSnapshot.MatchDetails?.Home?.CornerKicks ?? 0,
                    AwayCornerKicks = midSnapshot.MatchDetails?.Away?.CornerKicks ?? 0
                } : null,
                
                FinalStats = new LiveStats
                {
                    PlayedTime = lastSnapshot.PlayedTime,
                    Score = lastSnapshot.Score,
                    HomeDangerousAttacks = lastSnapshot.MatchSituation?.Home?.TotalDangerousAttacks ?? 0,
                    AwayDangerousAttacks = lastSnapshot.MatchSituation?.Away?.TotalDangerousAttacks ?? 0,
                    HomeShotsOnTarget = lastSnapshot.MatchDetails?.Home?.ShotsOnTarget ?? 0,
                    AwayShotsOnTarget = lastSnapshot.MatchDetails?.Away?.ShotsOnTarget ?? 0,
                    HomeCornerKicks = lastSnapshot.MatchDetails?.Home?.CornerKicks ?? 0,
                    AwayCornerKicks = lastSnapshot.MatchDetails?.Away?.CornerKicks ?? 0
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error calculating prediction result for match {lastSnapshot.MatchId}");
            return null;
        }
    }
    
    /// <summary>
    /// Gets current prediction results from cache or generates new ones if needed
    /// </summary>
    public async Task<PredictionResultsResponse> GetPredictionResultsAsync()
    {
        // Check if we have cached results
        if (_cache.TryGetValue(CACHE_KEY_PREDICTION_RESULTS, out List<PredictionResult> cachedResults) && 
            cachedResults != null && 
            cachedResults.Any())
        {
            return new PredictionResultsResponse
            {
                Results = cachedResults,
                LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }
        
        // Generate new results
        await ProcessCompletedMatchesAsync();
        
        // Try to get from cache again
        if (_cache.TryGetValue(CACHE_KEY_PREDICTION_RESULTS, out cachedResults) && 
            cachedResults != null && 
            cachedResults.Any())
        {
            return new PredictionResultsResponse
            {
                Results = cachedResults,
                LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }
        
        // Return empty response if still nothing
        return new PredictionResultsResponse
        {
            Results = new List<PredictionResult>(),
            LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }
    
    /// <summary>
    /// Helper method to parse score string (e.g., "2-1")
    /// </summary>
    private (int Home, int Away)? ParseScore(string scoreStr)
    {
        if (string.IsNullOrEmpty(scoreStr))
            return null;
            
        var parts = scoreStr.Split('-');
        if (parts.Length != 2)
            return null;
            
        if (!int.TryParse(parts[0], out int homeGoals) || !int.TryParse(parts[1], out int awayGoals))
            return null;
            
        return (homeGoals, awayGoals);
    }
    
    /// <summary>
    /// Helper method to parse played time string to get minutes
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
/// Model class for prediction results
/// </summary>
public class PredictionResult
{
    public int MatchId { get; set; }
    
    public string HomeTeam { get; set; }
    
    public string AwayTeam { get; set; }
    
    public string FinalScore { get; set; }
    
    public DateTime MatchTime { get; set; }
    
    public string PredictedFavorite { get; set; }
    
    public int PredictedConfidence { get; set; }
    
    public double PredictedExpectedGoals { get; set; }
    
    public string ActualOutcome { get; set; }
    
    public bool IsPredictionCorrect { get; set; }
    
    public bool IsGoalPredictionAccurate { get; set; }
    
    public LiveStats LiveStats { get; set; }
    
    public LiveStats FinalStats { get; set; }
}

/// <summary>
/// Model class for live statistics
/// </summary>
public class LiveStats
{
    public string PlayedTime { get; set; }
    
    public string Score { get; set; }
    
    public int HomeDangerousAttacks { get; set; }
    
    public int AwayDangerousAttacks { get; set; }
    
    public int HomeShotsOnTarget { get; set; }
    
    public int AwayShotsOnTarget { get; set; }
    
    public int HomeCornerKicks { get; set; }
    
    public int AwayCornerKicks { get; set; }
}

/// <summary>
/// Model class for prediction results response
/// </summary>
public class PredictionResultsResponse
{
    public List<PredictionResult> Results { get; set; }
    
    public string LastUpdated { get; set; }
}