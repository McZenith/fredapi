using System.Text.Json.Serialization;

namespace fredapi.Model.Live.StatsSeasonTopgoalsResponse;

public class StatsSeasonTopgoalsResponse
{
    [JsonPropertyName("query")]
    public string Query { get; set; }
        
    [JsonPropertyName("doc")]
    public List<Doc> Doc { get; set; }
}

public class Doc
{
    [JsonPropertyName("event")]
    public string Event { get; set; }
        
    // We assume the date string is in ISO format so DateTime works.
    [JsonPropertyName("date")]
    public DateTime Date { get; set; }
        
    [JsonPropertyName("data")]
    public Data Data { get; set; }
}

public class Data
{
    [JsonPropertyName("players")]
    public List<PlayerEntry> Players { get; set; }
        
    // The teams are keyed by a string (e.g. "2851") and map to a Team object.
    [JsonPropertyName("teams")]
    public Dictionary<string, Team> Teams { get; set; }
}

public class PlayerEntry
{
    [JsonPropertyName("_doc")]
    public string Doc { get; set; }
        
    [JsonPropertyName("_id")]
    public int Id { get; set; }
        
    [JsonPropertyName("playerid")]
    public int Playerid { get; set; }
        
    [JsonPropertyName("player")]
    public Player Player { get; set; }
        
    // Each player entry may have per-team statistics, keyed by team id.
    [JsonPropertyName("teams")]
    public Dictionary<string, TeamStats> Teams { get; set; }
        
    // Overall statistics for the player.
    [JsonPropertyName("total")]
    public Stats Total { get; set; }
        
    [JsonPropertyName("away")]
    public Score Away { get; set; }
        
    [JsonPropertyName("home")]
    public Score Home { get; set; }
        
    [JsonPropertyName("firsthalf")]
    public Score Firsthalf { get; set; }
        
    [JsonPropertyName("secondhalf")]
    public Score Secondhalf { get; set; }
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
    public string Fullname { get; set; }
        
    [JsonPropertyName("birthdate")]
    public TimeInfo Birthdate { get; set; }
        
    [JsonPropertyName("nationality")]
    public CountryCode Nationality { get; set; }
        
    [JsonPropertyName("position")]
    public Position Position { get; set; }
        
    [JsonPropertyName("primarypositiontype")]
    public string Primarypositiontype { get; set; }
        
    [JsonPropertyName("haslogo")]
    public bool Haslogo { get; set; }
        
    [JsonPropertyName("jerseynumber")]
    public int JerseysNumber { get; set; }
}

public class TimeInfo
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
    public int Uts { get; set; }
}

public class CountryCode
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
    public int Continentid { get; set; }
        
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
    public string Shortname { get; set; }
        
    [JsonPropertyName("abbr")]
    public string Abbr { get; set; }
}

public class TeamStats
{
    [JsonPropertyName("active")]
    public bool Active { get; set; }
        
    [JsonPropertyName("lastevent")]
    public string Lastevent { get; set; }
        
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
    public int SubstitutedIn { get; set; }
        
    // These fields may sometimes be missing, so we use nullable types.
    [JsonPropertyName("first_goals")]
    public int? FirstGoals { get; set; }
        
    [JsonPropertyName("last_goals")]
    public int? LastGoals { get; set; }
        
    [JsonPropertyName("shirtnumber")]
    public string Shirtnumber { get; set; }
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
    public int SubstitutedIn { get; set; }
        
    [JsonPropertyName("first_goals")]
    public int? FirstGoals { get; set; }
        
    [JsonPropertyName("last_goals")]
    public int? LastGoals { get; set; }
}

public class Score
{
    [JsonPropertyName("goals")]
    public int Goals { get; set; }
}

public class Team
{
    [JsonPropertyName("_doc")]
    public string Doc { get; set; }
        
    [JsonPropertyName("_id")]
    public int Id { get; set; }
        
    [JsonPropertyName("_rcid")]
    public int Rcid { get; set; }
        
    [JsonPropertyName("_sid")]
    public int Sid { get; set; }
        
    [JsonPropertyName("name")]
    public string Name { get; set; }
        
    [JsonPropertyName("mediumname")]
    public string Mediumname { get; set; }
        
    [JsonPropertyName("suffix")]
    public string Suffix { get; set; }
        
    [JsonPropertyName("abbr")]
    public string Abbr { get; set; }
        
    [JsonPropertyName("nickname")]
    public string Nickname { get; set; }
        
    [JsonPropertyName("teamtypeid")]
    public int Teamtypeid { get; set; }
        
    [JsonPropertyName("iscountry")]
    public bool Iscountry { get; set; }
        
    [JsonPropertyName("sex")]
    public string Sex { get; set; }
        
    [JsonPropertyName("haslogo")]
    public bool Haslogo { get; set; }
        
    [JsonPropertyName("founded")]
    public string Founded { get; set; }
        
    [JsonPropertyName("website")]
    public string Website { get; set; }
        
    [JsonPropertyName("homejersey")]
    public Jersey Homejersey { get; set; }
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
        
    // Some teams include a "sleevelong" property.
    [JsonPropertyName("sleevelong")]
    public string Sleevelong { get; set; }
        
    [JsonPropertyName("real")]
    public bool Real { get; set; }
}