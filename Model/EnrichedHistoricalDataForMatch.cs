using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using fredapi.Model.Historical.MatchBookmakerOdds;
using fredapi.Model.Historical.MatchDetailsExtended;
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
using fredapi.Model.Historical.SeasonTeamPositionHistoryResponse;
using fredapi.Model.Historical.StatsCupBracketsResponse;
using fredapi.Model.Historical.StatsFormtable;
using fredapi.Model.Historical.StatsMatchForm;
using fredapi.Model.Historical.StatsMatchSituationResponse;
using fredapi.Model.Historical.StatsMatchTableslice;
using fredapi.Model.Historical.StatsSeasonGoals;
using fredapi.Model.Historical.StatsSeasonMeta;
using FredApi.Model.Historical.StatsSeasonOverUnder;
using fredapi.Model.Historical.StatsSeasonTopgoalsResponse;
using fredapi.Model.Historical.StatsTeamInfoResponse;
using fredapi.Model.Historical.StatsTeamLastxResponse;
using fredapi.Model.Historical.StatsTeamSquadResponse;
using fredapi.Model.Historical.StatsTeamVersusRecentResponse;
using fredapi.Model.Historical.TeamFixturesResponse;
using fredapi.Model.Live.StatsSeasonUniqueTeamStatsResponse;
using Match = fredapi.Model.Historical.MatchInfo.Match;

namespace fredapi.Model.EnrichedHistoricalDataForMatch;

public class EnrichedHistoricalDataForMatch
{
    [BsonId]
    public ObjectId Id { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Core identifiers
    public string MatchId { get; set; } = string.Empty;
    public string SeasonId { get; set; } = string.Empty;
    public string Team1Id { get; set; } = string.Empty;
    public string Team2Id { get; set; } = string.Empty;

    // Core match data - from initial response
    [BsonIgnoreIfNull]
    public SportMatchesResponse.Match? CoreMatchData { get; set; }

    // Match-specific responses
    [BsonIgnoreIfNull]
    public MatchInfoResponse? MatchInfo { get; set; }

    [BsonIgnoreIfNull]
    public MatchTimelinedeltaResponse? MatchTimelineDelta { get; set; }

    [BsonIgnoreIfNull]
    public MatchDetailsExtendedResponse? MatchDetailsExtended { get; set; }

    [BsonIgnoreIfNull]
    public MatchTimelineResponse? MatchTimeline { get; set; }

    [BsonIgnoreIfNull]
    public MatchSquadsResponse? MatchSquads { get; set; }

    [BsonIgnoreIfNull]
    public MatchPhrasesResponse? MatchPhrases { get; set; }
    
    [BsonIgnoreIfNull]
    public MatchPhrasesDeltaResponse? MatchPhrasesDelta { get; set; }

    [BsonIgnoreIfNull]
    public MatchFunfactsResponse? MatchFunFacts { get; set; }

    [BsonIgnoreIfNull]
    public MatchBookmakerOddsResponse? BookmakerOdds { get; set; }

    // Season-related responses
    [BsonIgnoreIfNull]
    public StatsSeasonMetaResponse? SeasonMeta { get; set; }

    [BsonIgnoreIfNull]
    public SeasonLivetableResponse? SeasonLiveTable { get; set; }

    [BsonIgnoreIfNull]
    public StatsSeasonGoalsResponse? SeasonGoals { get; set; }

    [BsonIgnoreIfNull]
    public StatsFormtableResponse? FormTable { get; set; }

    [BsonIgnoreIfNull]
    public StatsMatchTablesliceResponse? TableSlice { get; set; }

    // Match form and statistics
    [BsonIgnoreIfNull]
    public StatsMatchFormResponse? MatchForm { get; set; }

    // Team-specific responses
    [BsonIgnoreIfNull]
    public StatsTeamVersusRecentResponse? TeamVersusRecent { get; set; }

    [BsonIgnoreIfNull]
    public StatsTeamLastxResponse? Team1LastX { get; set; }

    [BsonIgnoreIfNull]
    public StatsTeamLastxResponse? Team2LastX { get; set; }
    
    // Position history and statistics
    [BsonIgnoreIfNull]
    public SeasonTeamPositionHistoryResponse? TeamPositionHistory { get; set; }

    // Optional extras that might not be available for all matches
    [BsonIgnoreIfNull]
    public StatsMatchSituationResponse? MatchSituation { get; set; }

    [BsonIgnoreIfNull]
    public MatchInsightsResponse? MatchInsights { get; set; }

    [BsonIgnoreIfNull]
    public StatsCupBracketsResponse? CupBrackets { get; set; }

    [BsonIgnoreIfNull]
    public SeasonDynamicTableResponse? DynamicTable { get; set; }
    
    [BsonIgnoreIfNull]
    public StatsSeasonOverUnderResponse? OverUnderStats { get; set; }
    
    [BsonIgnoreIfNull]
    public StatsTeamSquadResponse? TeamSquad1 { get; set; }
    
    [BsonIgnoreIfNull]
    public StatsTeamSquadResponse? TeamSquad2 { get; set; }
    
    [BsonIgnoreIfNull]
    public StatsTeamInfoResponse? TeamInfo1 { get; set; }
    [BsonIgnoreIfNull]
    public StatsTeamInfoResponse? TeamInfo2 { get; set; }
    
    [BsonIgnoreIfNull]
    public TeamFixturesResponse? TeamFixtures1 { get; set; }
    
    [BsonIgnoreIfNull]
    public TeamFixturesResponse? TeamFixtures2 { get; set; }
    
    [BsonIgnoreIfNull]
    public StatsSeasonUniqueTeamStatsResponse? UniqueTeamStats { get; set; }
    
    [BsonIgnoreIfNull]
    public StatsSeasonTopgoalsResponse? SeasonTopGoals { get; set; }
}