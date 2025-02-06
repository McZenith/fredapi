using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace fredapi.Model.Historical.StatsMatchForm
{
    public class StatsMatchFormResponse
    {
        [JsonPropertyName("queryUrl")]
        public string QueryUrl { get; set; }
        
        [JsonPropertyName("doc")]
        public List<StatsMatchFormDoc> Doc { get; set; }
    }

    public class StatsMatchFormDoc
    {
        [JsonPropertyName("event")]
        public string Event { get; set; }
        
        [JsonPropertyName("_dob")]
        public long Dob { get; set; }
        
        [JsonPropertyName("_maxage")]
        public int MaxAge { get; set; }
        
        [JsonPropertyName("data")]
        public StatsMatchFormData Data { get; set; }
    }

    public class StatsMatchFormData
    {
        [JsonPropertyName("_id")]
        public int Id { get; set; }
        
        [JsonPropertyName("teams")]
        public StatsMatchFormTeams Teams { get; set; }
    }

    public class StatsMatchFormTeams
    {
        [JsonPropertyName("home")]
        public StatsMatchFormTeamInfo Home { get; set; }
        
        [JsonPropertyName("away")]
        public StatsMatchFormTeamInfo Away { get; set; }
    }

    public class StatsMatchFormTeamInfo
    {
        [JsonPropertyName("team")]
        public Team Team { get; set; }
        
        [JsonPropertyName("form")]
        public List<MatchFormEntry> Form { get; set; }
        
        [JsonPropertyName("streak")]
        public Streak Streak { get; set; }
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

    public class MatchFormEntry
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }
    }

    public class Streak
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }
        
        [JsonPropertyName("value")]
        public int Value { get; set; }
    }
}
