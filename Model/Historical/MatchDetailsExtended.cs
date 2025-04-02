using System.Text.Json.Serialization;

namespace fredapi.Model.Historical.MatchDetailsExtended
{
    public class MatchDetailsExtendedResponse
    {
        [JsonPropertyName("queryUrl")]
        public string QueryUrl { get; set; }
        
        [JsonPropertyName("doc")]
        public List<MatchDetailsExtendedDoc> Doc { get; set; }
    }

    public class MatchDetailsExtendedDoc
    {
        [JsonPropertyName("event")]
        public string Event { get; set; }
        
        [JsonPropertyName("_dob")]
        public long Dob { get; set; }
        
        [JsonPropertyName("_maxage")]
        public int MaxAge { get; set; }
        
        [JsonPropertyName("data")]
        public MatchDetails Data { get; set; }
    }

    public class MatchDetails
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }
        
        [JsonPropertyName("_matchid")]
        public int MatchId { get; set; }
        
        [JsonPropertyName("teams")]
        public Teams Teams { get; set; }
        
        // "index" is an array – if you expect elements, change object to the correct type.
        [JsonPropertyName("index")]
        public List<object> Index { get; set; }
        
        // "types" is a key/value dictionary where both key and value are strings.
        [JsonPropertyName("types")]
        public Dictionary<string, string> Types { get; set; }
    }

    public class Teams
    {
        [JsonPropertyName("home")]
        public string Home { get; set; }
        
        [JsonPropertyName("away")]
        public string Away { get; set; }
    }
}