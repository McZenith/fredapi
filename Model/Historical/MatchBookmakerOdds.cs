using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace fredapi.Model.Historical.MatchBookmakerOdds
{
    public class MatchBookmakerOddsResponse
    {
        [JsonPropertyName("queryUrl")]
        public string QueryUrl { get; set; }
        
        [JsonPropertyName("doc")]
        public List<MatchBookmakerOddsDoc> Doc { get; set; }
    }

    public class MatchBookmakerOddsDoc
    {
        [JsonPropertyName("event")]
        public string Event { get; set; }
        
        [JsonPropertyName("_dob")]
        public long Dob { get; set; }
        
        [JsonPropertyName("_maxage")]
        public int MaxAge { get; set; }
        
        /// <summary>
        /// The data property is a dictionary whose key is the match id (as a string) and whose value is 
        /// another dictionary mapping the bet type (as a string) to a Bet.
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
        
        [JsonPropertyName("bookmakerbetid")]
        public string BookmakerBetId { get; set; }
        
        [JsonPropertyName("oddstype")]
        public string OddsType { get; set; }
        
        [JsonPropertyName("oddstypeshort")]
        public string OddsTypeShort { get; set; }
        
        [JsonPropertyName("oddstypeid")]
        public string OddsTypeId { get; set; }
        
        // Some bets include a handicap object ("hcp")
        [JsonPropertyName("hcp")]
        public Hcp Hcp { get; set; }
        
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
        
        // The type of clientbetnumber is not specified (it is null in the sample) so we use object.
        [JsonPropertyName("clientbetnumber")]
        public object ClientBetNumber { get; set; }
        
        [JsonPropertyName("updated_uts")]
        public long UpdatedUts { get; set; }
    }

    public class Outcome
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("id")]
        public string Id { get; set; }
        
        [JsonPropertyName("odds")]
        public string Odds { get; set; }
        
        [JsonPropertyName("tbid")]
        public string Tbid { get; set; }
    }

    public class Hcp
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("id")]
        public string Id { get; set; }
        
        [JsonPropertyName("value")]
        public string Value { get; set; }
    }
}
