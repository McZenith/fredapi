using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace fredapi.Model.Historical.MatchFunfacts
{
    public class MatchFunfactsResponse
    {
        [JsonPropertyName("queryUrl")]
        public string QueryUrl { get; set; }
        
        [JsonPropertyName("doc")]
        public List<MatchFunfactsDoc> Doc { get; set; }
    }

    public class MatchFunfactsDoc
    {
        [JsonPropertyName("event")]
        public string Event { get; set; }
        
        [JsonPropertyName("_dob")]
        public long Dob { get; set; }
        
        [JsonPropertyName("_maxage")]
        public int MaxAge { get; set; }
        
        [JsonPropertyName("data")]
        public FunfactsData Data { get; set; }
    }

    public class FunfactsData
    {
        [JsonPropertyName("_id")]
        public int Id { get; set; }
        
        [JsonPropertyName("funfacts")]
        public List<string> Funfacts { get; set; }
        
        [JsonPropertyName("facts")]
        public List<Fact> Facts { get; set; }
    }

    public class Fact
    {
        [JsonPropertyName("_id")]
        public long Id { get; set; }
        
        [JsonPropertyName("_typeid")]
        public int TypeId { get; set; }
        
        [JsonPropertyName("sentence")]
        public string Sentence { get; set; }
    }
}