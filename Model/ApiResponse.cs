using System.Text.Json.Serialization;

namespace fredapi.Model.ApiResponse;

public class ApiResponse
{
    [JsonPropertyName("bizCode")]
    public int BizCode { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; }

    [JsonPropertyName("data")]
    public List<TournamentData> Data { get; set; }
}

public class TournamentData
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("events")]
    public List<Event> Events { get; set; }

    [JsonPropertyName("categoryName")]
    public string CategoryName { get; set; }

    [JsonPropertyName("categoryId")]
    public string CategoryId { get; set; }
}

public class Event
{
    [JsonPropertyName("eventId")]
    public string EventId { get; set; }

    [JsonPropertyName("gameId")]
    public string GameId { get; set; }

    [JsonPropertyName("productStatus")]
    public string ProductStatus { get; set; }

    [JsonPropertyName("estimateStartTime")]
    public long EstimateStartTime { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("setScore")]
    public string SetScore { get; set; }

    [JsonPropertyName("gameScore")]
    public List<string> GameScore { get; set; }

    [JsonPropertyName("period")]
    public string Period { get; set; }

    [JsonPropertyName("matchStatus")]
    public string MatchStatus { get; set; }

    [JsonPropertyName("playedSeconds")]
    public string PlayedSeconds { get; set; }

    [JsonPropertyName("homeTeamId")]
    public string HomeTeamId { get; set; }

    [JsonPropertyName("homeTeamName")]
    public string HomeTeamName { get; set; }

    [JsonPropertyName("awayTeamName")]
    public string AwayTeamName { get; set; }

    [JsonPropertyName("awayTeamId")]
    public string AwayTeamId { get; set; }

    [JsonPropertyName("sport")]
    public Sport Sport { get; set; }

    [JsonPropertyName("totalMarketSize")]
    public int TotalMarketSize { get; set; }

    [JsonPropertyName("markets")]
    public List<Market> Markets { get; set; }

    [JsonPropertyName("bookingStatus")]
    public string BookingStatus { get; set; }

    [JsonPropertyName("topTeam")]
    public bool TopTeam { get; set; }

    [JsonPropertyName("commentsNum")]
    public int? CommentsNum { get; set; }

    [JsonPropertyName("topicId")]
    public int? TopicId { get; set; }

    [JsonPropertyName("fixtureVenue")]
    public FixtureVenue FixtureVenue { get; set; }

    [JsonPropertyName("giftGrabActivityResult")]
    public GiftGrabActivityResultVO GiftGrabActivityResultVO { get; set; }

    [JsonPropertyName("ai")]
    public bool Ai { get; set; }

    [JsonPropertyName("bgEvent")]
    public bool BgEvent { get; set; }

    [JsonPropertyName("matchTrackerNotAllowed")]
    public bool MatchTrackerNotAllowed { get; set; }

    [JsonPropertyName("eventSource")]
    public EventSource EventSource { get; set; }

    [JsonPropertyName("banned")]
    public bool Banned { get; set; }
}

public class Category
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("tournament")]
    public Tournament Tournament { get; set; }
}

public class Tournament
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }
}

public class Market
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("specifier")]
    public string Specifier { get; set; }

    [JsonPropertyName("product")]
    public int Product { get; set; }

    [JsonPropertyName("desc")]
    public string Desc { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("cashOutStatus")]
    public int? CashOutStatus { get; set; }

    [JsonPropertyName("group")]
    public string Group { get; set; }

    [JsonPropertyName("groupId")]
    public string GroupId { get; set; }

    [JsonPropertyName("marketGuide")]
    public string MarketGuide { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("favourite")]
    public int Favourite { get; set; }

    [JsonPropertyName("outcomes")]
    public List<Outcome> Outcomes { get; set; }

    [JsonPropertyName("farNearOdds")]
    public int FarNearOdds { get; set; }

    [JsonPropertyName("marketExtendVOS")]
    public List<MarketExtendVO> MarketExtendVOS { get; set; }

    [JsonPropertyName("sourceType")]
    public string? SourceType { get; set; }

    [JsonPropertyName("availableScore")]
    public string AvailableScore { get; set; }

    [JsonPropertyName("banned")]
    public bool Banned { get; set; }

    [JsonPropertyName("suspendedReason")]
    public string SuspendedReason { get; set; }
}

public class Outcome
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("odds")]
    public string Odds { get; set; }

    [JsonPropertyName("probability")]
    public string Probability { get; set; }

    [JsonPropertyName("isActive")]
    public int IsActive { get; set; }

    [JsonPropertyName("cashOutIsActive")]
    public int? CashOutIsActive { get; set; }

    [JsonPropertyName("desc")]
    public string Desc { get; set; }

    [JsonPropertyName("isWinning")]
    public int? IsWinning { get; set; }

    [JsonPropertyName("refundFactor")]
    public double? RefundFactor { get; set; }
}

public class MarketExtendVO
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("rootMarketId")]
    public string RootMarketId { get; set; }

    [JsonPropertyName("nodeMarketId")]
    public string NodeMarketId { get; set; }

    [JsonPropertyName("notSupport")]
    public bool NotSupport { get; set; }
}

public class FixtureVenue
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
}

public class GiftGrabActivityResultVO
{
    [JsonPropertyName("activityEnabled")]
    public bool ActivityEnabled { get; set; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }
}

public class EventSource
{
    [JsonPropertyName("preMatchSource")]
    public Source? PreMatchSource { get; set; }

    [JsonPropertyName("liveSource")]
    public Source? LiveSource { get; set; }
}

public class Source
{
    [JsonPropertyName("sourceType")]
    public string SourceType { get; set; }

    [JsonPropertyName("sourceId")]
    public string SourceId { get; set; }
}

public class Sport
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("category")]
    public Category Category { get; set; }
}