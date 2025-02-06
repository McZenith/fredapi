using System.Text.Json.Serialization;

namespace fredapi.Model.Historical.StatsSeasonTablesResponse
{
    // Root response class
    public class StatsSeasonTablesResponse
    {
        [JsonPropertyName("queryUrl")]
        public string QueryUrl { get; set; }

        [JsonPropertyName("doc")]
        public List<StatsSeasonTablesDoc> Documents { get; set; }
    }

    public class StatsSeasonTablesDoc
    {
        [JsonPropertyName("event")]
        public string Event { get; set; }

        [JsonPropertyName("_dob")]
        public long Dob { get; set; }

        [JsonPropertyName("_maxage")]
        public int MaxAge { get; set; }

        [JsonPropertyName("data")]
        public SeasonTablesData Data { get; set; }
    }

    public class SeasonTablesData
    {
        // Season-level info
        [JsonPropertyName("_id")]
        public string SeasonId { get; set; }

        [JsonPropertyName("_doc")]
        public string Doc { get; set; }

        [JsonPropertyName("_utid")]
        public int Utid { get; set; }

        [JsonPropertyName("_sid")]
        public int Sid { get; set; }

        [JsonPropertyName("name")]
        public string SeasonName { get; set; }

        [JsonPropertyName("abbr")]
        public string SeasonAbbr { get; set; }

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

        // The list of tables for this season
        [JsonPropertyName("tables")]
        public List<LeagueTable> Tables { get; set; }
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

    public class LeagueTable
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }

        [JsonPropertyName("_id")]
        public string TableId { get; set; }

        [JsonPropertyName("parenttableid")]
        public object ParentTableId { get; set; }  // may be null

        [JsonPropertyName("leaguetypeid")]
        public object LeagueTypeId { get; set; }   // may be null

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
        public string TableName { get; set; }

        [JsonPropertyName("abbr")]
        public string TableAbbr { get; set; }

        [JsonPropertyName("groupname")]
        public string GroupName { get; set; }

        [JsonPropertyName("tournament")]
        public Tournament Tournament { get; set; }

        [JsonPropertyName("realcategory")]
        public RealCategory RealCategory { get; set; }

        [JsonPropertyName("rules")]
        public TiebreakRule Rules { get; set; }

        [JsonPropertyName("totalrows")]
        public int TotalRows { get; set; }

        [JsonPropertyName("tablerows")]
        public List<TableRow> TableRows { get; set; }

        // Additional properties from the season tables object
        [JsonPropertyName("tournamentid")]
        public int TournamentId { get; set; }

        [JsonPropertyName("seasontype")]
        public string SeasonType { get; set; }

        [JsonPropertyName("seasontypename")]
        public string SeasonTypeName { get; set; }

        [JsonPropertyName("seasontypeunique")]
        public string SeasonTypeUnique { get; set; }

        [JsonPropertyName("roundbyround")]
        public bool RoundByRound { get; set; }

        [JsonPropertyName("order")]
        public int? Order { get; set; }

        // "set" and "header" arrays (and matchtype, tabletype) can be added as needed.
        [JsonPropertyName("set")]
        public List<TableSet> Sets { get; set; }

        [JsonPropertyName("header")]
        public List<TableHeader> Header { get; set; }

        [JsonPropertyName("matchtype")]
        public List<MatchType> MatchTypes { get; set; }

        [JsonPropertyName("tabletype")]
        public List<TableType> TableTypes { get; set; }
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
        public object Ground { get; set; } // can be null

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
    }

    public class RealCategory
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }

        [JsonPropertyName("_id")]
        public int Id { get; set; }

        [JsonPropertyName("_sid")]
        public int Sid { get; set; }

        [JsonPropertyName("_rcid")]
        public int RcId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("cc")]
        public CountryCode CC { get; set; }
    }

    public class CountryCode
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }

        [JsonPropertyName("_id")]
        public int Id { get; set; }

        [JsonPropertyName("a2")]
        public string A2 { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("a3")]
        public string A3 { get; set; }

        [JsonPropertyName("ioc")]
        public string Ioc { get; set; }

        [JsonPropertyName("continentid")]
        public int ContinentId { get; set; }

        [JsonPropertyName("continent")]
        public string Continent { get; set; }

        [JsonPropertyName("population")]
        public int Population { get; set; }
    }

    public class TiebreakRule
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }

        [JsonPropertyName("_id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class TableRow
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }

        [JsonPropertyName("_id")]
        public long Id { get; set; }

        // Promotion information may be present
        [JsonPropertyName("promotion")]
        public Promotion Promotion { get; set; }

        [JsonPropertyName("changeTotal")]
        public int ChangeTotal { get; set; }

        [JsonPropertyName("changeHome")]
        public int ChangeHome { get; set; }

        [JsonPropertyName("changeAway")]
        public int ChangeAway { get; set; }

        [JsonPropertyName("drawTotal")]
        public int DrawTotal { get; set; }

        [JsonPropertyName("drawHome")]
        public int DrawHome { get; set; }

        [JsonPropertyName("drawAway")]
        public int DrawAway { get; set; }

        [JsonPropertyName("goalDiffTotal")]
        public int GoalDiffTotal { get; set; }

        [JsonPropertyName("goalDiffHome")]
        public int GoalDiffHome { get; set; }

        [JsonPropertyName("goalDiffAway")]
        public int GoalDiffAway { get; set; }

        [JsonPropertyName("goalsAgainstTotal")]
        public int GoalsAgainstTotal { get; set; }

        [JsonPropertyName("goalsAgainstHome")]
        public int GoalsAgainstHome { get; set; }

        [JsonPropertyName("goalsAgainstAway")]
        public int GoalsAgainstAway { get; set; }

        [JsonPropertyName("goalsForTotal")]
        public int GoalsForTotal { get; set; }

        [JsonPropertyName("goalsForHome")]
        public int GoalsForHome { get; set; }

        [JsonPropertyName("goalsForAway")]
        public int GoalsForAway { get; set; }

        [JsonPropertyName("lossTotal")]
        public int LossTotal { get; set; }

        [JsonPropertyName("lossHome")]
        public int LossHome { get; set; }

        [JsonPropertyName("lossAway")]
        public int LossAway { get; set; }

        [JsonPropertyName("total")]
        public int TotalMatches { get; set; }

        [JsonPropertyName("home")]
        public int HomeMatches { get; set; }

        [JsonPropertyName("away")]
        public int AwayMatches { get; set; }

        [JsonPropertyName("pointsTotal")]
        public int PointsTotal { get; set; }

        [JsonPropertyName("pointsHome")]
        public int PointsHome { get; set; }

        [JsonPropertyName("pointsAway")]
        public int PointsAway { get; set; }

        [JsonPropertyName("pos")]
        public int PositionOverall { get; set; }

        [JsonPropertyName("posHome")]
        public int PositionHome { get; set; }

        [JsonPropertyName("posAway")]
        public int PositionAway { get; set; }

        [JsonPropertyName("sortPositionTotal")]
        public int SortPositionTotal { get; set; }

        [JsonPropertyName("sortPositionHome")]
        public int SortPositionHome { get; set; }

        [JsonPropertyName("sortPositionAway")]
        public int SortPositionAway { get; set; }

        [JsonPropertyName("team")]
        public Team Team { get; set; }

        [JsonPropertyName("winTotal")]
        public int WinTotal { get; set; }

        [JsonPropertyName("winHome")]
        public int WinHome { get; set; }

        [JsonPropertyName("winAway")]
        public int WinAway { get; set; }
    }

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

    // Optional: classes for "set", "header", "matchtype", and "tabletype"
    public class TableSet
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("headers")]
        public List<int> Headers { get; set; }
    }

    public class TableHeader
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("column")]
        public string Column { get; set; }

        [JsonPropertyName("tooltip")]
        public string Tooltip { get; set; }

        [JsonPropertyName("datapriority")]
        public string DataPriority { get; set; }
    }

    public class MatchType
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }

        [JsonPropertyName("_id")]
        public int Id { get; set; }

        [JsonPropertyName("settypeid")]
        public int SetTypeId { get; set; }

        [JsonPropertyName("column")]
        public string Column { get; set; }
    }

    public class TableType
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }

        [JsonPropertyName("_id")]
        public int Id { get; set; }

        [JsonPropertyName("column")]
        public string Column { get; set; }
    }
}
