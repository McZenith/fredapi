using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace fredapi.Model.Live.StatsTeamLastXResponse
{
    public class StatsTeamLastXResponse
    {
        [JsonPropertyName("queryUrl")]
        public string QueryUrl { get; set; }

        [JsonPropertyName("doc")]
        public List<StatsTeamLastXDoc> Doc { get; set; }
    }

    public class StatsTeamLastXDoc
    {
        [JsonPropertyName("event")]
        public string Event { get; set; }

        [JsonPropertyName("_dob")]
        public long Dob { get; set; }

        [JsonPropertyName("_maxage")]
        public int MaxAge { get; set; }

        [JsonPropertyName("data")]
        public StatsTeamLastXData Data { get; set; }
    }

    public class StatsTeamLastXData
    {
        [JsonPropertyName("team")]
        public UniqueTeam Team { get; set; }

        [JsonPropertyName("matches")]
        public List<Match> Matches { get; set; }

        [JsonPropertyName("tournaments")]
        public Dictionary<string, Tournament> Tournaments { get; set; }

        [JsonPropertyName("uniquetournaments")]
        public Dictionary<string, UniqueTournament> Uniquetournaments { get; set; }

        [JsonPropertyName("realcategories")]
        public Dictionary<string, RealCategory> Realcategories { get; set; }
    }

    // Represents a unique team (used for "team" in data)
    public class UniqueTeam
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }

        [JsonPropertyName("_id")]
        public int Id { get; set; }

        [JsonPropertyName("_rcid")]
        public int Rcid { get; set; }

        [JsonPropertyName("_sid")]
        public int Sid { get; set; }

        public string Name { get; set; }

        public string Mediumname { get; set; }

        public string Suffix { get; set; }

        public string Abbr { get; set; }

        public string Nickname { get; set; }

        public int Teamtypeid { get; set; }

        public bool Iscountry { get; set; }

        public string Sex { get; set; }

        public bool Haslogo { get; set; }

        public string Founded { get; set; }

        public string Website { get; set; }
    }

    // Represents a match in the "matches" array.
    public class Match
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }

        [JsonPropertyName("_id")]
        public int Id { get; set; }

        [JsonPropertyName("_sid")]
        public int Sid { get; set; }

        [JsonPropertyName("_rcid")]
        public int Rcid { get; set; }

        [JsonPropertyName("_tid")]
        public int Tid { get; set; }

        [JsonPropertyName("_utid")]
        public int Utid { get; set; }

        [JsonPropertyName("time")]
        public TimeObject Time { get; set; }

        public int Round { get; set; }

        [JsonPropertyName("roundname")]
        public TableRound Roundname { get; set; }

        public int Week { get; set; }

        public Result Result { get; set; }

        public Periods Periods { get; set; }

        [JsonPropertyName("_seasonid")]
        public int SeasonId { get; set; }

        public Teams Teams { get; set; }

        public bool Neutralground { get; set; }

        public string Comment { get; set; }

        public object Status { get; set; }

        public bool Tobeannounced { get; set; }

        public bool Postponed { get; set; }

        [JsonPropertyName("canceled")]
        public bool Canceled { get; set; }

        public bool Inlivescore { get; set; }

        public int Stadiumid { get; set; }

        public object Bestof { get; set; }

        public bool Walkover { get; set; }

        public bool Retired { get; set; }

        public bool Disqualified { get; set; }

        public List<MatchForm> Form { get; set; }
    }

    public class TimeObject
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }

        [JsonPropertyName("time")]
        public string TimeValue { get; set; }

        [JsonPropertyName("date")]
        public string Date { get; set; }

        [JsonPropertyName("tz")]
        public string Tz { get; set; }

        [JsonPropertyName("tzoffset")]
        public int Tzoffset { get; set; }

        [JsonPropertyName("uts")]
        public long Uts { get; set; }
    }

    public class TableRound
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }

        [JsonPropertyName("_id")]
        public int Id { get; set; }

        public int Name { get; set; }
    }

    public class Result
    {
        public int Home { get; set; }
        public int Away { get; set; }
        public string Period { get; set; }
        public string Winner { get; set; }
    }

    public class Periods
    {
        public PeriodScore P1 { get; set; }

        [JsonPropertyName("ft")]
        public PeriodScore Ft { get; set; }
    }

    public class PeriodScore
    {
        public int Home { get; set; }
        public int Away { get; set; }
    }

    public class Teams
    {
        public Team Home { get; set; }
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

        public int Uid { get; set; }

        public bool Virtual { get; set; }

        public string Name { get; set; }

        public string Mediumname { get; set; }

        public string Abbr { get; set; }

        public string Nickname { get; set; }

        public bool Iscountry { get; set; }

        public bool Haslogo { get; set; }
    }

    public class MatchForm
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }

        public string Uniqueteamid { get; set; }

        public int Matchid { get; set; }

        public FormData Form { get; set; }
    }

    public class FormData
    {
        [JsonPropertyName("home")]
        public Dictionary<string, string> Home { get; set; }

        [JsonPropertyName("away")]
        public Dictionary<string, string> Away { get; set; }

        [JsonPropertyName("total")]
        public Dictionary<string, string> Total { get; set; }
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
        public int Rcid { get; set; }

        [JsonPropertyName("_isk")]
        public int Isk { get; set; }

        [JsonPropertyName("_tid")]
        public int Tid { get; set; }

        [JsonPropertyName("_utid")]
        public int Utid { get; set; }

        [JsonPropertyName("_gender")]
        public string Gender { get; set; }

        public string Name { get; set; }

        public string Abbr { get; set; }

        public object Ground { get; set; }

        public bool Friendly { get; set; }

        public int Seasonid { get; set; }

        public int Currentseason { get; set; }

        public string Year { get; set; }

        public string Seasontype { get; set; }

        public string Seasontypename { get; set; }

        public string Seasontypeunique { get; set; }

        public int Livetable { get; set; }

        public object Cuprosterid { get; set; }

        public bool Roundbyround { get; set; }

        public int Tournamentlevelorder { get; set; }

        public string Tournamentlevelname { get; set; }

        public bool Outdated { get; set; }
    }

    public class UniqueTournament
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }

        [JsonPropertyName("_id")]
        public int Id { get; set; }

        [JsonPropertyName("_utid")]
        public int Utid { get; set; }

        [JsonPropertyName("_sid")]
        public int Sid { get; set; }

        [JsonPropertyName("_rcid")]
        public int Rcid { get; set; }

        public string Name { get; set; }

        public int Currentseason { get; set; }

        public bool Friendly { get; set; }
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
        public int Rcid { get; set; }

        public string Name { get; set; }

        public CountryCode Cc { get; set; }
    }

    public class CountryCode
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }

        [JsonPropertyName("_id")]
        public int Id { get; set; }

        public string A2 { get; set; }

        public string Name { get; set; }

        public string A3 { get; set; }

        public string Ioc { get; set; }

        public int Continentid { get; set; }

        public string Continent { get; set; }

        public int Population { get; set; }
    }
}
