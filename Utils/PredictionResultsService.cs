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
    private readonly IMemoryCache _cache;
    private readonly IHubContext<LiveMatchHub> _hubContext;
    private readonly PredictionEnrichedMatchService _predictionEnrichedMatchService;
    
    // Constants for cache keys
    private const string CACHE_KEY_PREDICTION_RESULTS = "prediction_results";
    
    public PredictionResultsService(
        ILogger<PredictionResultsService> logger,
        IMemoryCache cache,
        IHubContext<LiveMatchHub> hubContext,
        PredictionEnrichedMatchService predictionEnrichedMatchService)
    {
        _logger = logger;
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
        var sw = System.Diagnostics.Stopwatch.StartNew();
        _logger.LogInformation("Processing completed matches for prediction results");
        
        // Use optimized date range for snapshot lookup
        var endTime = DateTime.UtcNow;
        var startTime = endTime.AddHours(-24); // Last 24 hours
        
        // Get snapshots with optimized retrieval that won't time out
        var allSnapshots = await _predictionEnrichedMatchService.GetAllMatchSnapshotsAsync(startTime, endTime);
        
        if (!allSnapshots.Any())
        {
            _logger.LogInformation("No snapshots found in the specified time range");
            return;
        }
        
        _logger.LogInformation($"Retrieved {allSnapshots.Count} total snapshots, filtering for completed matches");
        
        // Filter for completed matches more efficiently
        var completedMatchIds = new HashSet<int>();
        
        // First check for matches where we know they're completed
        foreach (var snapshot in allSnapshots)
        {
            if (IsMatchCompleted(snapshot) && !completedMatchIds.Contains(snapshot.MatchId))
            {
                completedMatchIds.Add(snapshot.MatchId);
            }
        }
        
        _logger.LogInformation($"Found {completedMatchIds.Count} completed matches to process");
        
        if (!completedMatchIds.Any())
        {
            _logger.LogInformation("No completed matches found in the time range");
            return;
        }
        
        // Process matches in batches to avoid memory pressure
        var predictionResults = new List<PredictionResult>();
        int processedCount = 0;
        int batchSize = 5; // Process in small batches
        
        foreach (var matchIdBatch in completedMatchIds.Chunk(batchSize))
        {
            List<Task<PredictionResult>> processingTasks = new List<Task<PredictionResult>>();
            
            foreach (var matchId in matchIdBatch)
            {
                processingTasks.Add(Task.Run(async () => 
                {
                    try
                    {
                        // Get all snapshots for this match
                        var matchSnapshots = allSnapshots.Where(s => s.MatchId == matchId).ToList();
                        
                        // Need at least 2 snapshots (pre-match and final)
                        if (matchSnapshots.Count < 2)
                            return null;
                        
                        // Calculate prediction accuracy
                        var result = CalculatePredictionResultWithTimelineSnapshots(matchSnapshots);
                        
                        if (result != null)
                        {
                            _logger.LogDebug($"Successfully processed match {matchId}");
                            return result;
                        }
                        
                        return null;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing match {matchId} for prediction results");
                        return null;
                    }
                }, stoppingToken));
            }
            
            try
            {
                // Wait for all tasks in this batch to complete
                var results = await Task.WhenAll(processingTasks);
                
                // Add non-null results to our collection
                foreach (var result in results.Where(r => r != null))
                {
                    predictionResults.Add(result);
                }
                
                processedCount += matchIdBatch.Length;
                _logger.LogInformation($"Processed {processedCount}/{completedMatchIds.Count} matches");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing batch of matches");
            }
            
            // Check for cancellation between batches
            if (stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning("Processing canceled before completion");
                break;
            }
        }
        
        if (predictionResults.Any())
        {
            // Cache the results
            _cache.Set(CACHE_KEY_PREDICTION_RESULTS, predictionResults, TimeSpan.FromHours(1));
            
            // Send prediction results to clients
            try
            {
                await _hubContext.Clients.All.SendAsync("ReceivePredictionResults", 
                    new PredictionResultsResponse 
                    {
                        Results = predictionResults,
                        LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                    }, 
                    stoppingToken);
                    
                _logger.LogInformation($"Sent {predictionResults.Count} prediction results to clients");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending prediction results to clients via SignalR");
            }
        }
        
        sw.Stop();
        _logger.LogInformation($"Completed match processing in {sw.ElapsedMilliseconds}ms");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error processing completed matches for prediction results");
    }
}

    /// <summary>
    /// Determines if a match is completed based on played time and match status
    /// </summary>
    public static bool IsMatchCompleted(MatchSnapshot snapshot)
    {
        // Check for null values first
        if (snapshot.PlayedTime == null || snapshot.MatchStatus == null)
            return false;
    
        // Check played time - parse first part before colon to get minutes
        if (snapshot.PlayedTime.Contains(':'))
        {
            string minutesPart = snapshot.PlayedTime.Split(':')[0];
        
            // Handle added time format (e.g., "90+3")
            if (minutesPart.Contains('+'))
            {
                minutesPart = minutesPart.Split('+')[0];
            }
        
            // Try to parse the minutes as integer
            if (int.TryParse(minutesPart, out int minutes) && minutes >= 90)
            {
                return true;
            }
        }
    
        // Check match status for "ended" text
        if (snapshot.MatchStatus.ToLower().Contains("ended"))
        {
            return true;
        }
    
        // Additional check for "finished" text
        if (snapshot.MatchStatus.ToLower().Contains("finish"))
        {
            return true;
        }
    
        return false;
    }  
    /// <summary>
    /// Calculates prediction accuracy for a match with 9 timeline snapshots
    /// </summary>
    private PredictionResult CalculatePredictionResultWithTimelineSnapshots(List<MatchSnapshot> snapshots)
    {
        try
        {
            if (!snapshots.Any())
                return null;
                
            // Get first and last snapshots
            var firstSnapshot = snapshots.First();
            var lastSnapshot = snapshots.Last();
            
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
            
            // Find 9 snapshots across the match timeline (0-10, 10-20, ..., 80-90)
            var timelineSnapshots = GetTimelineSnapshots(snapshots);
            
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
                
                // Include timeline statistics
                TimelineStats = timelineSnapshots.Select(s => new LiveStats
                {
                    PlayedTime = s.PlayedTime,
                    Score = s.Score,
                    HomeDangerousAttacks = s.MatchSituation?.Home?.TotalDangerousAttacks ?? 0,
                    AwayDangerousAttacks = s.MatchSituation?.Away?.TotalDangerousAttacks ?? 0,
                    HomeShotsOnTarget = s.MatchDetails?.Home?.ShotsOnTarget ?? 0,
                    AwayShotsOnTarget = s.MatchDetails?.Away?.ShotsOnTarget ?? 0,
                    HomeCornerKicks = s.MatchDetails?.Home?.CornerKicks ?? 0,
                    AwayCornerKicks = s.MatchDetails?.Away?.CornerKicks ?? 0,
                    Timestamp = s.Timestamp
                }).ToList(),
                
                FinalStats = new LiveStats
                {
                    PlayedTime = lastSnapshot.PlayedTime,
                    Score = lastSnapshot.Score,
                    HomeDangerousAttacks = lastSnapshot.MatchSituation?.Home?.TotalDangerousAttacks ?? 0,
                    AwayDangerousAttacks = lastSnapshot.MatchSituation?.Away?.TotalDangerousAttacks ?? 0,
                    HomeShotsOnTarget = lastSnapshot.MatchDetails?.Home?.ShotsOnTarget ?? 0,
                    AwayShotsOnTarget = lastSnapshot.MatchDetails?.Away?.ShotsOnTarget ?? 0,
                    HomeCornerKicks = lastSnapshot.MatchDetails?.Home?.CornerKicks ?? 0,
                    AwayCornerKicks = lastSnapshot.MatchDetails?.Away?.CornerKicks ?? 0,
                    Timestamp = lastSnapshot.Timestamp
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error calculating prediction result with timeline snapshots for match {snapshots.FirstOrDefault()?.MatchId}");
            return null;
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
            
        var parts = scoreStr.Split(':');
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
    
    // Timeline snapshots (9 points throughout the match)
    public List<LiveStats> TimelineStats { get; set; } = new List<LiveStats>();
    
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
    
    // Added timestamp for better timeline visualization
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Model class for prediction results response
/// </summary>
public class PredictionResultsResponse
{
    public List<PredictionResult> Results { get; set; }
    
    public string LastUpdated { get; set; }
}