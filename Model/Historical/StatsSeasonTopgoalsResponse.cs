using System.Text.Json.Serialization;

namespace fredapi.Model.Historical.StatsSeasonTopgoalsResponse;

public class StatsSeasonTopgoalsResponse
    {
        [JsonPropertyName("queryUrl")]
        public string QueryUrl { get; set; }
        
        [JsonPropertyName("doc")]
        public List<Doc> Doc { get; set; }
    }

    public class Doc
    {
        // “event” is a reserved word in C#, so we rename it.
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
        [JsonPropertyName("season")]
        public Season Season { get; set; }
        
        [JsonPropertyName("players")]
        public List<PlayerEntry> Players { get; set; }
        
        // The teams property is an object whose keys are team IDs (as strings)
        [JsonPropertyName("teams")]
        public Dictionary<string, Team> Teams { get; set; }
    }

    public class Season
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }
        
        [JsonPropertyName("_id")]
        public int Id { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("year")]
        public string Year { get; set; }
        
        // Add any other season properties as needed.
    }

    public class PlayerEntry
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }
        
        [JsonPropertyName("playerid")]
        public int PlayerId { get; set; }
        
        [JsonPropertyName("player")]
        public Player Player { get; set; }
        
        // The “teams” for a player comes as a dictionary keyed by team id
        [JsonPropertyName("teams")]
        public Dictionary<string, TeamStats> Teams { get; set; }
        
        [JsonPropertyName("total")]
        public Stats Total { get; set; }
        
        [JsonPropertyName("home")]
        public Stats Home { get; set; }
        
        [JsonPropertyName("away")]
        public Stats Away { get; set; }
        
        [JsonPropertyName("firsthalf")]
        public Stats FirstHalf { get; set; }
        
        [JsonPropertyName("secondhalf")]
        public Stats SecondHalf { get; set; }
    }

    public class Player
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }
        
        [JsonPropertyName("_id")]
        public int Id { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("fullname")]
        public string FullName { get; set; }
        
        [JsonPropertyName("birthdate")]
        public string Birthdate { get; set; }
        // Alternatively, you could parse this as a DateTime.
        
        [JsonPropertyName("nationality")]
        public Country Nationality { get; set; }
        
        [JsonPropertyName("position")]
        public Position Position { get; set; }
        
        [JsonPropertyName("primarypositiontype")]
        public object PrimaryPositionType { get; set; }
        
        [JsonPropertyName("haslogo")]
        public bool HasLogo { get; set; }
        
        [JsonPropertyName("jerseynumber")]
        public string JerseyNumber { get; set; }
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

    public class Position
    {
        [JsonPropertyName("_id")]
        public int Id { get; set; }
        
        [JsonPropertyName("_type")]
        public string Type { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("shortname")]
        public string ShortName { get; set; }
        
        [JsonPropertyName("abbr")]
        public string Abbr { get; set; }
    }

    public class TeamStats
    {
        [JsonPropertyName("active")]
        public bool Active { get; set; }
        
        [JsonPropertyName("lastevent")]
        public string LastEvent { get; set; }
        
        [JsonPropertyName("started")]
        public int Started { get; set; }
        
        [JsonPropertyName("goals")]
        public int Goals { get; set; }
        
        [JsonPropertyName("matches")]
        public int Matches { get; set; }
        
        [JsonPropertyName("goal_points")]
        public int GoalPoints { get; set; }
        
        [JsonPropertyName("minutes_played")]
        public int MinutesPlayed { get; set; }
        
        [JsonPropertyName("substituted_in")]
        public int? SubstitutedIn { get; set; }
        
        [JsonPropertyName("first_goals")]
        public int? FirstGoals { get; set; }
        
        [JsonPropertyName("last_goals")]
        public int? LastGoals { get; set; }
        
        [JsonPropertyName("shirtnumber")]
        public string ShirtNumber { get; set; }
    }

    public class Stats
    {
        [JsonPropertyName("goals")]
        public int Goals { get; set; }
        
        [JsonPropertyName("matches")]
        public int Matches { get; set; }
        
        [JsonPropertyName("goal_points")]
        public int GoalPoints { get; set; }
        
        [JsonPropertyName("minutes_played")]
        public int MinutesPlayed { get; set; }
        
        [JsonPropertyName("substituted_in")]
        public int? SubstitutedIn { get; set; }
        
        [JsonPropertyName("first_goals")]
        public int? FirstGoals { get; set; }
        
        [JsonPropertyName("last_goals")]
        public int? LastGoals { get; set; }
        
        [JsonPropertyName("penalties")]
        public int? Penalties { get; set; }
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
        public string Suffix { get; set; }
        
        [JsonPropertyName("abbr")]
        public string Abbr { get; set; }
        
        [JsonPropertyName("nickname")]
        public string Nickname { get; set; }
        
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
        public string Website { get; set; }
        
        [JsonPropertyName("homejersey")]
        public Jersey HomeJersey { get; set; }
    }

    public class Jersey
    {
        [JsonPropertyName("base")]
        public string Base { get; set; }
        
        [JsonPropertyName("sleeve")]
        public string Sleeve { get; set; }
        
        [JsonPropertyName("number")]
        public string Number { get; set; }
        
        [JsonPropertyName("type")]
        public string Type { get; set; }
        
        [JsonPropertyName("real")]
        public bool Real { get; set; }
        
        // Optional properties
        [JsonPropertyName("sleevelong")]
        public string SleeveLong { get; set; }
        
        [JsonPropertyName("stripes")]
        public string Stripes { get; set; }
    }