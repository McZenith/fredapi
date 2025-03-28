using System.Text.Json.Serialization;
using System.Collections.Generic;
using fredapi.SportRadarService.Background;

namespace fredapi.Model
{
    public class TeamLastXStatsModel
    {
        [JsonPropertyName("team")]
        public TeamInfo Team { get; set; }

        [JsonPropertyName("matches")]
        public List<MatchStat> Matches { get; set; } = new();

        [JsonPropertyName("tournaments")]
        public Dictionary<string, TournamentInfo> Tournaments { get; set; } = new();

        [JsonPropertyName("uniquetournaments")]
        public Dictionary<string, UniqueTournamentInfo> UniqueTournaments { get; set; } = new();

        [JsonPropertyName("realcategories")]
        public Dictionary<string, RealCategoryInfo> RealCategories { get; set; } = new();

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