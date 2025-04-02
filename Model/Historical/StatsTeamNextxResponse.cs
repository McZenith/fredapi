using System.Text.Json.Serialization;

namespace fredapi.Model.Historical.StatsTeamNextxResponse
{
    public class StatsTeamNextxResponse
    {
        [JsonPropertyName("queryUrl")]
        public string QueryUrl { get; set; }

        [JsonPropertyName("doc")]
        public List<Doc> Doc { get; set; }
    }

    public class Doc
    {
        // "event" is a reserved word in C#, so we use EventName.
        [JsonPropertyName("event")]
        public string EventName { get; set; }
        
        [JsonPropertyName("_dob")]
        public long Dob { get; set; }
        
        [JsonPropertyName("_maxage")]
        public int MaxAge { get; set; }
        
        [JsonPropertyName("data")]
        public Data Data { get; set; }
    }

    public class Data
    {
        [JsonPropertyName("team")]
        public Team Team { get; set; }
        
        [JsonPropertyName("matches")]
        public List<Match> Matches { get; set; }
        
        [JsonPropertyName("tournaments")]
        public Dictionary<string, Tournament> Tournaments { get; set; }
        
        [JsonPropertyName("uniquetournaments")]
        public Dictionary<string, UniqueTournament> UniqueTournaments { get; set; }
        
        [JsonPropertyName("realcategories")]
        public Dictionary<string, RealCategory> RealCategories { get; set; }
    }

    public class Team
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }
        
        [JsonPropertyName("_id")]
        public int Id { get; set; }
        
        [JsonPropertyName("_rcid")]
        public int RcId { get; set; }
        
        [JsonPropertyName("_sid")]
        public int Sid { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("mediumname")]
        public string MediumName { get; set; }
        
        [JsonPropertyName("suffix")]
        public object Suffix { get; set; }
        
        [JsonPropertyName("abbr")]
        public string Abbr { get; set; }
        
        [JsonPropertyName("nickname")]
        public object Nickname { get; set; }
        
        [JsonPropertyName("teamtypeid")]
        public int TeamTypeId { get; set; }
        
        [JsonPropertyName("iscountry")]
        public bool IsCountry { get; set; }
        
        [JsonPropertyName("sex")]
        public string Sex { get; set; }
        
        [JsonPropertyName("haslogo")]
        public bool HasLogo { get; set; }
        
        [JsonPropertyName("founded")]
        public string Founded { get; set; }
        
        [JsonPropertyName("website")]
        public object Website { get; set; }
    }

    public class Match
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }
        
        [JsonPropertyName("_id")]
        public long Id { get; set; }
        
        [JsonPropertyName("_sid")]
        public int Sid { get; set; }
        
        [JsonPropertyName("_rcid")]
        public int RcId { get; set; }
        
        [JsonPropertyName("_tid")]
        public int Tid { get; set; }
        
        [JsonPropertyName("_utid")]
        public int Utid { get; set; }
        
        [JsonPropertyName("time")]
        public TimeData Time { get; set; }
        
        [JsonPropertyName("round")]
        public int? Round { get; set; }
        
        [JsonPropertyName("roundname")]
        public TableRound RoundName { get; set; }
        
        [JsonPropertyName("week")]
        public int Week { get; set; }
        
        [JsonPropertyName("result")]
        public Result Result { get; set; }
        
        [JsonPropertyName("periods")]
        public Periods Periods { get; set; }
        
        [JsonPropertyName("_seasonid")]
        public int SeasonId { get; set; }
        
        [JsonPropertyName("teams")]
        public MatchTeams Teams { get; set; }
        
        [JsonPropertyName("neutralground")]
        public bool NeutralGround { get; set; }
        
        [JsonPropertyName("comment")]
        public string Comment { get; set; }
        
        [JsonPropertyName("status")]
        public object Status { get; set; }
        
        [JsonPropertyName("tobeannounced")]
        public bool ToBeAnnounced { get; set; }
        
        [JsonPropertyName("postponed")]
        public bool Postponed { get; set; }
        
        [JsonPropertyName("canceled")]
        public bool Canceled { get; set; }
        
        [JsonPropertyName("inlivescore")]
        public bool InLiveScore { get; set; }
        
        [JsonPropertyName("stadiumid")]
        public int StadiumId { get; set; }
        
        [JsonPropertyName("bestof")]
        public object BestOf { get; set; }
        
        [JsonPropertyName("walkover")]
        public bool Walkover { get; set; }
        
        [JsonPropertyName("retired")]
        public bool Retired { get; set; }
        
        [JsonPropertyName("disqualified")]
        public bool Disqualified { get; set; }
        
        // The next two properties are extra in this response.
        [JsonPropertyName("jerseys")]
        public Jerseys Jerseys { get; set; }
        
        [JsonPropertyName("matchdifficultyrating")]
        public MatchDifficultyRating MatchDifficultyRating { get; set; }
    }

    public class Jerseys
    {
        [JsonPropertyName("home")]
        public JerseysDetail Home { get; set; }
        
        [JsonPropertyName("away")]
        public JerseysDetail Away { get; set; }
    }

    public class JerseysDetail
    {
        [JsonPropertyName("player")]
        public JerseyDetail Player { get; set; }
        
        [JsonPropertyName("GK")]
        public JerseyDetail GK { get; set; }
    }

    public class JerseyDetail
    {
        [JsonPropertyName("base")]
        public string Base { get; set; }
        
        [JsonPropertyName("sleeve")]
        public string Sleeve { get; set; }
        
        [JsonPropertyName("number")]
        public string Number { get; set; }
        
        // "stripes" is optional.
        [JsonPropertyName("stripes")]
        public string Stripes { get; set; }
        
        [JsonPropertyName("type")]
        public string Type { get; set; }
        
        // Some jersey objects may include an extra property "sleevelong"
        [JsonPropertyName("sleevelong")]
        public string SleeveLong { get; set; }
        
        [JsonPropertyName("real")]
        public bool Real { get; set; }
    }

    public class MatchDifficultyRating
    {
        [JsonPropertyName("home")]
        public int? Home { get; set; }
        
        [JsonPropertyName("away")]
        public int? Away { get; set; }
    }

    public class TimeData
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }
        
        [JsonPropertyName("time")]
        public string Time { get; set; }
        
        [JsonPropertyName("date")]
        public string Date { get; set; }
        
        [JsonPropertyName("tz")]
        public string Tz { get; set; }
        
        [JsonPropertyName("tzoffset")]
        public int Tzoffset { get; set; }
        
        [JsonPropertyName("uts")]
        public long Uts { get; set; }
    }

    public class TableRound
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }
        
        [JsonPropertyName("_id")]
        public int Id { get; set; }
        
        // In this sample roundname "name" is a number.
        [JsonPropertyName("name")]
        public int Name { get; set; }
    }

    public class Result
    {
        [JsonPropertyName("home")]
        public int? Home { get; set; }
        
        [JsonPropertyName("away")]
        public int? Away { get; set; }
        
        [JsonPropertyName("period")]
        public string Period { get; set; }
        
        [JsonPropertyName("winner")]
        public string Winner { get; set; }
    }

    public class Periods
    {
        [JsonPropertyName("p1")]
        public Score P1 { get; set; }
        
        [JsonPropertyName("ft")]
        public Score FT { get; set; }
        
        // Sometimes an overtime period "ot" is included.
        [JsonPropertyName("ot")]
        public Score OT { get; set; }
    }

    public class Score
    {
        [JsonPropertyName("home")]
        public int Home { get; set; }
        
        [JsonPropertyName("away")]
        public int Away { get; set; }
    }

    public class MatchTeams
    {
        [JsonPropertyName("home")]
        public BasicTeam Home { get; set; }
        
        [JsonPropertyName("away")]
        public BasicTeam Away { get; set; }
    }

    public class BasicTeam
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }
        
        [JsonPropertyName("_id")]
        public long Id { get; set; }
        
        [JsonPropertyName("_sid")]
        public int Sid { get; set; }
        
        [JsonPropertyName("uid")]
        public int Uid { get; set; }
        
        [JsonPropertyName("virtual")]
        public bool Virtual { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("mediumname")]
        public string MediumName { get; set; }
        
        [JsonPropertyName("abbr")]
        public string Abbr { get; set; }
        
        [JsonPropertyName("nickname")]
        public object Nickname { get; set; }
        
        [JsonPropertyName("iscountry")]
        public bool IsCountry { get; set; }
        
        [JsonPropertyName("haslogo")]
        public bool HasLogo { get; set; }
    }

    // Tournament and UniqueTournament classes
    public class Tournament
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }
        
        [JsonPropertyName("_id")]
        public int Id { get; set; }
        
        [JsonPropertyName("_sid")]
        public int Sid { get; set; }
        
        [JsonPropertyName("_rcid")]
        public int RcId { get; set; }
        
        [JsonPropertyName("_isk")]
        public int IsK { get; set; }
        
        [JsonPropertyName("_tid")]
        public int Tid { get; set; }
        
        [JsonPropertyName("_utid")]
        public int Utid { get; set; }
        
        [JsonPropertyName("_gender")]
        public string Gender { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("abbr")]
        public string Abbr { get; set; }
        
        [JsonPropertyName("ground")]
        public object Ground { get; set; }
        
        [JsonPropertyName("friendly")]
        public bool Friendly { get; set; }
        
        [JsonPropertyName("seasonid")]
        public int SeasonId { get; set; }
        
        [JsonPropertyName("currentseason")]
        public int CurrentSeason { get; set; }
        
        [JsonPropertyName("year")]
        public string Year { get; set; }
        
        [JsonPropertyName("seasontype")]
        public string SeasonType { get; set; }
        
        [JsonPropertyName("seasontypename")]
        public string SeasonTypeName { get; set; }
        
        [JsonPropertyName("seasontypeunique")]
        public string SeasonTypeUnique { get; set; }
        
        [JsonPropertyName("livetable")]
        public object LiveTable { get; set; }
        
        [JsonPropertyName("cuprosterid")]
        public object CupRosterId { get; set; }
        
        [JsonPropertyName("roundbyround")]
        public bool RoundByRound { get; set; }
        
        [JsonPropertyName("tournamentlevelorder")]
        public int TournamentLevelOrder { get; set; }
        
        [JsonPropertyName("tournamentlevelname")]
        public string TournamentLevelName { get; set; }
        
        [JsonPropertyName("outdated")]
        public bool Outdated { get; set; }
    }

    public class UniqueTournament
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }
        
        [JsonPropertyName("_id")]
        public int Id { get; set; }
        
        [JsonPropertyName("_utid")]
        public int Utid { get; set; }
        
        [JsonPropertyName("_sid")]
        public int Sid { get; set; }
        
        [JsonPropertyName("_rcid")]
        public int RcId { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("currentseason")]
        public int CurrentSeason { get; set; }
        
        [JsonPropertyName("friendly")]
        public bool Friendly { get; set; }
    }

    public class RealCategory
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }
        
        [JsonPropertyName("_id")]
        public int Id { get; set; }
        
        [JsonPropertyName("_sid")]
        public int Sid { get; set; }
        
        [JsonPropertyName("_rcid")]
        public int RcId { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("cc")]
        public Country CC { get; set; }
    }

    public class Country
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }
        
        [JsonPropertyName("_id")]
        public int Id { get; set; }
        
        [JsonPropertyName("a2")]
        public string A2 { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("a3")]
        public string A3 { get; set; }
        
        [JsonPropertyName("ioc")]
        public string Ioc { get; set; }
        
        [JsonPropertyName("continentid")]
        public int ContinentId { get; set; }
        
        [JsonPropertyName("continent")]
        public string Continent { get; set; }
        
        [JsonPropertyName("population")]
        public int Population { get; set; }
    }
}
