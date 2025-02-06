using System.Text.Json.Serialization;

namespace fredapi.Model.Historical.MatchBookmakerOddsResponse
{
    // The root object that holds the response.
    public class MatchBookmakerOddsResponse
    {
        [JsonPropertyName("queryUrl")]
        public string QueryUrl { get; set; }

        [JsonPropertyName("doc")]
        public List<BookmakerOddsDoc> Documents { get; set; }
    }

    public class BookmakerOddsDoc
    {
        [JsonPropertyName("event")]
        public string Event { get; set; }

        [JsonPropertyName("_dob")]
        public long Dob { get; set; }

        [JsonPropertyName("_maxage")]
        public int MaxAge { get; set; }

        [JsonPropertyName("data")]
        public BookmakerOddsData Data { get; set; }
    }

    public class BookmakerOddsData
    {
        // The JSON key "51103457" is the match ID. Because JSON property names may be numeric
        // you can use a dictionary to map from match id (as string) to its odds data.
        [JsonPropertyName("51103457")]
        public Dictionary<string, BookmakerOdds> OddsByOddstype { get; set; }
    }

    public class BookmakerOdds
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }

        [JsonPropertyName("matchid")]
        public int MatchId { get; set; }

        [JsonPropertyName("clientmatchid")]
        public string ClientMatchId { get; set; }

        [JsonPropertyName("bookmakerid")]
        public int BookmakerId { get; set; }

        [JsonPropertyName("bookmakerbetid")]
        public string BookmakerBetId { get; set; }

        [JsonPropertyName("oddstype")]
        public string OddsType { get; set; }

        [JsonPropertyName("oddstypeshort")]
        public string OddsTypeShort { get; set; }

        [JsonPropertyName("oddstypeid")]
        public string OddsTypeId { get; set; }

        // For bet types that include an handicap (hcp), this property may be present.
        [JsonPropertyName("hcp")]
        public Handicap Hcp { get; set; }

        // The outcomes are keyed by outcome IDs.
        [JsonPropertyName("outcomes")]
        public Dictionary<string, Outcome> Outcomes { get; set; }

        [JsonPropertyName("livebet")]
        public bool LiveBet { get; set; }

        [JsonPropertyName("ismatchodds")]
        public bool IsMatchOdds { get; set; }

        [JsonPropertyName("extra")]
        public string Extra { get; set; }

        [JsonPropertyName("active")]
        public bool Active { get; set; }

        [JsonPropertyName("betstop")]
        public bool BetStop { get; set; }

        [JsonPropertyName("clientbetnumber")]
        public object ClientBetNumber { get; set; } // Could be null or a string/number

        [JsonPropertyName("updated_uts")]
        public long UpdatedUts { get; set; }
    }

    public class Handicap
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        // The handicap value – note that for different bet types this might be formatted differently.
        [JsonPropertyName("value")]
        public string Value { get; set; }
    }

    public class Outcome
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        // Some outcomes include an "id" as a string.
        [JsonPropertyName("id")]
        public string Id { get; set; }

        // The odds as a string (it may be a numeric value represented as text)
        [JsonPropertyName("odds")]
        public string Odds { get; set; }

        // The “tbid” property – a bookmaker–internal id.
        [JsonPropertyName("tbid")]
        public string Tbid { get; set; }
    }
}
