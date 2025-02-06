using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace fredapi.Model.Historical.StatsTeamUsualLineupResponse
{
    public class StatsTeamUsualLineupResponse
    {
        [JsonPropertyName("queryUrl")]
        public string QueryUrl { get; set; }
        
        [JsonPropertyName("doc")]
        public List<Doc> Doc { get; set; }
    }

    public class Doc
    {
        // "event" is a reserved keyword in C#. We rename it to EventName.
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
        [JsonPropertyName("formation")]
        public string Formation { get; set; }
        
        [JsonPropertyName("players")]
        public List<ExtendedPlayer> Players { get; set; }
    }

    public class ExtendedPlayer
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }
        
        [JsonPropertyName("_id")]
        public int Id { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        // Some responses may include a fullname.
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
        
        [JsonPropertyName("height")]
        public int Height { get; set; }
        
        [JsonPropertyName("weight")]
        public int Weight { get; set; }
        
        // Some responses include both _foot and foot.
        [JsonPropertyName("_foot")]
        public string FootRaw { get; set; }
        
        [JsonPropertyName("foot")]
        public string Foot { get; set; }
        
        [JsonPropertyName("nickname")]
        public string Nickname { get; set; }
        
        [JsonPropertyName("minutesplayed")]
        public int MinutesPlayed { get; set; }
        
        [JsonPropertyName("shirtnumber")]
        public string ShirtNumber { get; set; }
        
        // Market value is optional and may be absent.
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
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        // Additional fields for the lineup
        [JsonPropertyName("order")]
        public int Order { get; set; }
        
        [JsonPropertyName("matchpos")]
        public string MatchPos { get; set; }
        
        [JsonPropertyName("basename")]
        public string BaseName { get; set; }
        
        [JsonPropertyName("baseshortname")]
        public string BaseShortName { get; set; }
        
        [JsonPropertyName("baseabbr")]
        public string BaseAbbr { get; set; }
    }
}
