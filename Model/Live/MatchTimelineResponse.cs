using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace fredapi.Model.Live.MatchTimelineResponse
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

        [JsonPropertyName("events")]
        public List<TimelineEvent> Events { get; set; }
    }

    #region Match Information

    // The match object is similar to the one in your "match_info" endpoint.
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
        public Time Dt { get; set; }

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
        public bool EndedUts { get; set; }

        [JsonPropertyName("p")]
        public string P { get; set; }

        [JsonPropertyName("ptime")]
        public long Ptime { get; set; }

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
        public int StadiumId { get; set; }

        [JsonPropertyName("localderby")]
        public bool LocalDerby { get; set; }

        [JsonPropertyName("distance")]
        public int Distance { get; set; }

        [JsonPropertyName("weather")]
        public int Weather { get; set; }

        [JsonPropertyName("pitchcondition")]
        public int PitchCondition { get; set; }

        [JsonPropertyName("temperature")]
        public int? Temperature { get; set; }

        [JsonPropertyName("wind")]
        public int? Wind { get; set; }

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

    public class Time
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }

        [JsonPropertyName("time")]
        public string TimeValue { get; set; }

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

        // In this JSON sample the "name" is numeric.
        [JsonPropertyName("name")]
        public int Name { get; set; }
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

        [JsonPropertyName("advantage")]
        public object Advantage { get; set; }

        [JsonPropertyName("tiebreak")]
        public object Tiebreak { get; set; }

        [JsonPropertyName("paperscorecard")]
        public object PaperScoreCard { get; set; }

        [JsonPropertyName("insights")]
        public bool Insights { get; set; }

        [JsonPropertyName("penaltyshootout")]
        public int PenaltyShootout { get; set; }

        [JsonPropertyName("scouttest")]
        public bool ScoutTest { get; set; }

        [JsonPropertyName("lmtsupport")]
        public int LmtSupport { get; set; }

        [JsonPropertyName("venue")]
        public bool Venue { get; set; }

        [JsonPropertyName("matchdatacomplete")]
        public bool MatchDataComplete { get; set; }

        [JsonPropertyName("mediacoverage")]
        public bool MediaCoverage { get; set; }

        [JsonPropertyName("substitutions")]
        public bool Substitutions { get; set; }
    }

    public class Result
    {
        [JsonPropertyName("home")]
        public int Home { get; set; }

        [JsonPropertyName("away")]
        public int Away { get; set; }

        [JsonPropertyName("winner")]
        public int? Winner { get; set; }
    }

    public class TimeInfo
    {
        [JsonPropertyName("injurytime")]
        public int? InjuryTime { get; set; }

        [JsonPropertyName("ended")]
        public int? Ended { get; set; }

        [JsonPropertyName("started")]
        public int? Started { get; set; }

        [JsonPropertyName("played")]
        public int? Played { get; set; }

        [JsonPropertyName("remaining")]
        public int? Remaining { get; set; }

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

    public class Cards
    {
        [JsonPropertyName("home")]
        public CardDetails Home { get; set; }

        [JsonPropertyName("away")]
        public CardDetails Away { get; set; }
    }

    public class CardDetails
    {
        [JsonPropertyName("yellow_count")]
        public int YellowCount { get; set; }

        [JsonPropertyName("red_count")]
        public int RedCount { get; set; }
    }

    #endregion

    #region Timeline Events

    public class TimelineEvent
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }

        [JsonPropertyName("_id")]
        public long Id { get; set; }

        [JsonPropertyName("_scoutid")]
        public string ScoutId { get; set; }

        [JsonPropertyName("_sid")]
        public int Sid { get; set; }

        [JsonPropertyName("_rcid")]
        public int Rcid { get; set; }

        [JsonPropertyName("_tid")]
        public int Tid { get; set; }

        [JsonPropertyName("_dc")]
        public bool Dc { get; set; }

        [JsonPropertyName("_typeid")]
        public string TypeId { get; set; }

        [JsonPropertyName("uts")]
        public long Uts { get; set; }

        [JsonPropertyName("updated_uts")]
        public long UpdatedUts { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("matchid")]
        public int MatchId { get; set; }

        [JsonPropertyName("disabled")]
        public int Disabled { get; set; }

        [JsonPropertyName("time")]
        public int Time { get; set; }

        [JsonPropertyName("seconds")]
        public int Seconds { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("injurytime")]
        public int InjuryTime { get; set; }

        [JsonPropertyName("team")]
        public string Team { get; set; }

        // Optional properties

        [JsonPropertyName("params")]
        public Dictionary<string, string> Params { get; set; }

        [JsonPropertyName("player")]
        public Player Player { get; set; }

        [JsonPropertyName("X")]
        public int? X { get; set; }

        [JsonPropertyName("Y")]
        public int? Y { get; set; }

        [JsonPropertyName("side")]
        public string Side { get; set; }

        [JsonPropertyName("sideid")]
        public string SideId { get; set; }

        [JsonPropertyName("result")]
        public Result Result { get; set; }

        [JsonPropertyName("status")]
        public Status Status { get; set; }

        [JsonPropertyName("period")]
        public int? Period { get; set; }

        [JsonPropertyName("coordinates")]
        public List<Coordinate> Coordinates { get; set; }

        [JsonPropertyName("situation")]
        public string Situation { get; set; }
    }

    public class Coordinate
    {
        [JsonPropertyName("team")]
        public string Team { get; set; }

        [JsonPropertyName("X")]
        public int X { get; set; }

        [JsonPropertyName("Y")]
        public int Y { get; set; }
    }

    #endregion

    #region Player & Related Classes

    // This Player model is re-used for timeline events. It covers the full details
    // as seen in some events (e.g. for a shot off target) but will also work
    // when only a minimal player object (with just a "name") is provided.
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
        public Time Birthdate { get; set; }

        [JsonPropertyName("nationality")]
        public CountryCode Nationality { get; set; }

        [JsonPropertyName("position")]
        public Position Position { get; set; }

        [JsonPropertyName("primarypositiontype")]
        public object PrimaryPositionType { get; set; }

        [JsonPropertyName("haslogo")]
        public bool HasLogo { get; set; }
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

    #endregion
}
