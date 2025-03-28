using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace fredapi.Model
{
    public class TeamScoringConcedingModel
    {
        [JsonPropertyName("team")]
        public TeamInfo Team { get; set; }

        [JsonPropertyName("stats")]
        public TeamStats Stats { get; set; }

        // These properties will be populated from the doc wrapper structure
        [JsonPropertyName("queryUrl")]
        public string QueryUrl { get; set; }

        [JsonPropertyName("event")]
        public string Event { get; set; }

        [JsonPropertyName("_dob")]
        public long Dob { get; set; }

        [JsonPropertyName("_maxage")]
        public int Maxage { get; set; }
    }

    public class TeamStats
    {
        [JsonPropertyName("totalmatches")]
        public MatchCount TotalMatches { get; set; }

        [JsonPropertyName("totalwins")]
        public MatchCount TotalWins { get; set; }

        [JsonPropertyName("scoring")]
        public ScoringStats Scoring { get; set; }

        [JsonPropertyName("conceding")]
        public ConcedingStats Conceding { get; set; }

        [JsonPropertyName("averagegoalsbyminutes")]
        public Averages AverageGoalsByMinutes { get; set; }
    }

    public class MatchCount
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("home")]
        public int Home { get; set; }

        [JsonPropertyName("away")]
        public int Away { get; set; }
    }

    public class ScoringStats
    {
        [JsonPropertyName("goalsscored")]
        public MatchCount GoalsScored { get; set; }

        [JsonPropertyName("atleastonegoal")]
        public MatchCount AtLeastOneGoal { get; set; }

        [JsonPropertyName("failedtoscore")]
        public MatchCount FailedToScore { get; set; }

        [JsonPropertyName("scoringathalftime")]
        public MatchCount ScoringAtHalftime { get; set; }

        [JsonPropertyName("scoringatfulltime")]
        public MatchCount ScoringAtFulltime { get; set; }

        [JsonPropertyName("bothteamsscored")]
        public MatchCount BothTeamsScored { get; set; }

        [JsonPropertyName("goalsscoredaverage")]
        public Averages GoalsScoredAverage { get; set; }

        [JsonPropertyName("atleastonegoalaverage")]
        public Averages AtLeastOneGoalAverage { get; set; }

        [JsonPropertyName("failedtoscoreaverage")]
        public Averages FailedToScoreAverage { get; set; }

        [JsonPropertyName("scoringathalftimeaverage")]
        public Averages ScoringAtHalftimeAverage { get; set; }

        [JsonPropertyName("scoringatfulltimeaverage")]
        public Averages ScoringAtFulltimeAverage { get; set; }

        [JsonPropertyName("goalmarginatvictoryaverage")]
        public Averages GoalMarginAtVictoryAverage { get; set; }

        [JsonPropertyName("halftimegoalmarginatvictoryaverage")]
        public Averages HalftimeGoalMarginAtVictoryAverage { get; set; }

        [JsonPropertyName("bothteamsscoredaverage")]
        public Averages BothTeamsScoredAverage { get; set; }

        [JsonPropertyName("goalsbyminutes")]
        public Dictionary<string, int> GoalsByMinutes { get; set; } = new();
    }

    public class ConcedingStats
    {
        [JsonPropertyName("goalsconceded")]
        public MatchCount GoalsConceded { get; set; }

        [JsonPropertyName("cleansheets")]
        public MatchCount CleanSheets { get; set; }

        [JsonPropertyName("goalsconcededfirsthalf")]
        public MatchCount GoalsConcededFirstHalf { get; set; }

        [JsonPropertyName("goalsconcededaverage")]
        public Averages GoalsConcededAverage { get; set; }

        [JsonPropertyName("cleansheetsaverage")]
        public Averages CleanSheetsAverage { get; set; }

        [JsonPropertyName("goalsconcededfirsthalfaverage")]
        public Averages GoalsConcededFirstHalfAverage { get; set; }

        [JsonPropertyName("minutespergoalconceded")]
        public Averages MinutesPerGoalConceded { get; set; }

        [JsonPropertyName("goalsbyminutes")]
        public Dictionary<string, int> GoalsByMinutes { get; set; } = new();
    }

    public class Averages
    {
        [JsonPropertyName("total")]
        public double Total { get; set; }

        [JsonPropertyName("home")]
        public double Home { get; set; }

        [JsonPropertyName("away")]
        public double Away { get; set; }
    }
}