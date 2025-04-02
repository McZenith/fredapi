using System.Text.Json.Serialization;

namespace fredapi.Model.Historical.MatchPhrasesDeltaResponse
{
    public class MatchPhrasesDeltaResponse
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
        public MatchPhrasesDeltaData Data { get; set; }
    }

    public class MatchPhrasesDeltaData
    {
        [JsonPropertyName("owncommentary")]
        public bool OwnCommentary { get; set; }

        [JsonPropertyName("phrases")]
        public List<string> Phrases { get; set; }
    }
    
}