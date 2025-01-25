using System.Text.Json;
using System.Text.Json.Serialization;

namespace fredapi.Model.SportMatchesResponse;

public class SportMatchesResponse
{
    [JsonPropertyName("queryUrl")]
    public string QueryUrl { get; set; }
    
    [JsonPropertyName("doc")]
    public List<SportDoc> Doc { get; set; }
}

public class SportDoc
{
    [JsonPropertyName("event")]
    public string Event { get; set; }
    
    [JsonPropertyName("_dob")]
    public long Dob { get; set; }
    
    [JsonPropertyName("_maxage")]
    public int Maxage { get; set; }
    
    [JsonPropertyName("data")]
    public SportData Data { get; set; }
}

public class SportData
{
    [JsonPropertyName("sport")]
    public Sport Sport { get; set; }
}

public class Sport
{
    [JsonPropertyName("_doc")]
    public string Doc { get; set; }
    
    [JsonPropertyName("_id")]
    public int Id { get; set; }
    
    [JsonPropertyName("_sid")]
    public int Sid { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("realcategories")]
    public List<RealCategory> RealCategories { get; set; }
    
    [JsonPropertyName("live")]
    public bool Live { get; set; }
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
    public int Rcid { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("cc")]
    public CountryCode Cc { get; set; }
    
    [JsonPropertyName("tournaments")]
    public List<Tournament> Tournaments { get; set; }
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
    public int ContinentId { get; set; }
    
    [JsonPropertyName("continent")]
    public string Continent { get; set; }
    
    [JsonPropertyName("population")]
    public int Population { get; set; }
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
    public int Rcid { get; set; }
    
    [JsonPropertyName("_isk")]
    public int Isk { get; set; }
    
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
    public string Ground { get; set; }
    
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
    
    //[JsonConverter(typeof(BooleanOrStringConverter))]
    //[JsonPropertyName("livetable")]
    //public bool LiveTable { get; set; }
    
    [JsonPropertyName("cuprosterid")]
    public string CupRosterId { get; set; }
    
    [JsonPropertyName("roundbyround")]
    public bool RoundByRound { get; set; }
    
    [JsonPropertyName("tournamentlevelname")]
    public string TournamentLevelName { get; set; }
    
    [JsonPropertyName("outdated")]
    public bool Outdated { get; set; }
    
    [JsonPropertyName("matches")]
    public List<Match> Matches { get; set; }
}

public class Match
{
    [JsonPropertyName("_doc")]
    public string Doc { get; set; }
    
    [JsonPropertyName("_id")]
    public int Id { get; set; }
    
    [JsonPropertyName("_sid")]
    public int Sid { get; set; }
    
    [JsonPropertyName("_seasonid")]
    public int SeasonId { get; set; }
    
    [JsonPropertyName("_rcid")]
    public int Rcid { get; set; }
    
    [JsonPropertyName("_tid")]
    public int Tid { get; set; }
    
    [JsonPropertyName("_utid")]
    public int Utid { get; set; }
    
    [JsonPropertyName("round")]
    public int Round { get; set; }
    
    [JsonPropertyName("week")]
    public int Week { get; set; }
    
    [JsonPropertyName("coverage")]
    public Coverage Coverage { get; set; }
    
    [JsonPropertyName("result")]
    public Result Result { get; set; }
    
    [JsonPropertyName("periods")]
    public MatchPeriods? Periods { get; set; }
    
    //[JsonPropertyName("updated_uts")]
   // public long UpdatedUts { get; set; }
    
    //[JsonPropertyName("ended_uts")]
    //public bool EndedUts { get; set; }
    
    //[JsonPropertyName("p")]
    //public string P { get; set; }
    
    //[JsonPropertyName("ptime")]
    //public bool Ptime { get; set; }
    
    [JsonPropertyName("timeinfo")]
    public TimeInfo TimeInfo { get; set; }
    
    [JsonPropertyName("teams")]
    public Teams Teams { get; set; }
    
    [JsonPropertyName("status")]
    public Status Status { get; set; }
    
    [JsonPropertyName("removed")]
    public bool Removed { get; set; }
    
    [JsonPropertyName("facts")]
    public bool Facts { get; set; }
    
    [JsonPropertyName("stadiumid")]
    public int? StadiumId { get; set; }
    
    [JsonPropertyName("localderby")]
    public bool LocalDerby { get; set; }
    
    [JsonPropertyName("distance")]
    public int? Distance { get; set; }
    
    //[JsonPropertyName("weather")]
    //public string Weather { get; set; }
    
    //[JsonPropertyName("pitchcondition")]
    //public string PitchCondition { get; set; }
    
    //[JsonPropertyName("temperature")]
    //public string Temperature { get; set; }
    
    //[JsonPropertyName("wind")]
    //public string Wind { get; set; }
    
    //[JsonPropertyName("windadvantage")]
    //public int WindAdvantage { get; set; }
    
    [JsonConverter(typeof(BooleanStringConverter))]
    [JsonPropertyName("matchstatus")]
    public string MatchStatus { get; set; }
    
    [JsonPropertyName("postponed")]
    public bool Postponed { get; set; }
    
    [JsonPropertyName("cancelled")]
    public bool Cancelled { get; set; }
    
    [JsonPropertyName("walkover")]
    public bool Walkover { get; set; }
    
   // [JsonPropertyName("hf")]
    //public int Hf { get; set; }
    
    [JsonPropertyName("periodlength")]
    public int PeriodLength { get; set; }
    
    [JsonPropertyName("numberofperiods")]
    public int NumberOfPeriods { get; set; }
    
    [JsonPropertyName("overtimelength")]
    public int OvertimeLength { get; set; }
    
    [JsonPropertyName("tobeannounced")]
    public bool ToBeAnnounced { get; set; }
}

public class RoundName
{
    [JsonPropertyName("_doc")]
    public string Doc { get; set; }
    
    [JsonPropertyName("_id")]
    public int Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
}

public class Coverage
{
    [JsonPropertyName("lineup")]
    public int Lineup { get; set; }
    
    [JsonPropertyName("formations")]
    public int Formations { get; set; }
    
    [JsonPropertyName("livetable")]
    public int LiveTable { get; set; }
    
    [JsonPropertyName("injuries")]
    public int Injuries { get; set; }
    
    [JsonPropertyName("ballspotting")]
    public bool BallSpotting { get; set; }
    
    [JsonPropertyName("cornersonly")]
    public bool CornersOnly { get; set; }
    
    [JsonPropertyName("multicast")]
    public bool Multicast { get; set; }
    
    [JsonPropertyName("scoutmatch")]
    public int ScoutMatch { get; set; }
    
    [JsonPropertyName("scoutcoveragestatus")]
    public int ScoutCoverageStatus { get; set; }
    
    [JsonPropertyName("scoutconnected")]
    public bool ScoutConnected { get; set; }
    
    [JsonPropertyName("liveodds")]
    public bool LiveOdds { get; set; }
    
    [JsonPropertyName("deepercoverage")]
    public bool DeeperCoverage { get; set; }
    
    [JsonPropertyName("tacticallineup")]
    public bool TacticalLineup { get; set; }
    
    [JsonPropertyName("basiclineup")]
    public bool BasicLineup { get; set; }
    
    [JsonPropertyName("hasstats")]
    public bool HasStats { get; set; }
    
    [JsonPropertyName("inlivescore")]
    public bool InLiveScore { get; set; }
}

public class Result
{
    [JsonConverter(typeof(JsonStringNumberConverter))]
    [JsonPropertyName("home")]
    public string Home { get; set; }
    [JsonConverter(typeof(JsonStringNumberConverter))]
    [JsonPropertyName("away")]
    public string Away { get; set; }
    
    [JsonPropertyName("winner")]
    public string Winner { get; set; }
}

public class TimeInfo
{
    [JsonPropertyName("injurytime")]
    public string InjuryTime { get; set; }
    
    [JsonPropertyName("ended")]
    public string Ended { get; set; }
    
    [JsonPropertyName("started")]
    public string Started { get; set; }
    
    [JsonPropertyName("played")]
    public string Played { get; set; }
    
    [JsonPropertyName("remaining")]
    public string Remaining { get; set; }
    
    [JsonPropertyName("running")]
    public bool Running { get; set; }
}

public class Teams
{
    [JsonPropertyName("home")]
    public Team Home { get; set; }
    
    [JsonPropertyName("away")]
    public Team Away { get; set; }
}

public class MatchPeriods
{
    // Individual periods can be null
    [JsonPropertyName("p1")]
    public Period? P1 { get; set; }
    
    [JsonPropertyName("p2")]
    public Period? P2 { get; set; }
    
    [JsonPropertyName("ft")]
    public Period? FT { get; set; }
    
    [JsonPropertyName("ht")]
    public Period? HT { get; set; }
}

public class Period
{
    [JsonConverter(typeof(JsonStringNumberConverter))]
    public string? Home { get; set; }

    [JsonConverter(typeof(JsonStringNumberConverter))]
    public string? Away { get; set; }
}

public class Team
{
    [JsonPropertyName("_id")]
    public int Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("mediumname")]
    public string MediumName { get; set; }
    
    [JsonPropertyName("abbr")]
    public string Abbr { get; set; }
    
    [JsonPropertyName("nickname")]
    public string Nickname { get; set; }
    
    [JsonPropertyName("iscountry")]
    public bool IsCountry { get; set; }
    
    [JsonPropertyName("haslogo")]
    public bool HasLogo { get; set; }
}

public class Status
{
    [JsonPropertyName("_doc")]
    public string Doc { get; set; }
    
    [JsonPropertyName("_id")]
    public int Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("shortName")]
    public string ShortName { get; set; }
}

public class BooleanStringConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        try
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    return reader.GetString();
                case JsonTokenType.Number:
                    return reader.GetInt64().ToString();
                case JsonTokenType.True:
                    return "true";
                case JsonTokenType.False:
                    return "false";
                case JsonTokenType.Null:
                    return null;
                default:
                    return null;
            }
        }
        catch
        {
            return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value == null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(value);
    }
}

public class BooleanOrStringConverter : JsonConverter<object>
{
    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        try
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    return reader.GetString();
                case JsonTokenType.Number:
                    return reader.GetInt64().ToString();
                case JsonTokenType.True:
                    return true;
                case JsonTokenType.False:
                    return false;
                case JsonTokenType.Null:
                    return null;
                default:
                    return null;
            }
        }
        catch
        {
            return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
        }
        else if (value is bool boolValue)
        {
            writer.WriteBooleanValue(boolValue);
        }
        else
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}

public class JsonStringNumberConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        try
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    return reader.GetString();
                case JsonTokenType.Number:
                    // Handle both integer and decimal numbers
                    if (reader.TryGetInt32(out int intValue))
                        return intValue.ToString();
                    if (reader.TryGetInt64(out long longValue))
                        return longValue.ToString();
                    if (reader.TryGetDouble(out double doubleValue))
                        return doubleValue.ToString();
                    return "0";
                case JsonTokenType.True:
                    return "true";
                case JsonTokenType.False:
                    return "false";
                case JsonTokenType.Null:
                    return null;
                default:
                    return null;
            }
        }
        catch (Exception)
        {
            return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value == null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(value);
    }
}