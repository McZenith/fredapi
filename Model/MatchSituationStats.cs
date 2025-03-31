using System.Text.Json.Serialization;

namespace fredapi.Model.MatchSituationStats;

public class MatchSituationStats
{
    [JsonPropertyName("matchid")]
    public string MatchId { get; set; }

    [JsonPropertyName("data")]
    public List<TimeSliceStats> Data { get; set; }

    public AggregatedStats AggregatedStats { get; set; }
}

public class TimeSliceStats
{
    [JsonPropertyName("time")]
    public int Time { get; set; }

    [JsonPropertyName("injurytime")]
    public int InjuryTime { get; set; }

    [JsonPropertyName("home")]
    public TeamStats Home { get; set; }

    [JsonPropertyName("away")]
    public TeamStats Away { get; set; }
}

public class TeamStats
{
    [JsonPropertyName("attack")]
    public int Attack { get; set; }

    [JsonPropertyName("dangerous")]
    public int Dangerous { get; set; }

    [JsonPropertyName("safe")]
    public int Safe { get; set; }

    [JsonPropertyName("attackcount")]
    public int AttackCount { get; set; }

    [JsonPropertyName("dangerouscount")]
    public int DangerousCount { get; set; }

    [JsonPropertyName("safecount")]
    public int SafeCount { get; set; }
}

public class AggregatedStats
{
    public TeamAggregatedStats Home { get; set; }
    public TeamAggregatedStats Away { get; set; }
    public int TotalTime { get; set; }
    public string DominantTeam { get; set; }
    public string MatchMomentum { get; set; }
}

public class TeamAggregatedStats
{
    public int TotalAttacks { get; set; }
    public int TotalDangerousAttacks { get; set; }
    public int TotalSafeAttacks { get; set; }
    public int TotalAttackCount { get; set; }
    public int TotalDangerousCount { get; set; }
    public int TotalSafeCount { get; set; }
    public double AttackPercentage { get; set; }
    public double DangerousAttackPercentage { get; set; }
    public double SafeAttackPercentage { get; set; }
    public List<TeamStats> Last5Minutes { get; set; }
}