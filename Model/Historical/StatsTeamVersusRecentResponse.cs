using System.Text.Json.Serialization;

namespace fredapi.Model.Historical.StatsTeamVersusRecentResponse;

public class StatsTeamVersusRecentResponse

{
    [JsonPropertyName("queryUrl")] public string QueryUrl { get; set; }

    [JsonPropertyName("doc")] public List<Doc> Docs { get; set; }
}

public class Doc
{
    [JsonPropertyName("event")] public string Event { get; set; }

    [JsonPropertyName("_dob")] public long Dob { get; set; }

    [JsonPropertyName("_maxage")] public int MaxAge { get; set; }

    [JsonPropertyName("data")] public Data Data { get; set; }
}

public class Data
{
    [JsonPropertyName("livematchid")] public object LiveMatchId { get; set; } // type unknown (null in sample)

    [JsonPropertyName("matches")] public List<Match> Matches { get; set; }

    [JsonPropertyName("tournaments")] public Dictionary<string, Tournament> Tournaments { get; set; }

    [JsonPropertyName("realcategories")] public Dictionary<string, RealCategory> RealCategories { get; set; }

    [JsonPropertyName("teams")] public Dictionary<string, UniqueTeam> Teams { get; set; }

    [JsonPropertyName("currentmanagers")] public Dictionary<string, List<Player>> CurrentManagers { get; set; }

    [JsonPropertyName("jersey")] public Dictionary<string, Jersey> Jersey { get; set; }

    [JsonPropertyName("next")] public Match Next { get; set; }
}

public class Match
{
    [JsonPropertyName("_doc")] public string Doc { get; set; }

    [JsonPropertyName("_id")] public int Id { get; set; }

    [JsonPropertyName("_sid")] public int Sid { get; set; }

    [JsonPropertyName("_rcid")] public int Rcid { get; set; }

    [JsonPropertyName("_tid")] public int Tid { get; set; }

    [JsonPropertyName("_utid")] public int Utid { get; set; }

    [JsonPropertyName("time")] public TimeInfo Time { get; set; }

    [JsonPropertyName("round")] public int Round { get; set; }

    [JsonPropertyName("roundname")] public RoundName RoundName { get; set; }

    [JsonPropertyName("week")] public int Week { get; set; }

    [JsonPropertyName("result")] public Result Result { get; set; }

    [JsonPropertyName("periods")] public Periods Periods { get; set; }

    [JsonPropertyName("_seasonid")] public int SeasonId { get; set; }

    [JsonPropertyName("teams")] public MatchTeams Teams { get; set; }

    [JsonPropertyName("neutralground")] public bool NeutralGround { get; set; }

    [JsonPropertyName("comment")] public string Comment { get; set; }

    [JsonPropertyName("status")] public object Status { get; set; }

    [JsonPropertyName("tobeannounced")] public bool ToBeAnnounced { get; set; }

    [JsonPropertyName("postponed")] public bool Postponed { get; set; }

    [JsonPropertyName("canceled")] public bool Canceled { get; set; }

    [JsonPropertyName("inlivescore")] public bool InLiveScore { get; set; }

    [JsonPropertyName("stadiumid")] public int StadiumId { get; set; }

    [JsonPropertyName("bestof")] public object BestOf { get; set; }

    [JsonPropertyName("walkover")] public bool Walkover { get; set; }

    [JsonPropertyName("retired")] public bool Retired { get; set; }

    [JsonPropertyName("disqualified")] public bool Disqualified { get; set; }

    // Only present in the "next" match:
    [JsonPropertyName("referee")] public List<Player> Referee { get; set; }

    [JsonPropertyName("manager")] public ManagerPair Manager { get; set; }

    [JsonPropertyName("stadium")] public Stadium Stadium { get; set; }

    [JsonPropertyName("matchdifficultyrating")]
    public MatchDifficultyRating MatchDifficultyRating { get; set; }
}

public class TimeInfo
{
    [JsonPropertyName("_doc")] public string Doc { get; set; }

    [JsonPropertyName("time")] public string Time { get; set; }

    [JsonPropertyName("date")] public string Date { get; set; }

    [JsonPropertyName("tz")] public string TimeZone { get; set; }

    [JsonPropertyName("tzoffset")] public int TimeZoneOffset { get; set; }

    [JsonPropertyName("uts")] public long Uts { get; set; }
}

public class RoundName
{
    [JsonPropertyName("_doc")] public string Doc { get; set; }

    [JsonPropertyName("_id")] public int Id { get; set; }

    [JsonPropertyName("name")] public int Name { get; set; }
}

public class Result
{
    [JsonPropertyName("home")] public int? Home { get; set; }

    [JsonPropertyName("away")] public int? Away { get; set; }

    [JsonPropertyName("period")] public string Period { get; set; }

    [JsonPropertyName("winner")] public string Winner { get; set; }
}

public class Periods
{
    [JsonPropertyName("p1")] public Score P1 { get; set; }

    [JsonPropertyName("ft")] public Score FT { get; set; }
}

public class Score
{
    [JsonPropertyName("home")] public int Home { get; set; }

    [JsonPropertyName("away")] public int Away { get; set; }
}

public class MatchTeams
{
    [JsonPropertyName("home")] public TeamDetail Home { get; set; }

    [JsonPropertyName("away")] public TeamDetail Away { get; set; }
}

public class TeamDetail
{
    [JsonPropertyName("_doc")] public string Doc { get; set; }

    [JsonPropertyName("_id")] public int Id { get; set; }

    [JsonPropertyName("_sid")] public int Sid { get; set; }

    [JsonPropertyName("uid")] public int Uid { get; set; }

    [JsonPropertyName("virtual")] public bool Virtual { get; set; }

    [JsonPropertyName("name")] public string Name { get; set; }

    [JsonPropertyName("mediumname")] public string MediumName { get; set; }

    [JsonPropertyName("abbr")] public string Abbr { get; set; }

    [JsonPropertyName("nickname")] public string Nickname { get; set; }

    [JsonPropertyName("iscountry")] public bool IsCountry { get; set; }

    [JsonPropertyName("haslogo")] public bool HasLogo { get; set; }
}

public class Tournament
{
    [JsonPropertyName("_doc")] public string Doc { get; set; }

    [JsonPropertyName("_id")] public int Id { get; set; }

    [JsonPropertyName("_sid")] public int Sid { get; set; }

    [JsonPropertyName("_rcid")] public int Rcid { get; set; }

    [JsonPropertyName("_isk")] public int Isk { get; set; }

    [JsonPropertyName("_tid")] public int Tid { get; set; }

    [JsonPropertyName("_utid")] public int Utid { get; set; }

    [JsonPropertyName("_gender")] public string Gender { get; set; }

    [JsonPropertyName("name")] public string Name { get; set; }

    [JsonPropertyName("abbr")] public string Abbr { get; set; }

    [JsonPropertyName("ground")] public string Ground { get; set; }

    [JsonPropertyName("friendly")] public bool Friendly { get; set; }

    [JsonPropertyName("seasonid")] public int SeasonId { get; set; }

    [JsonPropertyName("currentseason")] public int CurrentSeason { get; set; }

    [JsonPropertyName("year")] public string Year { get; set; }

    [JsonPropertyName("seasontype")] public string SeasonType { get; set; }

    [JsonPropertyName("seasontypename")] public string SeasonTypeName { get; set; }

    [JsonPropertyName("seasontypeunique")] public string SeasonTypeUnique { get; set; }

    [JsonPropertyName("livetable")] public int LiveTable { get; set; }

    [JsonPropertyName("cuprosterid")] public object CupRosterId { get; set; }

    [JsonPropertyName("roundbyround")] public bool RoundByRound { get; set; }

    [JsonPropertyName("tournamentlevelorder")]
    public int TournamentLevelOrder { get; set; }

    [JsonPropertyName("tournamentlevelname")]
    public string TournamentLevelName { get; set; }

    [JsonPropertyName("outdated")] public bool Outdated { get; set; }
}

public class RealCategory
{
    [JsonPropertyName("_doc")] public string Doc { get; set; }

    [JsonPropertyName("_id")] public int Id { get; set; }

    [JsonPropertyName("_sid")] public int Sid { get; set; }

    [JsonPropertyName("_rcid")] public int Rcid { get; set; }

    [JsonPropertyName("name")] public string Name { get; set; }

    [JsonPropertyName("cc")] public CountryCode CC { get; set; }
}

public class CountryCode
{
    [JsonPropertyName("_id")] public int Id { get; set; }

    [JsonPropertyName("a2")] public string A2 { get; set; }

    [JsonPropertyName("name")] public string Name { get; set; }

    [JsonPropertyName("a3")] public string A3 { get; set; }

    [JsonPropertyName("ioc")] public string Ioc { get; set; }

    [JsonPropertyName("continentid")] public int ContinentId { get; set; }

    [JsonPropertyName("continent")] public string Continent { get; set; }

    [JsonPropertyName("population")] public int Population { get; set; }
}

public class UniqueTeam
{
    [JsonPropertyName("_doc")] public string Doc { get; set; }

    [JsonPropertyName("_id")] public int Id { get; set; }

    [JsonPropertyName("_rcid")] public int Rcid { get; set; }

    [JsonPropertyName("_sid")] public int Sid { get; set; }

    [JsonPropertyName("name")] public string Name { get; set; }

    [JsonPropertyName("mediumname")] public string MediumName { get; set; }

    [JsonPropertyName("suffix")] public string Suffix { get; set; }

    [JsonPropertyName("abbr")] public string Abbr { get; set; }

    [JsonPropertyName("nickname")] public string Nickname { get; set; }

    [JsonPropertyName("teamtypeid")] public int TeamTypeId { get; set; }

    [JsonPropertyName("iscountry")] public bool IsCountry { get; set; }

    [JsonPropertyName("sex")] public string Sex { get; set; }

    [JsonPropertyName("haslogo")] public bool HasLogo { get; set; }

    [JsonPropertyName("founded")] public string Founded { get; set; }

    [JsonPropertyName("website")] public string Website { get; set; }
}

public class Player
{
    [JsonPropertyName("_doc")] public string Doc { get; set; } // e.g. "player"

    [JsonPropertyName("_id")] public int Id { get; set; }

    [JsonPropertyName("name")] public string Name { get; set; }

    [JsonPropertyName("fullname")] public string FullName { get; set; }

    [JsonPropertyName("birthdate")] public TimeInfo BirthDate { get; set; }

    [JsonPropertyName("nationality")] public CountryCode Nationality { get; set; }

    [JsonPropertyName("primarypositiontype")]
    public object PrimaryPositionType { get; set; }

    [JsonPropertyName("haslogo")] public bool HasLogo { get; set; }

    [JsonPropertyName("membersince")] public TimeInfo MemberSince { get; set; }
}

public class Jersey
{
    [JsonPropertyName("base")] public string Base { get; set; }

    [JsonPropertyName("sleeve")] public string Sleeve { get; set; }

    [JsonPropertyName("number")] public string Number { get; set; }

    [JsonPropertyName("stripes")] public string Stripes { get; set; }

    [JsonPropertyName("type")] public string Type { get; set; }

    [JsonPropertyName("real")] public bool Real { get; set; }
}

public class ManagerPair
{
    [JsonPropertyName("home")] public Player Home { get; set; }

    [JsonPropertyName("away")] public Player Away { get; set; }
}

public class Stadium
{
    [JsonPropertyName("_doc")] public string Doc { get; set; }

    [JsonPropertyName("_id")] public string Id { get; set; } // sometimes an id string

    [JsonPropertyName("name")] public string Name { get; set; }

    [JsonPropertyName("description")] public string Description { get; set; }

    [JsonPropertyName("city")] public string City { get; set; }

    [JsonPropertyName("country")] public string Country { get; set; }

    [JsonPropertyName("state")] public string State { get; set; }

    [JsonPropertyName("cc")] public CountryCode CC { get; set; }

    [JsonPropertyName("capacity")] public string Capacity { get; set; }

    [JsonPropertyName("hometeams")] public List<UniqueTeam> HomeTeams { get; set; }

    [JsonPropertyName("constryear")] public string ConstrYear { get; set; }

    [JsonPropertyName("googlecoords")] public string GoogleCoords { get; set; }

    [JsonPropertyName("pitchsize")] public PitchSize PitchSize { get; set; }
}

public class PitchSize
{
    [JsonPropertyName("x")] public int X { get; set; }

    [JsonPropertyName("y")] public int Y { get; set; }
}

public class MatchDifficultyRating
{
    [JsonPropertyName("away")] public int Away { get; set; }

    [JsonPropertyName("home")] public int Home { get; set; }
}