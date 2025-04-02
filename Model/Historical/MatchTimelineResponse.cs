using System.Text.Json.Serialization;

namespace fredapi.Model.Historical.MatchTimeline
{
    // Root object
    public class MatchTimelineResponse
    {
        [JsonPropertyName("queryUrl")]
        public string QueryUrl { get; set; }
        
        [JsonPropertyName("doc")]
        public List<MatchTimelineDoc> Doc { get; set; }
    }

    public class MatchTimelineDoc
    {
        [JsonPropertyName("event")]
        public string Event { get; set; }
        
        [JsonPropertyName("_dob")]
        public long Dob { get; set; }
        
        [JsonPropertyName("_maxage")]
        public int MaxAge { get; set; }
        
        [JsonPropertyName("data")]
        public MatchTimelineData Data { get; set; }
    }

    public class MatchTimelineData
    {
        [JsonPropertyName("match")]
        public Match Match { get; set; }
        
        // The events array is empty in the sample; define properties as needed.
        [JsonPropertyName("events")]
        public List<MatchTimelineEvent> Events { get; set; }
    }

    // You can expand this class when you know the structure of timeline events.
    public class MatchTimelineEvent
    {
        // Example placeholder properties:
        // [JsonPropertyName("time")]
        // public string Time { get; set; }
        // [JsonPropertyName("type")]
        // public string Type { get; set; }
        // [JsonPropertyName("description")]
        // public string Description { get; set; }
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
        public Teams Teams { get; set; }
        
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
        
        [JsonPropertyName("weather")]
        public object Weather { get; set; }
        
        [JsonPropertyName("pitchcondition")]
        public object PitchCondition { get; set; }
        
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
        
        // In the sample, "name" is a number (25); you may choose object or a more specific type.
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

    public class Teams
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
}
