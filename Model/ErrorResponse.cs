using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace fredapi.Model.Live.ErrorResponse
{
    public class ErrorResponse
    {
        [JsonPropertyName("queryUrl")]
        public string QueryUrl { get; set; }

        [JsonPropertyName("doc")]
        public List<ErrorDoc> Doc { get; set; }
    }

    public class ErrorDoc
    {
        [JsonPropertyName("event")]
        public string Event { get; set; }

        [JsonPropertyName("_dob")]
        public long Dob { get; set; }

        [JsonPropertyName("_maxage")]
        public int MaxAge { get; set; }

        [JsonPropertyName("data")]
        public ErrorData Data { get; set; }
    }

    public class ErrorData
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("query")]
        public string Query { get; set; }
    }
}