using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace fredapi.Model.Historical.TeamFixturesResponse
{
    // Top‐level response class
    public class TeamFixturesResponse
    {
        [JsonPropertyName("queryUrl")]
        public string QueryUrl { get; set; }
        
        [JsonPropertyName("doc")]
        public List<TeamFixturesDoc> Documents { get; set; }
    }

    public class TeamFixturesDoc
    {
        [JsonPropertyName("event")]
        public string Event { get; set; }

        [JsonPropertyName("_dob")]
        public long Dob { get; set; }

        [JsonPropertyName("_maxage")]
        public int MaxAge { get; set; }

        [JsonPropertyName("data")]
        public TeamFixturesData Data { get; set; }
    }

    public class TeamFixturesData
    {
        [JsonPropertyName("season")]
        public Season Season { get; set; }
        
        [JsonPropertyName("matches")]
        public List<Match> Matches { get; set; }
        
        // Tournaments keyed by their id (e.g. "37")
        [JsonPropertyName("tournaments")]
        public Dictionary<string, Tournament> Tournaments { get; set; }
        
        // Cups may be empty; if needed define a Cup class.
        [JsonPropertyName("cups")]
        public Dictionary<string, object> Cups { get; set; }
        
        // Tables keyed by their id (e.g. "84355")
        [JsonPropertyName("tables")]
        public Dictionary<string, LeagueTable> Tables { get; set; }
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
        public string TimeZone { get; set; }
        
        [JsonPropertyName("tzoffset")]
        public int TimeZoneOffset { get; set; }
        
        [JsonPropertyName("uts")]
        public long Uts { get; set; }
    }

    public class Match
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }
        
        [JsonPropertyName("_id")]
        public long Id { get; set; }
        
        [JsonPropertyName("_sid")]
        public int Sid { get; set; }
        
        [JsonPropertyName("_rcid")]
        public int RcId { get; set; }
        
        [JsonPropertyName("_tid")]
        public int Tid { get; set; }
        
        [JsonPropertyName("_utid")]
        public int Utid { get; set; }
        
        [JsonPropertyName("time")]
        public TimeInfo Time { get; set; }
        
        [JsonPropertyName("round")]
        public int Round { get; set; }
        
        [JsonPropertyName("roundname")]
        public RoundName RoundName { get; set; }
        
        [JsonPropertyName("week")]
        public int Week { get; set; }
        
        [JsonPropertyName("result")]
        public Result Result { get; set; }
        
        // "periods" can be null (for not–yet–played matches) or defined.
        [JsonPropertyName("periods")]
        public Periods Periods { get; set; }
        
        [JsonPropertyName("_seasonid")]
        public int SeasonId { get; set; }
        
        [JsonPropertyName("teams")]
        public Teams Teams { get; set; }
        
        [JsonPropertyName("neutralground")]
        public bool NeutralGround { get; set; }
        
        [JsonPropertyName("comment")]
        public string Comment { get; set; }
        
        // Other flags
        [JsonPropertyName("status")]
        public object Status { get; set; }
        
        [JsonPropertyName("tobeannounced")]
        public bool ToBeAnnounced { get; set; }
        
        [JsonPropertyName("postponed")]
        public bool Postponed { get; set; }
        
        [JsonPropertyName("canceled")]
        public bool Canceled { get; set; }
        
        [JsonPropertyName("inlivescore")]
        public bool InLiveScore { get; set; }
        
        [JsonPropertyName("stadiumid")]
        public int StadiumId { get; set; }
        
        [JsonPropertyName("bestof")]
        public object BestOf { get; set; }
        
        [JsonPropertyName("walkover")]
        public bool WalkOver { get; set; }
        
        [JsonPropertyName("retired")]
        public bool Retired { get; set; }
        
        [JsonPropertyName("disqualified")]
        public bool Disqualified { get; set; }
    }

    public class RoundName
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }
        
        [JsonPropertyName("_id")]
        public int Id { get; set; }
        
        [JsonPropertyName("name")]
        public int Name { get; set; }
    }

    public class Result
    {
        [JsonPropertyName("home")]
        public int? Home { get; set; }
        
        [JsonPropertyName("away")]
        public int? Away { get; set; }
        
        [JsonPropertyName("period")]
        public string Period { get; set; }
        
        [JsonPropertyName("winner")]
        public string Winner { get; set; }
    }

    public class Periods
    {
        // Example: "p1": { "home": 1, "away": 0 }, "ft": { "home": 1, "away": 0 }
        [JsonPropertyName("p1")]
        public ScoreDetail P1 { get; set; }
        
        [JsonPropertyName("ft")]
        public ScoreDetail FT { get; set; }
        
        // Add other period keys if needed
    }

    public class ScoreDetail
    {
        [JsonPropertyName("home")]
        public int? Home { get; set; }
        
        [JsonPropertyName("away")]
        public int? Away { get; set; }
    }

    public class Teams
    {
        [JsonPropertyName("home")]
        public Team Home { get; set; }
        
        [JsonPropertyName("away")]
        public Team Away { get; set; }
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
        
        [JsonPropertyName("abbr")]
        public string Abbr { get; set; }
        
        [JsonPropertyName("nickname")]
        public string Nickname { get; set; }
        
        [JsonPropertyName("iscountry")]
        public bool IsCountry { get; set; }
        
        [JsonPropertyName("haslogo")]
        public bool HasLogo { get; set; }
    }

    public class Tournament
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }
        
        [JsonPropertyName("_id")]
        public int Id { get; set; }
        
        [JsonPropertyName("_sid")]
        public int Sid { get; set; }
        
        [JsonPropertyName("_rcid")]
        public int RcId { get; set; }
        
        [JsonPropertyName("_isk")]
        public int Isk { get; set; }
        
        [JsonPropertyName("_tid")]
        public int Tid { get; set; }
        
        [JsonPropertyName("_utid")]
        public int Utid { get; set; }
        
        [JsonPropertyName("_gender")]
        public string Gender { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("abbr")]
        public string Abbr { get; set; }
        
        [JsonPropertyName("ground")]
        public object Ground { get; set; }
        
        [JsonPropertyName("friendly")]
        public bool Friendly { get; set; }
        
        [JsonPropertyName("seasonid")]
        public int SeasonId { get; set; }
        
        [JsonPropertyName("currentseason")]
        public int CurrentSeason { get; set; }
        
        [JsonPropertyName("year")]
        public string Year { get; set; }
        
        [JsonPropertyName("seasontype")]
        public string SeasonType { get; set; }
        
        [JsonPropertyName("seasontypename")]
        public string SeasonTypeName { get; set; }
        
        [JsonPropertyName("seasontypeunique")]
        public string SeasonTypeUnique { get; set; }
        
        [JsonPropertyName("livetable")]
        public int LiveTable { get; set; }
        
        [JsonPropertyName("cuprosterid")]
        public object CupRosterId { get; set; }
        
        [JsonPropertyName("roundbyround")]
        public bool RoundByRound { get; set; }
        
        [JsonPropertyName("tournamentlevelorder")]
        public int TournamentLevelOrder { get; set; }
        
        [JsonPropertyName("tournamentlevelname")]
        public string TournamentLevelName { get; set; }
        
        [JsonPropertyName("outdated")]
        public bool Outdated { get; set; }
        
        // For convenience, include a list of match IDs for the tournament
        [JsonPropertyName("matches")]
        public List<long> Matches { get; set; }
    }

    // The LeagueTable class here is modeled similarly to other endpoints.
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
        public int MaxRounds { get; set; }
        
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
        public int? Order { get; set; }
        
        // The list of match IDs used in the table
        [JsonPropertyName("matches")]
        public List<long> Matches { get; set; }
    }
}
