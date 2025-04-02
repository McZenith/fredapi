using System.Text.Json.Serialization;

namespace fredapi.Model.Live
{
    public class StatsMatchSituationResponse
    {
        [JsonPropertyName("queryUrl")]
        public string QueryUrl { get; set; }
        
        [JsonPropertyName("doc")]
        public List<Doc> Doc { get; set; }
    }

    public class Doc
    {
        [JsonPropertyName("event")]
        public string Event { get; set; }
        
        [JsonPropertyName("_dob")]
        public long Dob { get; set; }
        
        [JsonPropertyName("_maxage")]
        public int MaxAge { get; set; }
        
        [JsonPropertyName("data")]
        public MatchSituationDataWrapper Data { get; set; }
    }

    public class MatchSituationDataWrapper
    {
        // This object represents the data object in the JSON.
        // It includes the _doc field (which indicates the type of stats)
        // a matchid (as string) and a list of situation data entries.
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }
        
        [JsonPropertyName("matchid")]
        public string MatchId { get; set; }
        
        [JsonPropertyName("data")]
        public List<MatchSituationEntry> Data { get; set; }
    }

    public class MatchSituationEntry
    {
        [JsonPropertyName("time")]
        public int Time { get; set; }
        
        [JsonPropertyName("injurytime")]
        public int InjuryTime { get; set; }
        
        [JsonPropertyName("safe")]
        public int Safe { get; set; }
        
        [JsonPropertyName("safecount")]
        public int SafeCount { get; set; }
        
        [JsonPropertyName("home")]
        public SituationStats Home { get; set; }
        
        [JsonPropertyName("away")]
        public SituationStats Away { get; set; }
    }

    public class SituationStats
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
}
