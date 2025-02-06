using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace fredapi.Model.Historical.StatsSeasonMeta
{
    public class StatsSeasonMetaResponse
    {
        [JsonPropertyName("queryUrl")]
        public string QueryUrl { get; set; }

        [JsonPropertyName("doc")]
        public List<StatsSeasonMetaDoc> Doc { get; set; }
    }

    public class StatsSeasonMetaDoc
    {
        [JsonPropertyName("event")]
        public string Event { get; set; }

        [JsonPropertyName("_dob")]
        public long Dob { get; set; }

        [JsonPropertyName("_maxage")]
        public int MaxAge { get; set; }

        [JsonPropertyName("data")]
        public StatsSeasonMetaData Data { get; set; }
    }

    public class StatsSeasonMetaData
    {
        [JsonPropertyName("season")]
        public Season Season { get; set; }

        [JsonPropertyName("sport")]
        public Sport Sport { get; set; }

        [JsonPropertyName("realcategory")]
        public RealCategory RealCategory { get; set; }

        [JsonPropertyName("tournamentids")]
        public List<int> TournamentIds { get; set; }

        [JsonPropertyName("tableids")]
        public List<int> TableIds { get; set; }

        [JsonPropertyName("cupids")]
        public List<object> CupIds { get; set; }

        [JsonPropertyName("uniquetournament")]
        public UniqueTournament UniqueTournament { get; set; }

        [JsonPropertyName("statscoverage")]
        public StatsCoverage StatsCoverage { get; set; }
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

        [JsonPropertyName("coverage")]
        public SeasonCoverage Coverage { get; set; }

        [JsonPropertyName("h2hdefault")]
        public H2HDefault H2HDefault { get; set; }
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

    public class SeasonCoverage
    {
        [JsonPropertyName("stats_creation_table")]
        public CoverageItem StatsCreationTable { get; set; }

        [JsonPropertyName("stats_creation_cup")]
        public CoverageItem StatsCreationCup { get; set; }

        [JsonPropertyName("live")]
        public CoverageItem Live { get; set; }

        [JsonPropertyName("match_creation")]
        public CoverageItem MatchCreation { get; set; }

        [JsonPropertyName("player_service")]
        public CoverageItem PlayerService { get; set; }

        [JsonPropertyName("resulting")]
        public CoverageItem Resulting { get; set; }

        [JsonPropertyName("lineups")]
        public bool Lineups { get; set; }
    }

    public class CoverageItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("level")]
        public LevelInfo Level { get; set; }
    }

    public class LevelInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("value")]
        public int Value { get; set; }
    }

    public class H2HDefault
    {
        [JsonPropertyName("matchid")]
        public int MatchId { get; set; }

        [JsonPropertyName("teamidhome")]
        public int TeamIdHome { get; set; }

        [JsonPropertyName("teamidaway")]
        public int TeamIdAway { get; set; }
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
        public int RcId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("cc")]
        public CountryCode CC { get; set; }
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

    public class UniqueTournament
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }

        [JsonPropertyName("_id")]
        public int Id { get; set; }

        [JsonPropertyName("_utid")]
        public int Utid { get; set; }

        [JsonPropertyName("_sid")]
        public int Sid { get; set; }

        [JsonPropertyName("_rcid")]
        public int RcId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("currentseason")]
        public int CurrentSeason { get; set; }

        [JsonPropertyName("friendly")]
        public bool Friendly { get; set; }
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
        public bool LiveScoreEventGoalkeeperSave { get; set; }

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
