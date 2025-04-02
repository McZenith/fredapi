using System.Text.Json.Serialization;

namespace fredapi.Model.Historical.StatsSeasonUniqueteamstatsResponse
{
    public class StatsSeasonUniqueteamstatsResponse
    {
        [JsonPropertyName("queryUrl")]
        public string QueryUrl { get; set; }
        
        [JsonPropertyName("doc")]
        public List<Doc> Doc { get; set; }
    }

    public class Doc
    {
        // "event" is a reserved C# word so we map it to EventName.
        [JsonPropertyName("event")]
        public string EventName { get; set; }
        
        [JsonPropertyName("_dob")]
        public long Dob { get; set; }
        
        [JsonPropertyName("_maxage")]
        public int MaxAge { get; set; }
        
        [JsonPropertyName("data")]
        public Data Data { get; set; }
    }

    public class Data
    {
        [JsonPropertyName("season")]
        public Season Season { get; set; }
        
        [JsonPropertyName("stats")]
        public Stats Stats { get; set; }
    }

    public class Season
    {
        [JsonPropertyName("_id")]
        public string Id { get; set; }
        
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }
        
        [JsonPropertyName("_utid")]
        public int Utid { get; set; }
        
        [JsonPropertyName("_sid")]
        public int Sid { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("abbr")]
        public string Abbr { get; set; }
        
        [JsonPropertyName("start")]
        public TimeData Start { get; set; }
        
        [JsonPropertyName("end")]
        public TimeData End { get; set; }
        
        [JsonPropertyName("neutralground")]
        public bool NeutralGround { get; set; }
        
        [JsonPropertyName("friendly")]
        public bool Friendly { get; set; }
        
        [JsonPropertyName("currentseasonid")]
        public int CurrentSeasonId { get; set; }
        
        [JsonPropertyName("year")]
        public string Year { get; set; }
    }

    public class TimeData
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }
        
        [JsonPropertyName("time")]
        public string Time { get; set; }
        
        [JsonPropertyName("date")]
        public string Date { get; set; }
        
        [JsonPropertyName("tz")]
        public string Tz { get; set; }
        
        [JsonPropertyName("tzoffset")]
        public int Tzoffset { get; set; }
        
        [JsonPropertyName("uts")]
        public long Uts { get; set; }
    }

    public class Stats
    {
        // The "uniqueteams" property is an object with dynamic keys (team IDs)
        [JsonPropertyName("uniqueteams")]
        public Dictionary<string, UniqueTeamStats> UniqueTeams { get; set; }
    }

    public class UniqueTeamStats
    {
        [JsonPropertyName("uniqueteam")]
        public UniqueTeam UniqueTeam { get; set; }

        // Each statistical category is represented as an object.
        [JsonPropertyName("goal_attempts")]
        public StatDetail GoalAttempts { get; set; }
        
        [JsonPropertyName("shots_on_goal")]
        public StatDetail ShotsOnGoal { get; set; }
        
        [JsonPropertyName("shots_off_goal")]
        public StatDetail ShotsOffGoal { get; set; }
        
        [JsonPropertyName("corner_kicks")]
        public StatDetail CornerKicks { get; set; }
        
        [JsonPropertyName("ball_possession")]
        public StatDetail BallPossession { get; set; }
        
        [JsonPropertyName("shots_blocked")]
        public StatDetail ShotsBlocked { get; set; }
        
        [JsonPropertyName("cards_given")]
        public StatDetail CardsGiven { get; set; }
        
        [JsonPropertyName("freekicks")]
        public StatDetail Freekicks { get; set; }
        
        [JsonPropertyName("offside")]
        public StatDetail Offside { get; set; }
        
        [JsonPropertyName("shots_on_post")]
        public StatDetail ShotsOnPost { get; set; }
        
        [JsonPropertyName("shots_on_bar")]
        public StatDetail ShotsOnBar { get; set; }
        
        [JsonPropertyName("goals_by_foot")]
        public StatDetail GoalsByFoot { get; set; }
        
        [JsonPropertyName("goals_by_head")]
        public StatDetail GoalsByHead { get; set; }
        
        [JsonPropertyName("attendance")]
        public StatDetail Attendance { get; set; }
        
        [JsonPropertyName("yellow_cards")]
        public StatDetail YellowCards { get; set; }
        
        [JsonPropertyName("red_cards")]
        public StatDetail RedCards { get; set; }
        
        [JsonPropertyName("goals_scored")]
        public StatDetail GoalsScored { get; set; }
        
        [JsonPropertyName("goals_conceded")]
        public StatDetail GoalsConceded { get; set; }
        
        [JsonPropertyName("yellowred_cards")]
        public StatDetail YellowRedCards { get; set; }
        
        [JsonPropertyName("shootingefficiency")]
        public StatDetail ShootingEfficiency { get; set; }
        
        // Some stats come only as totals (often as strings).
        [JsonPropertyName("late_winning_goals")]
        public TotalStat LateWinningGoals { get; set; }
        
        [JsonPropertyName("penalty_success_count")]
        public TotalStat PenaltySuccessCount { get; set; }
        
        [JsonPropertyName("penalty_fail_count")]
        public TotalStat PenaltyFailCount { get; set; }
    }

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
        public object Suffix { get; set; }
        
        [JsonPropertyName("abbr")]
        public string Abbr { get; set; }
        
        [JsonPropertyName("nickname")]
        public object Nickname { get; set; }
        
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
        public object Website { get; set; }
    }

    public class StatDetail
    {
        [JsonPropertyName("average")]
        public double Average { get; set; }
        
        // In some cases the total might be numeric or a string (e.g. "32/299"). 
        // To cover both cases, we can declare Total as an object.
        [JsonPropertyName("total")]
        public object Total { get; set; }
        
        [JsonPropertyName("matches")]
        public int Matches { get; set; }
    }

    // For properties that only contain a total (as string) use a simple wrapper.
    public class TotalStat
    {
        [JsonPropertyName("total")]
        public string Total { get; set; }
    }
}
