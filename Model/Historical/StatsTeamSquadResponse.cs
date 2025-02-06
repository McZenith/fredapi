using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace fredapi.Model.Historical.StatsTeamSquadResponse
{
    public class StatsTeamSquadResponse
    {
        [JsonPropertyName("queryUrl")]
        public string QueryUrl { get; set; }
        
        [JsonPropertyName("doc")]
        public List<Doc> Doc { get; set; }
    }

    public class Doc
    {
        // "event" is a reserved word in C#, so we rename it to EventName.
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
        [JsonPropertyName("team")]
        public Team Team { get; set; }
        
        [JsonPropertyName("players")]
        public List<ExtendedPlayer> Players { get; set; }
        
        [JsonPropertyName("squadinfo")]
        public SquadInfo SquadInfo { get; set; }
        
        // Roles is a dictionary keyed by the player id (as string) whose value is a list of player roles.
        [JsonPropertyName("roles")]
        public Dictionary<string, List<PlayerRole>> Roles { get; set; }
        
        // Seasons keyed by season id string.
        [JsonPropertyName("seasons")]
        public Dictionary<string, Season> Seasons { get; set; }
        
        [JsonPropertyName("managers")]
        public List<Manager> Managers { get; set; }
    }

    public class Team
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

    public class ExtendedPlayer
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }
        
        [JsonPropertyName("_id")]
        public int Id { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        // "fullname" may be absent for some players.
        [JsonPropertyName("fullname")]
        public string FullName { get; set; }
        
        [JsonPropertyName("birthdate")]
        public TimeData Birthdate { get; set; }
        
        [JsonPropertyName("nationality")]
        public Country Nationality { get; set; }
        
        [JsonPropertyName("position")]
        public Position Position { get; set; }
        
        [JsonPropertyName("primarypositiontype")]
        public object PrimaryPositionType { get; set; }
        
        [JsonPropertyName("haslogo")]
        public bool HasLogo { get; set; }
        
        [JsonPropertyName("shirtnumber")]
        public string ShirtNumber { get; set; }
        
        [JsonPropertyName("membersince")]
        public TimeData MemberSince { get; set; }
        
        [JsonPropertyName("height")]
        public int? Height { get; set; }
        
        [JsonPropertyName("weight")]
        public int? Weight { get; set; }
        
        [JsonPropertyName("birthcountry")]
        public Country BirthCountry { get; set; }
        
        // Sometimes the JSON includes both "_foot" and "foot"
        [JsonPropertyName("_foot")]
        public string FootRaw { get; set; }
        
        [JsonPropertyName("foot")]
        public string Foot { get; set; }
        
        [JsonPropertyName("birthplace")]
        public string Birthplace { get; set; }
        
        [JsonPropertyName("twitter")]
        public string Twitter { get; set; }
        
        [JsonPropertyName("facebook")]
        public string Facebook { get; set; }
        
        [JsonPropertyName("nickname")]
        public string Nickname { get; set; }
        
        [JsonPropertyName("marketvalue")]
        public int? MarketValue { get; set; }
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
        public int TzOffset { get; set; }
        
        [JsonPropertyName("uts")]
        public long Uts { get; set; }
    }

    public class Country
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

    public class Position
    {
        [JsonPropertyName("_id")]
        public int Id { get; set; }
        
        [JsonPropertyName("_type")]
        public string Type { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("shortname")]
        public string ShortName { get; set; }
        
        [JsonPropertyName("abbr")]
        public string Abbr { get; set; }
    }

    public class SquadInfo
    {
        [JsonPropertyName("numberofplayers")]
        public int NumberOfPlayers { get; set; }
        
        [JsonPropertyName("averagesquadage")]
        public double AverageSquadAge { get; set; }
        
        // Averagestartingxiage is a dictionary keyed by season id.
        [JsonPropertyName("averagestartingxiage")]
        public Dictionary<string, string> AverageStartingXiAge { get; set; }
    }

    public class PlayerRole
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }
        
        [JsonPropertyName("_playerid")]
        public int PlayerId { get; set; }
        
        [JsonPropertyName("_id")]
        public int Id { get; set; }
        
        // _type can indicate role type (e.g., Player or On loan)
        [JsonPropertyName("_type")]
        public int Type { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("start")]
        public TimeData Start { get; set; }
        
        [JsonPropertyName("end")]
        public TimeData End { get; set; }
        
        [JsonPropertyName("active")]
        public bool Active { get; set; }
        
        [JsonPropertyName("team")]
        public Team Team { get; set; }
        
        [JsonPropertyName("shirt")]
        public string Shirt { get; set; }
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

    public class Manager
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }
        
        [JsonPropertyName("_id")]
        public int Id { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("fullname")]
        public string FullName { get; set; }
        
        [JsonPropertyName("birthdate")]
        public TimeData Birthdate { get; set; }
        
        [JsonPropertyName("nationality")]
        public Country Nationality { get; set; }
        
        [JsonPropertyName("primarypositiontype")]
        public object PrimaryPositionType { get; set; }
        
        [JsonPropertyName("haslogo")]
        public bool HasLogo { get; set; }
        
        [JsonPropertyName("membersince")]
        public TimeData MemberSince { get; set; }
    }
}
