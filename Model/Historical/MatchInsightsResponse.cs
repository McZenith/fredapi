using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace fredapi.Model.Historical.MatchInsightsResponse
{
    public class MatchInsightsResponse
    {
        [JsonPropertyName("queryUrl")]
        public string QueryUrl { get; set; }

        [JsonPropertyName("doc")]
        public List<Doc> Doc { get; set; }
    }

    public class Doc
    {
        [JsonPropertyName("event")]
        public string Event { get; set; }

        [JsonPropertyName("_dob")]
        public long Dob { get; set; }

        [JsonPropertyName("_maxage")]
        public int MaxAge { get; set; }

        [JsonPropertyName("data")]
        public InsightsData Data { get; set; }
    }

    public class InsightsData
    {
        [JsonPropertyName("match")]
        public Match Match { get; set; }

        // The "insights" property is an empty array in the sample.
        // You can define a proper Insight class if more details become available.
        [JsonPropertyName("insights")]
        public List<object> Insights { get; set; }
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

        // The match datetime details (named _dt in the JSON)
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
        public Periods Periods { get; set; }

        [JsonPropertyName("updated_uts")]
        public int UpdatedUts { get; set; }

        [JsonPropertyName("ended_uts")]
        public int EndedUts { get; set; }

        // The property "p" is a string in the JSON.
        [JsonPropertyName("p")]
        public string P { get; set; }

        [JsonPropertyName("ptime")]
        public int PTime { get; set; }

        [JsonPropertyName("timeinfo")]
        public TimeInfoDetail TimeInfo { get; set; }

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
        public int Uts { get; set; }
    }

    public class RoundName
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }

        [JsonPropertyName("_id")]
        public int Id { get; set; }

        // In this JSON, "name" is a number (e.g. 17)
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
        public object PaperScorecard { get; set; }

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

        [JsonPropertyName("player_service")]
        public CoverageService PlayerService { get; set; }

        [JsonPropertyName("live")]
        public CoverageService Live { get; set; }

        [JsonPropertyName("stats_creation_table")]
        public CoverageService StatsCreationTable { get; set; }

        [JsonPropertyName("resulting")]
        public CoverageService Resulting { get; set; }

        [JsonPropertyName("stats_creation_cup")]
        public CoverageService StatsCreationCup { get; set; }

        [JsonPropertyName("match_creation")]
        public CoverageService MatchCreation { get; set; }
    }

    public class CoverageService
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("level")]
        public LevelDetail Level { get; set; }
    }

    public class LevelDetail
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("value")]
        public int Value { get; set; }
    }

    public class Result
    {
        [JsonPropertyName("home")]
        public int Home { get; set; }

        [JsonPropertyName("away")]
        public int Away { get; set; }

        [JsonPropertyName("winner")]
        public string Winner { get; set; }
    }

    public class Periods
    {
        [JsonPropertyName("p1")]
        public Score P1 { get; set; }

        [JsonPropertyName("ft")]
        public Score FT { get; set; }
    }

    public class Score
    {
        [JsonPropertyName("home")]
        public int Home { get; set; }

        [JsonPropertyName("away")]
        public int Away { get; set; }
    }

    public class TimeInfoDetail
    {
        [JsonPropertyName("injurytime")]
        public object InjuryTime { get; set; }

        [JsonPropertyName("ended")]
        public string Ended { get; set; }

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
        public TeamDetail Home { get; set; }

        [JsonPropertyName("away")]
        public TeamDetail Away { get; set; }
    }

    public class TeamDetail
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
