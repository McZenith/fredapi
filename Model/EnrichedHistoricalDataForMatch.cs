using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using fredapi.Model.Historical.MatchBookmakerOdds;
using fredapi.Model.Historical.MatchFunfacts;
using fredapi.Model.Historical.MatchInfo;
using fredapi.Model.Historical.MatchInsightsResponse;
using fredapi.Model.Historical.MatchPhrases;
using fredapi.Model.Historical.MatchPhrasesDeltaResponse;
using fredapi.Model.Historical.MatchSquads;
using fredapi.Model.Historical.MatchTimeline;
using fredapi.Model.Historical.MatchTimelinedelta;
using fredapi.Model.Historical.SeasonDynamicTableResponse;
using fredapi.Model.Historical.SeasonLivetable;
using fredapi.Model.Historical.StatsCupBracketsResponse;
using fredapi.Model.Historical.StatsFormtable;
using fredapi.Model.Historical.StatsMatchForm;
using fredapi.Model.Historical.StatsMatchSituationResponse;
using fredapi.Model.Historical.StatsSeasonMeta;
using fredapi.Model.Historical.StatsSeasonTopgoalsResponse;
using fredapi.Model.Historical.StatsTeamLastxExtendedResponse;
using fredapi.Model.Historical.StatsTeamLastxResponse;
using fredapi.Model.Historical.StatsTeamVersusRecentResponse;

namespace fredapi.Model.EnrichedHistoricalDataForMatch;

public class EnrichedHistoricalDataForMatch
{
    [BsonId]
    public ObjectId Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? MatchId { get; set; } = string.Empty;
    public string? SeasonId { get; set; } = string.Empty;
    public string? Team1Id { get; set; } = string.Empty;
    public string? Team2Id { get; set; } = string.Empty;
    
    [BsonIgnoreIfNull]
    public string? CoreMatchData { get; set; }
    
    [BsonIgnoreIfNull]

    public MatchInfoResponse? MatchInfo { get; set; }
    
    [BsonIgnoreIfNull]
    public MatchTimelinedeltaData? MatchTimelineDelta { get; set; }
    
    [BsonIgnoreIfNull]
    public string? MatchDetailsExtended { get; set; }
    
    [BsonIgnoreIfNull]
    public MatchTimelineResponse? MatchTimeline { get; set; }
    
    [BsonIgnoreIfNull]
    public MatchSquadsResponse? MatchSquads { get; set; }
    
    [BsonIgnoreIfNull]
    public StatsMatchSituationResponse? MatchSituation { get; set; }
    
    [BsonIgnoreIfNull]
    public StatsMatchFormResponse? MatchForm { get; set; }
    [BsonIgnoreIfNull]
    public StatsSeasonMetaResponse? SeasonMeta { get; set; }
    
    [BsonIgnoreIfNull]
    public SeasonLivetableResponse? SeasonLiveTable { get; set; }
    
    [BsonIgnoreIfNull]
    public MatchBookmakerOddsResponse? BookmakerOdds { get; set; }
    
    [BsonIgnoreIfNull]
    public StatsSeasonTopgoalsResponse? SeasonTopGoals { get; set; }
    
    [BsonIgnoreIfNull]
    public StatsTeamVersusRecentResponse? TeamVersusRecent { get; set; }
    
    [BsonIgnoreIfNull]
    public StatsTeamLastxResponse? Team1LastX { get; set; }
    
    [BsonIgnoreIfNull]
    public StatsTeamLastxResponse? Team2LastX { get; set; }
    
    [BsonIgnoreIfNull]
    public MatchPhrasesResponse? MatchPhrases { get; set; }
    
    [BsonIgnoreIfNull]
    public MatchFunfactsResponse? MatchFunFacts { get; set; }
    
    [BsonIgnoreIfNull]
    public MatchPhrasesDeltaResponse? MatchPhrasesDelta { get; set; }
    
    [BsonIgnoreIfNull]
    public MatchInsightsResponse? MatchInsights { get; set; }
    
    [BsonIgnoreIfNull]
    public StatsFormtableResponse? FormTable { get; set; }
    
    [BsonIgnoreIfNull]
    public StatsTeamLastxExtendedResponse? LastXStatsTeam1 { get; set; }
    
    [BsonIgnoreIfNull]
    public StatsTeamLastxExtendedResponse? LastXStatsTeam2 { get; set; }
    
    [BsonIgnoreIfNull]
    public StatsCupBracketsResponse? CupBrackets { get; set; }
    
    [BsonIgnoreIfNull]
    public SeasonDynamicTableResponse? DynamicTable { get; set; }
}