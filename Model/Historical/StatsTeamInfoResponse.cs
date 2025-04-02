using System.Text.Json.Serialization;

namespace fredapi.Model.Historical.StatsTeamInfoResponse
{
    public class StatsTeamInfoResponse
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
        
        [JsonPropertyName("twitter")]
        public string Twitter { get; set; }
        
        [JsonPropertyName("hashtag")]
        public string Hashtag { get; set; }
        
        [JsonPropertyName("matchup")]
        public string Matchup { get; set; }
        
        [JsonPropertyName("homejersey")]
        public Jersey HomeJersey { get; set; }
        
        [JsonPropertyName("awayjersey")]
        public Jersey AwayJersey { get; set; }
        
        [JsonPropertyName("awayjersey2")]
        public Jersey AwayJersey2 { get; set; }
        
        [JsonPropertyName("gkjersey")]
        public Jersey GkJersey { get; set; }
        
        [JsonPropertyName("tournaments")]
        public List<Tournament> Tournaments { get; set; }
        
        [JsonPropertyName("stadium")]
        public Stadium Stadium { get; set; }
        
        [JsonPropertyName("manager")]
        public Manager Manager { get; set; }
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

    public class Jersey
    {
        [JsonPropertyName("base")]
        public string Base { get; set; }
        
        [JsonPropertyName("sleeve")]
        public string Sleeve { get; set; }
        
        [JsonPropertyName("number")]
        public string Number { get; set; }
        
        // Some jerseys include additional properties such as stripes.
        [JsonPropertyName("stripes")]
        public string Stripes { get; set; }
        
        [JsonPropertyName("type")]
        public string Type { get; set; }
        
        [JsonPropertyName("real")]
        public bool Real { get; set; }
    }

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

    public class Stadium
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }
        
        [JsonPropertyName("_id")]
        public string Id { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("description")]
        public string Description { get; set; }
        
        [JsonPropertyName("city")]
        public string City { get; set; }
        
        [JsonPropertyName("country")]
        public string Country { get; set; }
        
        [JsonPropertyName("state")]
        public object State { get; set; }
        
        // cc is a country object.
        [JsonPropertyName("cc")]
        public Country CC { get; set; }
        
        [JsonPropertyName("capacity")]
        public string Capacity { get; set; }
        
        [JsonPropertyName("hometeams")]
        public List<Team> HomeTeams { get; set; }
        
        [JsonPropertyName("constryear")]
        public string ConstructionYear { get; set; }
        
        [JsonPropertyName("googlecoords")]
        public string GoogleCoords { get; set; }
        
        [JsonPropertyName("pitchsize")]
        public PitchSize PitchSize { get; set; }
    }

    public class PitchSize
    {
        [JsonPropertyName("x")]
        public int X { get; set; }
        
        [JsonPropertyName("y")]
        public int Y { get; set; }
    }

    public class Manager
    {
        // The manager object is similar to a player.
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }
        
        [JsonPropertyName("_id")]
        public int Id { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("fullname")]
        public string FullName { get; set; }
        
        [JsonPropertyName("birthdate")]
        public TimeData Birthdate { get; set; }
        
        [JsonPropertyName("nationality")]
        public Country Nationality { get; set; }
        
        [JsonPropertyName("primarypositiontype")]
        public object PrimaryPositionType { get; set; }
        
        [JsonPropertyName("haslogo")]
        public bool HasLogo { get; set; }
        
        [JsonPropertyName("membersince")]
        public TimeData MemberSince { get; set; }
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

    // Added Country class
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
