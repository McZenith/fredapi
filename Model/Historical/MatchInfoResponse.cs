using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace fredapi.Model.Historical.MatchInfo
{
    public class MatchInfoResponse
    {
        [JsonPropertyName("queryUrl")]
        public string QueryUrl { get; set; }
        
        [JsonPropertyName("doc")]
        public List<MatchInfoDoc> Doc { get; set; }
    }

    public class MatchInfoDoc
    {
        [JsonPropertyName("event")]
        public string Event { get; set; }
        
        [JsonPropertyName("_dob")]
        public long Dob { get; set; }
        
        [JsonPropertyName("_maxage")]
        public int MaxAge { get; set; }
        
        [JsonPropertyName("data")]
        public MatchInfoData Data { get; set; }
    }

    public class MatchInfoData
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }
        
        [JsonPropertyName("match")]
        public Match Match { get; set; }
        
        [JsonPropertyName("cities")]
        public Cities Cities { get; set; }
        
        [JsonPropertyName("stadium")]
        public Stadium Stadium { get; set; }
        
        [JsonPropertyName("tournament")]
        public Tournament Tournament { get; set; }
        
        [JsonPropertyName("sport")]
        public Sport Sport { get; set; }
        
        [JsonPropertyName("realcategory")]
        public RealCategory RealCategory { get; set; }
        
        [JsonPropertyName("season")]
        public Season Season { get; set; }
        
        [JsonPropertyName("referee")]
        public Referee Referee { get; set; }
        
        [JsonPropertyName("manager")]
        public Manager Manager { get; set; }
        
        [JsonPropertyName("jerseys")]
        public Jerseys Jerseys { get; set; }
        
        [JsonPropertyName("statscoverage")]
        public StatsCoverage StatsCoverage { get; set; }
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
        
        // Note: In this API, "ended_uts" is not available so we use object.
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
        
        // "status" is not available; this property has been removed.
        
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
        public object Home { get; set; }
        
        [JsonPropertyName("away")]
        public object Away { get; set; }
        
        [JsonPropertyName("winner")]
        public object Winner { get; set; }
    }

    public class TimeInfoInfo
    {
        [JsonPropertyName("injurytime")]
        public object InjuryTime { get; set; }
        
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
        public object Nickname { get; set; }
        
        [JsonPropertyName("iscountry")]
        public bool IsCountry { get; set; }
        
        [JsonPropertyName("haslogo")]
        public bool HasLogo { get; set; }
        
        [JsonPropertyName("homerealcategoryid")]
        public int HomeRealCategoryId { get; set; }
        // CountryCode removed because it is not available.
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

    public class Cities
    {
        [JsonPropertyName("home")]
        public City Home { get; set; }
        
        [JsonPropertyName("away")]
        public City Away { get; set; }
    }

    public class City
    {
        [JsonPropertyName("_id")]
        public int Id { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
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
        
        // The "cc" (country code) property is omitted.
        
        [JsonPropertyName("capacity")]
        public string Capacity { get; set; }
        
        [JsonPropertyName("hometeams")]
        public List<Uniqueteam> HomeTeams { get; set; }
        
        [JsonPropertyName("constryear")]
        public string ConstrYear { get; set; }
        
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

    public class Uniqueteam
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
        public int LiveTable { get; set; }
        
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
        // The "cc" property has been removed.
    }

    public class Season
    {
        [JsonPropertyName("_id")]
        public string Id { get; set; }
        
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }
        
        [JsonPropertyName("_utid")]
        public int Utid { get; set; }
        
        [JsonPropertyName("_sid")]
        public int Sid { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("abbr")]
        public string Abbr { get; set; }
        
        [JsonPropertyName("start")]
        public TimeInfo Start { get; set; }
        
        [JsonPropertyName("end")]
        public TimeInfo End { get; set; }
        
        [JsonPropertyName("neutralground")]
        public bool NeutralGround { get; set; }
        
        [JsonPropertyName("friendly")]
        public bool Friendly { get; set; }
        
        [JsonPropertyName("currentseasonid")]
        public int CurrentSeasonId { get; set; }
        
        [JsonPropertyName("year")]
        public string Year { get; set; }
    }

    public class Referee
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }
        
        [JsonPropertyName("_id")]
        public int Id { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        // Birthdate is null in sample.
        [JsonPropertyName("birthdate")]
        public object Birthdate { get; set; }
        
        // Nationality information is not available.
    }

    public class Manager
    {
        [JsonPropertyName("home")]
        public ManagerInfo Home { get; set; }
        
        [JsonPropertyName("away")]
        public ManagerInfo Away { get; set; }
    }

    public class ManagerInfo
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
        
        // Nationality not available.
        
        [JsonPropertyName("primarypositiontype")]
        public object PrimaryPositionType { get; set; }
        
        [JsonPropertyName("haslogo")]
        public bool HasLogo { get; set; }
    }

    public class Jerseys
    {
        [JsonPropertyName("home")]
        public JerseySet Home { get; set; }
        
        [JsonPropertyName("away")]
        public JerseySet Away { get; set; }
    }

    public class JerseySet
    {
        [JsonPropertyName("player")]
        public JerseyDetails Player { get; set; }
        
        [JsonPropertyName("GK")]
        public JerseyDetails GK { get; set; }
    }

    public class JerseyDetails
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
        
        [JsonPropertyName("real")]
        public bool Real { get; set; }
    }

    public class StatsCoverage
    {
        [JsonPropertyName("complexstat")]
        public bool ComplexStat { get; set; }
        
        [JsonPropertyName("livetable")]
        public bool LiveTable { get; set; }
        
        [JsonPropertyName("halftimetable")]
        public bool HalfTimeTable { get; set; }
        
        [JsonPropertyName("overunder")]
        public bool OverUnder { get; set; }
        
        [JsonPropertyName("overunderhalftime")]
        public bool OverUnderHalftime { get; set; }
        
        [JsonPropertyName("fixtures")]
        public bool Fixtures { get; set; }
        
        [JsonPropertyName("leaguetable")]
        public bool LeagueTable { get; set; }
        
        [JsonPropertyName("headtohead")]
        public bool HeadToHead { get; set; }
        
        [JsonPropertyName("formtable")]
        public bool FormTable { get; set; }
        
        [JsonPropertyName("secondhalftables")]
        public bool SecondHalfTables { get; set; }
        
        [JsonPropertyName("divisionview")]
        public bool DivisionView { get; set; }
        
        [JsonPropertyName("matchdetails")]
        public bool MatchDetails { get; set; }
        
        [JsonPropertyName("lineups")]
        public bool Lineups { get; set; }
        
        [JsonPropertyName("formations")]
        public bool Formations { get; set; }
        
        [JsonPropertyName("topgoals")]
        public bool TopGoals { get; set; }
        
        [JsonPropertyName("topassists")]
        public bool TopAssists { get; set; }
        
        [JsonPropertyName("disciplinary")]
        public bool Disciplinary { get; set; }
        
        [JsonPropertyName("redcards")]
        public bool RedCards { get; set; }
        
        [JsonPropertyName("yellowcards")]
        public bool YellowCards { get; set; }
        
        [JsonPropertyName("goalminute")]
        public bool GoalMinute { get; set; }
        
        [JsonPropertyName("goalminscorer")]
        public bool GoalMinScorer { get; set; }
        
        [JsonPropertyName("substitutions")]
        public bool Substitutions { get; set; }
        
        [JsonPropertyName("squadservice")]
        public bool SquadService { get; set; }
        
        [JsonPropertyName("livescoreeventthrowin")]
        public bool LiveScoreEventThrowIn { get; set; }
        
        [JsonPropertyName("livescoreeventgoalkick")]
        public bool LiveScoreEventGoalKick { get; set; }
        
        [JsonPropertyName("livescoreeventfreekick")]
        public bool LiveScoreEventFreeKick { get; set; }
        
        [JsonPropertyName("livescoreeventshotsoffgoal")]
        public bool LiveScoreEventShotsOffGoal { get; set; }
        
        [JsonPropertyName("livescoreeventshotsongoal")]
        public bool LiveScoreEventShotsOnGoal { get; set; }
        
        [JsonPropertyName("livescoreeventgoalkeepersave")]
        public bool LiveScoreEventGoalKeeperSave { get; set; }
        
        [JsonPropertyName("livescoreeventcornerkick")]
        public bool LiveScoreEventCornerKick { get; set; }
        
        [JsonPropertyName("livescoreeventoffside")]
        public bool LiveScoreEventOffside { get; set; }
        
        [JsonPropertyName("livescoreeventfouls")]
        public bool LiveScoreEventFouls { get; set; }
        
        [JsonPropertyName("livescoreeventpossesion")]
        public bool LiveScoreEventPossesion { get; set; }
        
        [JsonPropertyName("referee")]
        public bool Referee { get; set; }
        
        [JsonPropertyName("stadium")]
        public bool Stadium { get; set; }
        
        [JsonPropertyName("cuproster")]
        public bool CupRoster { get; set; }
        
        [JsonPropertyName("staffmanagers")]
        public bool StaffManagers { get; set; }
        
        [JsonPropertyName("staffteamofficials")]
        public bool StaffTeamOfficials { get; set; }
        
        [JsonPropertyName("staffassistantcoaches")]
        public bool StaffAssistantCoaches { get; set; }
        
        [JsonPropertyName("jerseys")]
        public bool Jerseys { get; set; }
        
        [JsonPropertyName("goalscorer")]
        public bool GoalScorer { get; set; }
        
        [JsonPropertyName("deepercoverage")]
        public bool DeeperCoverage { get; set; }
        
        [JsonPropertyName("tablerules")]
        public bool TableRules { get; set; }
    }
}
