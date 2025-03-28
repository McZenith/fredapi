using System.Text.Json.Serialization;
using System.Collections.Generic;
using fredapi.SportRadarService.Background;
using MongoDB.Bson.Serialization.Attributes;

namespace fredapi.Model
{
    public class TeamVersusRecentModel
    {
        [JsonPropertyName("livematchid")]
        public string LiveMatchId { get; set; }

        [JsonPropertyName("matches")]
        public List<HeadToHeadMatch> Matches { get; set; } = new();

        [JsonPropertyName("tournaments")]
        public Dictionary<string, TournamentInfo> Tournaments { get; set; } = new();

        [JsonPropertyName("realcategories")]
        public Dictionary<string, RealCategoryInfo> RealCategories { get; set; } = new();

        [JsonPropertyName("teams")]
        public Dictionary<string, TeamInfo> Teams { get; set; } = new();

        [JsonPropertyName("currentmanagers")]
        public Dictionary<string, ManagerInfo> CurrentManagers { get; set; } = new();

        [JsonPropertyName("jersey")]
        public Dictionary<string, object> Jersey { get; set; } = new();

        [JsonPropertyName("next")]
        public NextMatchInfo Next { get; set; }

        // These properties will be populated from the doc wrapper structure
        [JsonPropertyName("queryUrl")]
        public string QueryUrl { get; set; }

        [JsonPropertyName("event")]
        public string Event { get; set; }

        [JsonPropertyName("_dob")]
        public long Dob { get; set; }

        [JsonPropertyName("_maxage")]
        public int Maxage { get; set; }
    }
}