using System.Text.Json.Serialization;

namespace FredApi.Model.Historical.StatsSeasonOverUnder
{
    // Root response class
    public class StatsSeasonOverUnderResponse
    {
        [JsonPropertyName("queryUrl")]
        public string QueryUrl { get; set; }

        [JsonPropertyName("doc")]
        public List<StatsSeasonOverUnderDoc> Doc { get; set; }
    }

    public class StatsSeasonOverUnderDoc
    {
        [JsonPropertyName("event")]
        public string Event { get; set; }

        [JsonPropertyName("_dob")]
        public long Dob { get; set; }

        [JsonPropertyName("_maxage")]
        public int MaxAge { get; set; }

        [JsonPropertyName("data")]
        public StatsSeasonOverUnderData Data { get; set; }
    }

    public class StatsSeasonOverUnderData
    {
        [JsonPropertyName("season")]
        public Season Season { get; set; }

        // “values” is a simple mapping from a string key (e.g. "1", "2", etc.) to a threshold string.
        [JsonPropertyName("values")]
        public Dictionary<string, string> Values { get; set; }

        // “stats” is a dictionary where each key is a team ID (as string)
        // and the value holds the over/under stats for that team.
        [JsonPropertyName("stats")]
        public Dictionary<string, StatsOverUnderTeam> Stats { get; set; }

        // The league summary – totals and averages across teams.
        [JsonPropertyName("league")]
        public League League { get; set; }
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
        public TimeInfo Start { get; set; }

        [JsonPropertyName("end")]
        public TimeInfo End { get; set; }

        [JsonPropertyName("neutralground")]
        public bool NeutralGround { get; set; }

        [JsonPropertyName("friendly")]
        public bool Friendly { get; set; }

        [JsonPropertyName("currentseasonid")]
        public int CurrentSeasonId { get; set; }

        [JsonPropertyName("year")]
        public string Year { get; set; }
    }

    public class TimeInfo
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
        public int TzOffset { get; set; }

        [JsonPropertyName("uts")]
        public long Uts { get; set; }
    }

    // This class holds over/under stats for a single team.
    public class StatsOverUnderTeam
    {
        [JsonPropertyName("team")]
        public Team Team { get; set; }

        // Some teams include properties such as matches, homematches, awaymatches.
        [JsonPropertyName("matches")]
        public int? Matches { get; set; }

        [JsonPropertyName("homematches")]
        public int? HomeMatches { get; set; }

        [JsonPropertyName("awaymatches")]
        public int? AwayMatches { get; set; }

        // “winrate” is provided as a dictionary keyed by phase ("p1", "ft", "p2")
        [JsonPropertyName("winrate")]
        public Dictionary<string, WinRate> WinRate { get; set; }

        // Goals scored and conceded broken down by phase.
        [JsonPropertyName("goalsscored")]
        public Dictionary<string, GoalsData> GoalsScored { get; set; }

        [JsonPropertyName("conceded")]
        public Dictionary<string, GoalsData> Conceded { get; set; }

        // The over/under statistics are provided in three groups:
        // "total", "home", and "away". Each is a dictionary keyed by a phase ("p1", "ft", "p2")
        // whose value is itself a dictionary with keys equal to the threshold values (e.g. "0.5", "1.5", …)
        [JsonPropertyName("total")]
        public Dictionary<string, Dictionary<string, OverUnderData>> Total { get; set; }

        [JsonPropertyName("home")]
        public Dictionary<string, Dictionary<string, OverUnderData>> Home { get; set; }

        [JsonPropertyName("away")]
        public Dictionary<string, Dictionary<string, OverUnderData>> Away { get; set; }
    }

    public class WinRate
    {
        [JsonPropertyName("matches")]
        public int Matches { get; set; }

        [JsonPropertyName("wins")]
        public int Wins { get; set; }

        [JsonPropertyName("average")]
        public double Average { get; set; }
    }

    public class GoalsData
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("average")]
        public double Average { get; set; }

        [JsonPropertyName("atleastonegoal")]
        public int AtLeastOneGoal { get; set; }

        [JsonPropertyName("matches")]
        public int Matches { get; set; }
    }

    // OverUnderData holds counts for “over” and “under” for a given threshold.
    public class OverUnderData
    {
        [JsonPropertyName("over")]
        public int Over { get; set; }

        [JsonPropertyName("under")]
        public int Under { get; set; }
    }

    public class Team
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }

        [JsonPropertyName("_id")]
        public int Id { get; set; }

        [JsonPropertyName("_sid")]
        public int Sid { get; set; }

        [JsonPropertyName("uid")]
        public int Uid { get; set; }

        [JsonPropertyName("virtual")]
        public bool Virtual { get; set; }

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

    // The league-level summary information (totals, averages, and aggregated over/under data).
    public class League
    {
        [JsonPropertyName("totals")]
        public LeagueTotals Totals { get; set; }
    }

    public class LeagueTotals
    {
        [JsonPropertyName("matches")]
        public int Matches { get; set; }

        // Goals scored and conceded aggregates per phase (p1, ft, p2).
        [JsonPropertyName("goalsscored")]
        public Dictionary<string, LeagueGoalData> GoalsScored { get; set; }

        [JsonPropertyName("conceded")]
        public Dictionary<string, LeagueGoalData> Conceded { get; set; }

        // Over/under breakdowns per phase.
        [JsonPropertyName("p1")]
        public Dictionary<string, LeagueOverUnder> P1 { get; set; }

        [JsonPropertyName("ft")]
        public Dictionary<string, LeagueOverUnder> FT { get; set; }

        [JsonPropertyName("p2")]
        public Dictionary<string, LeagueOverUnder> P2 { get; set; }
    }

    public class LeagueGoalData
    {
        [JsonPropertyName("average")]
        public double Average { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("matches")]
        public int Matches { get; set; }
    }

    public class LeagueOverUnder
    {
        [JsonPropertyName("totalover")]
        public int TotalOver { get; set; }

        [JsonPropertyName("over")]
        public double Over { get; set; }

        [JsonPropertyName("under")]
        public double Under { get; set; }
    }
}
