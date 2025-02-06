using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace fredapi.Model.Historical.MatchSquads
{
    // Root response
    public class MatchSquadsResponse
    {
        [JsonPropertyName("queryUrl")]
        public string QueryUrl { get; set; }
        
        [JsonPropertyName("doc")]
        public List<MatchSquadsDoc> Doc { get; set; }
    }

    public class MatchSquadsDoc
    {
        [JsonPropertyName("event")]
        public string Event { get; set; }
        
        [JsonPropertyName("_dob")]
        public long Dob { get; set; }
        
        [JsonPropertyName("_maxage")]
        public int MaxAge { get; set; }
        
        [JsonPropertyName("data")]
        public MatchSquadsData Data { get; set; }
    }

    public class MatchSquadsData
    {
        [JsonPropertyName("match")]
        public Match Match { get; set; }
        
        [JsonPropertyName("home")]
        public TeamSquad Home { get; set; }
        
        [JsonPropertyName("away")]
        public TeamSquad Away { get; set; }
        
        // “previous” is an empty array in this sample.
        [JsonPropertyName("previous")]
        public List<object> Previous { get; set; }
        
        // The players dictionary maps a key (typically the player’s id as string) to an extended player object.
        [JsonPropertyName("players")]
        public Dictionary<string, ExtendedPlayer> Players { get; set; }
    }

    // The match object is similar to other endpoints.
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
        
        [JsonPropertyName("_dt")]
        public TimeInfo Dt { get; set; }
        
        [JsonPropertyName("round")]
        public int Round { get; set; }
        
        [JsonPropertyName("roundname")]
        public RoundName RoundName { get; set; }
        
        [JsonPropertyName("week")]
        public int Week { get; set; }
        
        [JsonPropertyName("coverage")]
        public Coverage Coverage { get; set; }
        
        [JsonPropertyName("result")]
        public Result Result { get; set; }
        
        [JsonPropertyName("periods")]
        public object Periods { get; set; }
        
        [JsonPropertyName("updated_uts")]
        public long UpdatedUts { get; set; }
        
        [JsonPropertyName("ended_uts")]
        public object EndedUts { get; set; }
        
        [JsonPropertyName("p")]
        public string P { get; set; }
        
        [JsonPropertyName("ptime")]
        public object Ptime { get; set; }
        
        [JsonPropertyName("timeinfo")]
        public TimeInfoInfo TimeInfo { get; set; }
        
        [JsonPropertyName("teams")]
        public MatchTeams Teams { get; set; }
        
        [JsonPropertyName("status")]
        public Status Status { get; set; }
        
        [JsonPropertyName("removed")]
        public bool Removed { get; set; }
        
        [JsonPropertyName("facts")]
        public bool Facts { get; set; }
        
        [JsonPropertyName("stadiumid")]
        public int StadiumId { get; set; }
        
        [JsonPropertyName("localderby")]
        public bool LocalDerby { get; set; }
        
        [JsonPropertyName("distance")]
        public int Distance { get; set; }
        
        // In this endpoint these are numeric codes.
        [JsonPropertyName("weather")]
        public int Weather { get; set; }
        
        [JsonPropertyName("pitchcondition")]
        public int PitchCondition { get; set; }
        
        [JsonPropertyName("temperature")]
        public object Temperature { get; set; }
        
        [JsonPropertyName("wind")]
        public object Wind { get; set; }
        
        [JsonPropertyName("windadvantage")]
        public int WindAdvantage { get; set; }
        
        [JsonPropertyName("matchstatus")]
        public string MatchStatus { get; set; }
        
        [JsonPropertyName("postponed")]
        public bool Postponed { get; set; }
        
        [JsonPropertyName("cancelled")]
        public bool Cancelled { get; set; }
        
        [JsonPropertyName("walkover")]
        public bool Walkover { get; set; }
        
        [JsonPropertyName("hf")]
        public double Hf { get; set; }
        
        [JsonPropertyName("periodlength")]
        public int PeriodLength { get; set; }
        
        [JsonPropertyName("numberofperiods")]
        public int NumberOfPeriods { get; set; }
        
        [JsonPropertyName("overtimelength")]
        public int OvertimeLength { get; set; }
        
        [JsonPropertyName("tobeannounced")]
        public bool ToBeAnnounced { get; set; }
        
        [JsonPropertyName("cards")]
        public Cards Cards { get; set; }
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
        public int TzOffset { get; set; }
        
        [JsonPropertyName("uts")]
        public long Uts { get; set; }
    }

    public class RoundName
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }
        
        [JsonPropertyName("_id")]
        public int Id { get; set; }
        
        [JsonPropertyName("name")]
        public object Name { get; set; }
    }

    public class Coverage
    {
        [JsonPropertyName("lineup")]
        public int Lineup { get; set; }
        
        [JsonPropertyName("formations")]
        public int Formations { get; set; }
        
        [JsonPropertyName("livetable")]
        public int Livetable { get; set; }
        
        [JsonPropertyName("injuries")]
        public int Injuries { get; set; }
        
        [JsonPropertyName("ballspotting")]
        public bool Ballspotting { get; set; }
        
        [JsonPropertyName("cornersonly")]
        public bool Cornersonly { get; set; }
        
        [JsonPropertyName("multicast")]
        public bool Multicast { get; set; }
        
        [JsonPropertyName("scoutmatch")]
        public int Scoutmatch { get; set; }
        
        [JsonPropertyName("scoutcoveragestatus")]
        public int Scoutcoveragestatus { get; set; }
        
        [JsonPropertyName("scoutconnected")]
        public bool Scoutconnected { get; set; }
        
        [JsonPropertyName("liveodds")]
        public bool Liveodds { get; set; }
        
        [JsonPropertyName("deepercoverage")]
        public bool Deepercoverage { get; set; }
        
        [JsonPropertyName("tacticallineup")]
        public bool Tacticallineup { get; set; }
        
        [JsonPropertyName("basiclineup")]
        public bool Basiclineup { get; set; }
        
        [JsonPropertyName("hasstats")]
        public bool Hasstats { get; set; }
        
        [JsonPropertyName("inlivescore")]
        public bool Inlivescore { get; set; }
        
        [JsonPropertyName("advantage")]
        public object Advantage { get; set; }
        
        [JsonPropertyName("tiebreak")]
        public object Tiebreak { get; set; }
        
        [JsonPropertyName("paperscorecard")]
        public object Paperscorecard { get; set; }
        
        [JsonPropertyName("insights")]
        public bool Insights { get; set; }
        
        [JsonPropertyName("penaltyshootout")]
        public int Penaltyshootout { get; set; }
        
        [JsonPropertyName("scouttest")]
        public bool Scouttest { get; set; }
        
        [JsonPropertyName("lmtsupport")]
        public int Lmtsupport { get; set; }
        
        [JsonPropertyName("venue")]
        public bool Venue { get; set; }
        
        [JsonPropertyName("matchdatacomplete")]
        public bool Matchdatacomplete { get; set; }
        
        [JsonPropertyName("mediacoverage")]
        public bool Mediacoverage { get; set; }
        
        [JsonPropertyName("substitutions")]
        public bool Substitutions { get; set; }
    }

    public class Result
    {
        [JsonPropertyName("home")]
        public object Home { get; set; }
        
        [JsonPropertyName("away")]
        public object Away { get; set; }
        
        [JsonPropertyName("winner")]
        public object Winner { get; set; }
    }

    public class TimeInfoInfo
    {
        [JsonPropertyName("injurytime")]
        public object Injurytime { get; set; }
        
        [JsonPropertyName("ended")]
        public object Ended { get; set; }
        
        [JsonPropertyName("started")]
        public object Started { get; set; }
        
        [JsonPropertyName("played")]
        public object Played { get; set; }
        
        [JsonPropertyName("remaining")]
        public object Remaining { get; set; }
        
        [JsonPropertyName("running")]
        public bool Running { get; set; }
    }

    public class MatchTeams
    {
        [JsonPropertyName("home")]
        public Team TeamHome { get; set; }
        
        [JsonPropertyName("away")]
        public Team TeamAway { get; set; }
    }

    public class Team
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }
        
        [JsonPropertyName("_id")]
        public int Id { get; set; }
        
        [JsonPropertyName("_sid")]
        public int Sid { get; set; }
        
        [JsonPropertyName("uid")]
        public int Uid { get; set; }
        
        [JsonPropertyName("virtual")]
        public bool Virtual { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("mediumname")]
        public string Mediumname { get; set; }
        
        [JsonPropertyName("abbr")]
        public string Abbr { get; set; }
        
        [JsonPropertyName("nickname")]
        public object Nickname { get; set; }
        
        [JsonPropertyName("iscountry")]
        public bool Iscountry { get; set; }
        
        [JsonPropertyName("haslogo")]
        public bool Haslogo { get; set; }
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

    public class Cards
    {
        [JsonPropertyName("home")]
        public CardCount Home { get; set; }
        
        [JsonPropertyName("away")]
        public CardCount Away { get; set; }
    }

    public class CardCount
    {
        [JsonPropertyName("yellow_count")]
        public int YellowCount { get; set; }
        
        [JsonPropertyName("red_count")]
        public int RedCount { get; set; }
    }

    // Models for each team’s squad information
    public class TeamSquad
    {
        [JsonPropertyName("startinglineup")]
        public StartingLineup StartingLineup { get; set; }
        
        [JsonPropertyName("substitutes")]
        public List<SubstitutePlayer> Substitutes { get; set; }
        
        [JsonPropertyName("manager")]
        public Manager Manager { get; set; }
    }

    public class StartingLineup
    {
        [JsonPropertyName("formation")]
        public string Formation { get; set; }
        
        [JsonPropertyName("players")]
        public List<StartingLineupPlayer> Players { get; set; }
    }

    public class StartingLineupPlayer
    {
        // Maps the JSON property "_type"
        [JsonPropertyName("_type")]
        public int Type { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("order")]
        public int Order { get; set; }
        
        [JsonPropertyName("matchpos")]
        public string MatchPos { get; set; }
        
        [JsonPropertyName("playerid")]
        public int PlayerId { get; set; }
        
        [JsonPropertyName("playername")]
        public string PlayerName { get; set; }
        
        [JsonPropertyName("pos")]
        public string Pos { get; set; }
        
        [JsonPropertyName("shirtnumber")]
        public string ShirtNumber { get; set; }
    }

    public class SubstitutePlayer
    {
        [JsonPropertyName("playerid")]
        public int PlayerId { get; set; }
        
        [JsonPropertyName("playername")]
        public string PlayerName { get; set; }
        
        [JsonPropertyName("pos")]
        public string Pos { get; set; }
        
        [JsonPropertyName("shirtnumber")]
        public string ShirtNumber { get; set; }
    }

    // Manager for a squad – note the properties are similar to a simplified ExtendedPlayer.
    public class Manager
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
        
        [JsonPropertyName("primarypositiontype")]
        public object PrimaryPositionType { get; set; }
        
        [JsonPropertyName("haslogo")]
        public bool HasLogo { get; set; }
    }

    // Extended player data (detailed player information)
    public class ExtendedPlayer
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
        
        // Some players may include a secondary nationality.
        [JsonPropertyName("secondarynationality")]
        public CountryCode SecondaryNationality { get; set; }
        
        [JsonPropertyName("position")]
        public Position Position { get; set; }
        
        [JsonPropertyName("primarypositiontype")]
        public object PrimaryPositionType { get; set; }
        
        [JsonPropertyName("haslogo")]
        public bool HasLogo { get; set; }
        
        [JsonPropertyName("height")]
        public int? Height { get; set; }
        
        [JsonPropertyName("weight")]
        public int? Weight { get; set; }
        
        [JsonPropertyName("birthcountry")]
        public CountryCode BirthCountry { get; set; }
        
        // Some JSON uses "_foot" to indicate the preferred foot.
        [JsonPropertyName("_foot")]
        public string FootUnderscore { get; set; }
        
        [JsonPropertyName("foot")]
        public string Foot { get; set; }
        
        [JsonPropertyName("birthplace")]
        public string Birthplace { get; set; }
        
        [JsonPropertyName("twitter")]
        public string Twitter { get; set; }
        
        [JsonPropertyName("facebook")]
        public string Facebook { get; set; }
        
        [JsonPropertyName("nickname")]
        public string Nickname { get; set; }
        
        [JsonPropertyName("marketvalue")]
        public int? MarketValue { get; set; }
    }

    // Country code information
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

    // Position information for a player
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
}
