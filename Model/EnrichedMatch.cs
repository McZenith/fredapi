using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace fredapi.Model;

public class EnrichedMatch
{
    [BsonId]
    public ObjectId Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? MatchId { get; set; } = string.Empty;
    public string? SeasonId { get; set; } = string.Empty;
    public string? Team1Id { get; set; } = string.Empty;
    public string? Team2Id { get; set; } = string.Empty;
    public string? CoreMatchData { get; set; }
    public string? MatchInfo { get; set; }
    public string? MatchTimelineDelta { get; set; }
    public string? MatchDetailsExtended { get; set; }
    public string? MatchOdds { get; set; }
    public string? MatchTimeline { get; set; }
    public string? MatchSquads { get; set; }
    public string? MatchSituation { get; set; }
    public string? MatchForm { get; set; }
    public string? SeasonMeta { get; set; }
    public string? SeasonLiveTable { get; set; }
    public string? BookmakerOdds { get; set; }
    public string? SeasonTopGoals { get; set; }
    public string? TeamVersusRecent { get; set; }
    public string? Team1LastX { get; set; }
    public string? Team2LastX { get; set; }
    public string? MatchPhrases { get; set; }
    public string? MatchFunFacts { get; set; }
    public string? MatchPhrasesDelta { get; set; }
    public string? MatchInsights { get; set; }
    public string? MetaData { get; set; }
    public string? TimelineDelta { get; set; }
    public string? FormTable { get; set; }
    public string? LiveTable { get; set; }
    public string? TopGoals { get; set; }
    public string? VersusRecentStats { get; set; }
    public string? LastXStatsTeam1 { get; set; }
    public string? LastXStatsTeam2 { get; set; }
    public string? Phrases { get; set; }
    public string? FunFacts { get; set; }
    public string? PhraseDelta { get; set; }
    public string? CupBrackets { get; set; }
    public string? DynamicTable { get; set; }
}