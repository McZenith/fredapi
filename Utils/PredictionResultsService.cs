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
    /// Ensures all snapshots for each match are retrieved from the database
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

            // Get snapshots within time range to identify completed matches
            var recentSnapshots = await _predictionEnrichedMatchService.GetAllMatchSnapshotsAsync(startTime, endTime);

            if (!recentSnapshots.Any())
            {
                _logger.LogInformation("No snapshots found in the specified time range");
                return;
            }

            _logger.LogInformation(
                $"Retrieved {recentSnapshots.Count} total snapshots, filtering for completed matches");

            // Filter for completed matches more efficiently
            var completedMatchIds = new HashSet<int>();

            // First check for matches where we know they're completed
            foreach (var snapshot in recentSnapshots)
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
                            // Get ALL snapshots for this match from the database - not just from our time window
                            // This ensures we have the complete history for accurate timeline representation
                            var matchSnapshots = await _predictionEnrichedMatchService.GetMatchSnapshotsAsync(matchId);

                            // Log how many snapshots we found for this match
                            _logger.LogDebug($"Retrieved {matchSnapshots.Count} snapshots for match {matchId}");

                            // Need at least 9 snapshots for good time segment coverage
                            if (matchSnapshots.Count < 9)
                            {
                                _logger.LogInformation(
                                    $"Match {matchId} has insufficient snapshots: {matchSnapshots.Count}");

                                // We'll still process it if there are at least 2 snapshots, but log the limitation
                                if (matchSnapshots.Count < 2)
                                {
                                    _logger.LogInformation(
                                        $"Match {matchId} doesn't have enough snapshots for prediction analysis");
                                    return null;
                                }
                            }

                            // Calculate prediction accuracy
                            var result = CalculatePredictionResultWithTimelineSnapshots(matchSnapshots);

                            if (result != null)
                            {
                                // Log coverage quality
                                if (result.TimelineStats != null)
                                {
                                    _logger.LogDebug(
                                        $"Successfully processed match {matchId} with {result.TimelineStats.Count} timeline snapshots");
                                }

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
                // Add additional stats in logging
                var matchesWithCompleteTimeline = predictionResults.Count(r => r.TimelineStats?.Count >= 9);
                var matchesWithPartialTimeline = predictionResults.Count - matchesWithCompleteTimeline;

                _logger.LogInformation($"Processed {predictionResults.Count} matches total: " +
                                       $"{matchesWithCompleteTimeline} with complete timeline, " +
                                       $"{matchesWithPartialTimeline} with partial timeline");

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
    /// Calculates prediction accuracy for a match using representative timeline snapshots
    /// </summary>
    private PredictionResult CalculatePredictionResultWithTimelineSnapshots(List<MatchSnapshot> snapshots)
    {
        try
        {
            if (!snapshots.Any())
                return null;

            // Get first and last snapshots (by timestamp)
            var orderedSnapshots = snapshots.OrderBy(s => s.Timestamp).ToList();
            var firstSnapshot = orderedSnapshots.First();
            var lastSnapshot = orderedSnapshots.Last();

            // Extract prediction data
            var predictionData = firstSnapshot.PredictionData;
            if (predictionData == null)
            {
                _logger.LogWarning($"No prediction data available for match {firstSnapshot.MatchId}");
                return null; // No prediction data available
            }

            // Parse final score
            var finalScore = ParseScore(lastSnapshot.Score);
            if (finalScore == null)
            {
                _logger.LogWarning($"Invalid score format: '{lastSnapshot.Score}' for match {lastSnapshot.MatchId}");
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
            bool isPredictionCorrect =
                predictionData.Favorite.Equals(actualOutcome, StringComparison.OrdinalIgnoreCase);

            // Determine if goals prediction was accurate
            double totalGoals = homeGoals + awayGoals;
            double expectedGoals = predictionData.ExpectedGoals;

            // Consider prediction accurate if within 1 goal of actual
            bool isGoalPredictionAccurate = Math.Abs(totalGoals - expectedGoals) <= 1;

            // Get representative timeline snapshots
            var timelineSnapshots = GetTimelineSnapshots(snapshots);

            // Verify we have a reasonable number of snapshots
            if (timelineSnapshots.Count < 5)
            {
                _logger.LogWarning($"Insufficient timeline snapshots for match {firstSnapshot.MatchId}. " +
                                   $"Found only {timelineSnapshots.Count} timestamps across match timeline.");
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

                // Map timeline snapshots to stats objects
                TimelineStats = timelineSnapshots.Select(s => CreateLiveStats(s)).ToList(),

                // Final stats
                FinalStats = CreateLiveStats(lastSnapshot)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Error calculating prediction result with timeline snapshots for match {snapshots.FirstOrDefault()?.MatchId}");
            return null;
        }
    }

    /// <summary>
    /// Creates a LiveStats object from a snapshot
    /// </summary>
    private LiveStats CreateLiveStats(MatchSnapshot snapshot)
    {
        return new LiveStats
        {
            PlayedTime = snapshot.PlayedTime,
            Score = snapshot.Score,
            HomeDangerousAttacks = snapshot.MatchSituation?.Home?.TotalDangerousAttacks ?? 0,
            AwayDangerousAttacks = snapshot.MatchSituation?.Away?.TotalDangerousAttacks ?? 0,
            HomeShotsOnTarget = snapshot.MatchDetails?.Home?.ShotsOnTarget ?? 0,
            AwayShotsOnTarget = snapshot.MatchDetails?.Away?.ShotsOnTarget ?? 0,
            HomeCornerKicks = snapshot.MatchDetails?.Home?.CornerKicks ?? 0,
            AwayCornerKicks = snapshot.MatchDetails?.Away?.CornerKicks ?? 0,
            Timestamp = snapshot.Timestamp
        };
    }

    /// <summary>
    /// Gets snapshots across the match timeline (0-10, 10-20, ..., 80-90)
    /// Ensures each 10-minute segment has appropriate representation
    /// Does not duplicate snapshots when data is missing
    /// </summary>
    private List<MatchSnapshot> GetTimelineSnapshots(List<MatchSnapshot> allSnapshots)
    {
        // Ensure snapshots are ordered by timestamp
        var orderedSnapshots = allSnapshots.OrderBy(s => s.Timestamp).ToList();

        // Initialize result collection with capacity for 9 segments
        var result = new List<MatchSnapshot>(9);

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
            (80, 90) // Plus any added time
        };

        // Track which snapshots we've already used to avoid duplication
        var usedSnapshotIds = new HashSet<ObjectId>();

        // Dictionary to map time ranges to snapshots
        var timeRangeSnapshots = new Dictionary<(int, int), MatchSnapshot>();

        // Find best snapshot for each time range
        foreach (var (start, end) in timeRanges)
        {
            // First, find snapshots exactly within this range
            var rangeSnapshots = orderedSnapshots
                .Where(s =>
                {
                    var minutes = ParsePlayedTime(s.PlayedTime);
                    return minutes >= start && minutes < end;
                })
                .ToList();

            if (rangeSnapshots.Any())
            {
                // Find the snapshot closest to the middle of this range
                var middleTime = start + (end - start) / 2;
                var bestSnapshot = rangeSnapshots
                    .OrderBy(s => Math.Abs(ParsePlayedTime(s.PlayedTime) - middleTime))
                    .First();

                timeRangeSnapshots[(start, end)] = bestSnapshot;
            }
        }

        // Now we need to handle time ranges with no data
        // First, add all the snapshots we found for specific ranges
        foreach (var (start, end) in timeRanges)
        {
            if (timeRangeSnapshots.TryGetValue((start, end), out var snapshot))
            {
                // Add this snapshot if we haven't used it already
                if (!usedSnapshotIds.Contains(snapshot.Id))
                {
                    result.Add(snapshot);
                    usedSnapshotIds.Add(snapshot.Id);
                }
            }
            else
            {
                // For missing ranges, find the closest available snapshot by played time
                var middleOfRange = start + (end - start) / 2;
                var closestSnapshot = orderedSnapshots
                    .OrderBy(s => Math.Abs(ParsePlayedTime(s.PlayedTime) - middleOfRange))
                    .FirstOrDefault(s => !usedSnapshotIds.Contains(s.Id));

                if (closestSnapshot != null)
                {
                    result.Add(closestSnapshot);
                    usedSnapshotIds.Add(closestSnapshot.Id);
                }
                else
                {
                    // If we've used all snapshots already, note that we have missing data
                    // Instead of duplicating, we'll add placeholder information with null
                    _logger.LogWarning(
                        $"Missing representative snapshot for time range {start}-{end} and no unused snapshots available");

                    // We don't add anything in this case - this will result in fewer than 9 snapshots
                    // which is better than having duplicated snapshots
                }
            }
        }

        // Now we have a list of unique snapshots representing the timeline
        // Log information about coverage
        _logger.LogInformation($"Found {result.Count} distinct snapshots across 9 time segments");

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