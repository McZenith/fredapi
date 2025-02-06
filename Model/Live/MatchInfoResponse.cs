using System.Text.Json.Serialization;

namespace fredapi.Model.Live.MatchInfoResponse
{
    // Root object
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
        public Player Referee { get; set; }

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

        // The structure of "periods" is not provided; using object.
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

        // Renamed to avoid conflict with System.Time
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

        [JsonPropertyName("homerealcategoryid")]
        public int HomeRealCategoryId { get; set; }

        [JsonPropertyName("countrycode")]
        public CountryCode CountryCode { get; set; }
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

        // Note: in the JSON "_id" is a string.
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
        public string State { get; set; }

        [JsonPropertyName("cc")]
        public CountryCode Cc { get; set; }

        [JsonPropertyName("capacity")]
        public string Capacity { get; set; }

        [JsonPropertyName("hometeams")]
        public List<UniqueTeam> HomeTeams { get; set; }

        [JsonPropertyName("constryear")]
        public string ConstrYear { get; set; }

        [JsonPropertyName("googlecoords")]
        public string GoogleCoords { get; set; }

        [JsonPropertyName("pitchsize")]
        public PitchSize PitchSize { get; set; }
    }

    public class UniqueTeam
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
    }

    public class PitchSize
    {
        [JsonPropertyName("x")]
        public int X { get; set; }

        [JsonPropertyName("y")]
        public int Y { get; set; }
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

        [JsonPropertyName("cc")]
        public CountryCode Cc { get; set; }
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
        public Time Start { get; set; }

        [JsonPropertyName("end")]
        public Time End { get; set; }

        [JsonPropertyName("neutralground")]
        public bool NeutralGround { get; set; }

        [JsonPropertyName("friendly")]
        public bool Friendly { get; set; }

        [JsonPropertyName("currentseasonid")]
        public int CurrentSeasonId { get; set; }

        [JsonPropertyName("year")]
        public string Year { get; set; }
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

        // Some players (e.g. referee) may have null birthdate.
        [JsonPropertyName("birthdate")]
        public Time Birthdate { get; set; }

        [JsonPropertyName("nationality")]
        public CountryCode Nationality { get; set; }

        [JsonPropertyName("primarypositiontype")]
        public object PrimaryPositionType { get; set; }

        [JsonPropertyName("haslogo")]
        public bool HasLogo { get; set; }
    }

    public class Manager
    {
        [JsonPropertyName("home")]
        public Player Home { get; set; }

        [JsonPropertyName("away")]
        public Player Away { get; set; }
    }

    public class Jerseys
    {
        [JsonPropertyName("home")]
        public JerseysTeam Home { get; set; }

        [JsonPropertyName("away")]
        public JerseysTeam Away { get; set; }
    }

    public class JerseysTeam
    {
        [JsonPropertyName("player")]
        public Jersey Player { get; set; }

        [JsonPropertyName("GK")]
        public Jersey GK { get; set; }
    }

    public class Jersey
    {
        [JsonPropertyName("base")]
        public string Base { get; set; }

        [JsonPropertyName("sleeve")]
        public string Sleeve { get; set; }

        [JsonPropertyName("number")]
        public string Number { get; set; }

        // This property may be absent (as for some GK entries)
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
        public bool OverUnderHalfTime { get; set; }

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
        public bool LiveScoreEventPossession { get; set; }

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
