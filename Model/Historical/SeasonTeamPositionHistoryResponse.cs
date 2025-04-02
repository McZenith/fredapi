using System.Text.Json.Serialization;

namespace fredapi.Model.Historical.SeasonTeamPositionHistoryResponse
{
    // Root response for the stats_season_teampositionhistory endpoint.
    public class SeasonTeamPositionHistoryResponse
    {
        [JsonPropertyName("queryUrl")]
        public string QueryUrl { get; set; }
        
        [JsonPropertyName("doc")]
        public List<SeasonTeamPositionHistoryDoc> Documents { get; set; }
    }

    public class SeasonTeamPositionHistoryDoc
    {
        [JsonPropertyName("event")]
        public string Event { get; set; }
        
        [JsonPropertyName("_dob")]
        public long Dob { get; set; }
        
        [JsonPropertyName("_maxage")]
        public int MaxAge { get; set; }
        
        [JsonPropertyName("data")]
        public SeasonTeamPositionHistoryData Data { get; set; }
    }

    public class SeasonTeamPositionHistoryData
    {
        // Dictionary keyed by team id (as string) with details for each team.
        [JsonPropertyName("teams")]
        public Dictionary<string, UniqueTeam> Teams { get; set; }
        
        [JsonPropertyName("teamcount")]
        public int TeamCount { get; set; }
        
        [JsonPropertyName("roundcount")]
        public int RoundCount { get; set; }
        
        // Jersey colors for teams keyed by team id.
        [JsonPropertyName("jersey")]
        public Dictionary<string, Jersey> Jersey { get; set; }
        
        [JsonPropertyName("season")]
        public Season Season { get; set; }
        
        // Tables (typically the league table) keyed by table id.
        [JsonPropertyName("tables")]
        public Dictionary<string, LeagueTable> Tables { get; set; }
        
        // Position-related information for teams.
        // In this JSON the keys (such as "1", "2", etc.) hold promotion or relegation info.
        [JsonPropertyName("positiondata")]
        public Dictionary<string, Promotion> PositionData { get; set; }
        
        // For previous season (if any) keyed by team id.
        [JsonPropertyName("previousseason")]
        public Dictionary<string, SeasonPos> PreviousSeason { get; set; }
        
        // For the current season, each team id maps to a list of season–by–season position entries.
        [JsonPropertyName("currentseason")]
        public Dictionary<string, List<SeasonPos>> CurrentSeason { get; set; }
        
        // If any matches are missing from the history.
        [JsonPropertyName("matchesmissing")]
        public Dictionary<string, object> MatchesMissing { get; set; }
    }

    // A team as represented in this JSON (sometimes called “uniqueteam”).
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

    // A simple model for jersey colors and style.
    public class Jersey
    {
        [JsonPropertyName("base")]
        public string Base { get; set; }
        
        [JsonPropertyName("sleeve")]
        public string Sleeve { get; set; }
        
        [JsonPropertyName("number")]
        public string Number { get; set; }
        
        // The "stripes" property is sometimes present.
        [JsonPropertyName("stripes")]
        public string Stripes { get; set; }
        
        [JsonPropertyName("type")]
        public string Type { get; set; }
        
        [JsonPropertyName("real")]
        public bool Real { get; set; }
    }

    // Season information (reuse from other models as needed).
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

    // The LeagueTable model can be the same as in other endpoints.
    public class LeagueTable
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }
        
        [JsonPropertyName("_id")]
        public string Id { get; set; }
        
        [JsonPropertyName("parenttableid")]
        public object ParentTableId { get; set; }
        
        [JsonPropertyName("leaguetypeid")]
        public object LeagueTypeId { get; set; }
        
        [JsonPropertyName("parenttableids")]
        public Dictionary<string, object> ParentTableIds { get; set; }
        
        [JsonPropertyName("seasonid")]
        public string SeasonId { get; set; }
        
        [JsonPropertyName("maxrounds")]
        public string MaxRounds { get; set; }
        
        [JsonPropertyName("currentround")]
        public int CurrentRound { get; set; }
        
        [JsonPropertyName("presentationid")]
        public int PresentationId { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("abbr")]
        public string Abbr { get; set; }
        
        [JsonPropertyName("groupname")]
        public string GroupName { get; set; }
        
        [JsonPropertyName("tournamentid")]
        public int TournamentId { get; set; }
        
        [JsonPropertyName("seasontype")]
        public string SeasonType { get; set; }
        
        [JsonPropertyName("seasontypename")]
        public string SeasonTypeName { get; set; }
        
        [JsonPropertyName("seasontypeunique")]
        public string SeasonTypeUnique { get; set; }
        
        [JsonPropertyName("start")]
        public TimeInfo Start { get; set; }
        
        [JsonPropertyName("end")]
        public TimeInfo End { get; set; }
        
        [JsonPropertyName("roundbyround")]
        public bool RoundByRound { get; set; }
        
        [JsonPropertyName("order")]
        public object Order { get; set; }
    }

    // Promotion (or position status) as provided in the "positiondata" section.
    public class Promotion
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }
        
        [JsonPropertyName("_id")]
        public int Id { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("shortname")]
        public string ShortName { get; set; }
        
        [JsonPropertyName("cssclass")]
        public string CssClass { get; set; }
        
        [JsonPropertyName("position")]
        public int Position { get; set; }
    }

    // A season position history record.
    public class SeasonPos
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }
        
        [JsonPropertyName("round")]
        public int Round { get; set; }
        
        [JsonPropertyName("position")]
        public int Position { get; set; }
        
        [JsonPropertyName("seasonid")]
        public int SeasonId { get; set; }
        
        [JsonPropertyName("matchid")]
        public long MatchId { get; set; }
    }
}
