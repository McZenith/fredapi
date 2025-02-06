using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace fredapi.Model.Historical
{
    // Root response for the stats_season_teamscoringconceding endpoint.
    public class SeasonTeamScoringConcedingResponse
    {
        [JsonPropertyName("queryUrl")]
        public string QueryUrl { get; set; }
        
        [JsonPropertyName("doc")]
        public List<SeasonTeamScoringConcedingDoc> Documents { get; set; }
    }

    public class SeasonTeamScoringConcedingDoc
    {
        [JsonPropertyName("event")]
        public string Event { get; set; }
        
        [JsonPropertyName("_dob")]
        public long Dob { get; set; }
        
        [JsonPropertyName("_maxage")]
        public int MaxAge { get; set; }
        
        [JsonPropertyName("data")]
        public SeasonTeamScoringConcedingData Data { get; set; }
    }

    public class SeasonTeamScoringConcedingData
    {
        [JsonPropertyName("team")]
        public UniqueTeam Team { get; set; }
        
        [JsonPropertyName("stats")]
        public TeamStats Stats { get; set; }
    }

    // Represents a team (the "uniqueteam" object).
    public class UniqueTeam
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }
        
        [JsonPropertyName("_id")]
        public int Id { get; set; }
        
        [JsonPropertyName("_rcid")]
        public int RcId { get; set; }
        
        [JsonPropertyName("_sid")]
        public int Sid { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("mediumname")]
        public string MediumName { get; set; }
        
        [JsonPropertyName("suffix")]
        public string Suffix { get; set; }
        
        [JsonPropertyName("abbr")]
        public string Abbr { get; set; }
        
        [JsonPropertyName("nickname")]
        public string Nickname { get; set; }
        
        [JsonPropertyName("teamtypeid")]
        public int TeamTypeId { get; set; }
        
        [JsonPropertyName("iscountry")]
        public bool IsCountry { get; set; }
        
        [JsonPropertyName("sex")]
        public string Sex { get; set; }
        
        [JsonPropertyName("haslogo")]
        public bool HasLogo { get; set; }
        
        [JsonPropertyName("founded")]
        public string Founded { get; set; }
        
        [JsonPropertyName("website")]
        public string Website { get; set; }
    }

    // Holds the overall statistics for scoring and conceding.
    public class TeamStats
    {
        [JsonPropertyName("totalmatches")]
        public CountStat TotalMatches { get; set; }
        
        [JsonPropertyName("totalwins")]
        public CountStat TotalWins { get; set; }
        
        [JsonPropertyName("scoring")]
        public ScoringStats Scoring { get; set; }
        
        [JsonPropertyName("conceding")]
        public ConcedingStats Conceding { get; set; }
        
        [JsonPropertyName("averagegoalsbyminutes")]
        public Dictionary<string, double> AverageGoalsByMinutes { get; set; }
    }

    // For values given as a count (typically integers).
    public class CountStat
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }
        
        [JsonPropertyName("home")]
        public int Home { get; set; }
        
        [JsonPropertyName("away")]
        public int Away { get; set; }
    }

    // For average values (as decimals/doubles).
    public class AverageStat
    {
        [JsonPropertyName("total")]
        public double Total { get; set; }
        
        [JsonPropertyName("home")]
        public double Home { get; set; }
        
        [JsonPropertyName("away")]
        public double Away { get; set; }
    }

    // For the nested "goalsbyminutes" objects.
    public class GoalsByMinutesStat
    {
        [JsonPropertyName("total")]
        public double Total { get; set; }
        
        [JsonPropertyName("home")]
        public double Home { get; set; }
        
        [JsonPropertyName("away")]
        public double Away { get; set; }
    }

    // Statistics related to scoring.
    public class ScoringStats
    {
        [JsonPropertyName("goalsscored")]
        public CountStat GoalsScored { get; set; }
        
        [JsonPropertyName("atleastonegoal")]
        public CountStat AtLeastOneGoal { get; set; }
        
        [JsonPropertyName("failedtoscore")]
        public CountStat FailedToScore { get; set; }
        
        [JsonPropertyName("scoringathalftime")]
        public CountStat ScoringAtHalftime { get; set; }
        
        [JsonPropertyName("scoringatfulltime")]
        public CountStat ScoringAtFulltime { get; set; }
        
        [JsonPropertyName("bothteamsscored")]
        public CountStat BothTeamsScored { get; set; }
        
        [JsonPropertyName("goalsscoredaverage")]
        public AverageStat GoalsScoredAverage { get; set; }
        
        [JsonPropertyName("atleastonegoalaverage")]
        public AverageStat AtLeastOneGoalAverage { get; set; }
        
        [JsonPropertyName("failedtoscoreaverage")]
        public AverageStat FailedToScoreAverage { get; set; }
        
        [JsonPropertyName("scoringathalftimeaverage")]
        public AverageStat ScoringAtHalftimeAverage { get; set; }
        
        [JsonPropertyName("scoringatfulltimeaverage")]
        public AverageStat ScoringAtFulltimeAverage { get; set; }
        
        [JsonPropertyName("goalmarginatvictoryaverage")]
        public AverageStat GoalMarginAtVictoryAverage { get; set; }
        
        [JsonPropertyName("halftimegoalmarginatvictoryaverage")]
        public AverageStat HalftimeGoalMarginAtVictoryAverage { get; set; }
        
        [JsonPropertyName("bothteamsscoredaverage")]
        public AverageStat BothTeamsScoredAverage { get; set; }
        
        [JsonPropertyName("goalsbyminutes")]
        public Dictionary<string, GoalsByMinutesStat> GoalsByMinutes { get; set; }
    }

    // Statistics related to conceding.
    public class ConcedingStats
    {
        [JsonPropertyName("goalsconceded")]
        public CountStat GoalsConceded { get; set; }
        
        [JsonPropertyName("cleansheets")]
        public CountStat CleanSheets { get; set; }
        
        [JsonPropertyName("goalsconcededfirsthalf")]
        public CountStat GoalsConcededFirstHalf { get; set; }
        
        [JsonPropertyName("goalsconcededaverage")]
        public AverageStat GoalsConcededAverage { get; set; }
        
        [JsonPropertyName("cleansheetsaverage")]
        public AverageStat CleanSheetsAverage { get; set; }
        
        [JsonPropertyName("goalsconcededfirsthalfaverage")]
        public AverageStat GoalsConcededFirstHalfAverage { get; set; }
        
        [JsonPropertyName("minutespergoalconceded")]
        public AverageStat MinutesPerGoalConceded { get; set; }
        
        [JsonPropertyName("goalsbyminutes")]
        public Dictionary<string, GoalsByMinutesStat> GoalsByMinutes { get; set; }
    }
}
