using System.Text.Json.Serialization;

namespace fredapi.Model.Live.OddsResponse
{
    public class OddsResponse
    {
        [JsonPropertyName("queryUrl")]
        public string QueryUrl { get; set; }

        [JsonPropertyName("doc")]
        public List<OddsDoc> Doc { get; set; }
    }

    public class OddsDoc
    {
        [JsonPropertyName("event")]
        public string Event { get; set; }

        [JsonPropertyName("_dob")]
        public long Dob { get; set; }

        [JsonPropertyName("_maxage")]
        public int MaxAge { get; set; }

        /// <summary>
        /// The key is the match id (e.g. "51103457") and its value is a dictionary
        /// of bets keyed by their oddstype id (e.g. "9", "10", etc.)
        /// </summary>
        [JsonPropertyName("data")]
        public Dictionary<string, Dictionary<string, Bet>> Data { get; set; }
    }

    public class Bet
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }

        [JsonPropertyName("matchid")]
        public int MatchId { get; set; }

        [JsonPropertyName("clientmatchid")]
        public string ClientMatchId { get; set; }

        [JsonPropertyName("bookmakerid")]
        public int BookmakerId { get; set; }

        // Sometimes null; if it may hold an integer value, use int?
        [JsonPropertyName("bookmakerbetid")]
        public int? BookmakerBetId { get; set; }

        [JsonPropertyName("oddstype")]
        public string OddsType { get; set; }

        [JsonPropertyName("oddstypeshort")]
        public string OddsTypeShort { get; set; }

        [JsonPropertyName("oddstypeid")]
        public string OddsTypeId { get; set; }

        /// <summary>
        /// The handicap object is optional.
        /// </summary>
        [JsonPropertyName("hcp")]
        public Handicap Hcp { get; set; }

        /// <summary>
        /// A dictionary of outcomes where the key is a string (e.g. "1", "2", etc.)
        /// </summary>
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

        // This property is null in your JSON sample.
        [JsonPropertyName("clientbetnumber")]
        public int? ClientBetNumber { get; set; }

        [JsonPropertyName("updated_uts")]
        public long UpdatedUts { get; set; }
    }

    public class Outcome
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        // Although these are numbers in the JSON they appear as strings.
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("outcomeid")]
        public string OutcomeId { get; set; }

        [JsonPropertyName("odds")]
        public string Odds { get; set; }

        [JsonPropertyName("tbid")]
        public string Tbid { get; set; }

        [JsonPropertyName("_sk")]
        public string Sk { get; set; }
    }

    public class Handicap
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        // Although the value looks numeric, it's stored as a string.
        [JsonPropertyName("value")]
        public string Value { get; set; }
    }
}
