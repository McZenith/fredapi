using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace fredapi.Model.Live.StatsSeasonUniqueTeamStatsResponse
{
    public class StatsSeasonUniqueTeamStatsResponse
    {
        [JsonPropertyName("queryUrl")]
        public string QueryUrl { get; set; }

        [JsonPropertyName("doc")]
        public List<StatsSeasonUniqueTeamStatsDoc> Doc { get; set; }
    }

    public class StatsSeasonUniqueTeamStatsDoc
    {
        [JsonPropertyName("event")]
        public string Event { get; set; }

        [JsonPropertyName("_dob")]
        public long Dob { get; set; }

        [JsonPropertyName("_maxage")]
        public int MaxAge { get; set; }

        [JsonPropertyName("data")]
        public StatsSeasonUniqueTeamStatsData Data { get; set; }
    }

    public class StatsSeasonUniqueTeamStatsData
    {
        [JsonPropertyName("season")]
        public Season Season { get; set; }

        [JsonPropertyName("stats")]
        public Stats Stats { get; set; }
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

    public class Stats
    {
        [JsonPropertyName("uniqueteams")]
        public Dictionary<string, UniqueTeamStat> Uniqueteams { get; set; }
    }

    public class UniqueTeamStat
    {
        [JsonPropertyName("uniqueteam")]
        public UniqueTeam Uniqueteam { get; set; }

        [JsonPropertyName("goal_attempts")]
        public StatDetailNumber GoalAttempts { get; set; }

        [JsonPropertyName("shots_on_goal")]
        public StatDetailNumber ShotsOnGoal { get; set; }

        [JsonPropertyName("shots_off_goal")]
        public StatDetailNumber ShotsOffGoal { get; set; }

        [JsonPropertyName("corner_kicks")]
        public StatDetailNumber CornerKicks { get; set; }

        [JsonPropertyName("ball_possession")]
        public StatDetailNumber BallPossession { get; set; }

        [JsonPropertyName("shots_blocked")]
        public StatDetailNumber ShotsBlocked { get; set; }

        [JsonPropertyName("cards_given")]
        public StatDetailNumber CardsGiven { get; set; }

        [JsonPropertyName("freekicks")]
        public StatDetailNumber Freekicks { get; set; }

        [JsonPropertyName("offside")]
        public StatDetailNumber Offside { get; set; }

        [JsonPropertyName("shots_on_post")]
        public StatDetailNumber ShotsOnPost { get; set; }

        [JsonPropertyName("shots_on_bar")]
        public StatDetailNumber ShotsOnBar { get; set; }

        [JsonPropertyName("goals_by_foot")]
        public StatDetailNumber GoalsByFoot { get; set; }

        [JsonPropertyName("goals_by_head")]
        public StatDetailNumber GoalsByHead { get; set; }

        [JsonPropertyName("attendance")]
        public StatDetailNumber Attendance { get; set; }

        [JsonPropertyName("yellow_cards")]
        public StatDetailNumber YellowCards { get; set; }

        [JsonPropertyName("red_cards")]
        public StatDetailNumber RedCards { get; set; }

        [JsonPropertyName("goals_scored")]
        public StatDetailNumber GoalsScored { get; set; }

        [JsonPropertyName("goals_conceded")]
        public StatDetailNumber GoalsConceded { get; set; }

        [JsonPropertyName("yellowred_cards")]
        public StatDetailNumber YellowRedCards { get; set; }

        [JsonPropertyName("shootingefficiency")]
        public StatDetailString ShootingEfficiency { get; set; }

        [JsonPropertyName("late_winning_goals")]
        public StatSimple LateWinningGoals { get; set; }

        [JsonPropertyName("penalty_success_count")]
        public StatSimple PenaltySuccessCount { get; set; }

        [JsonPropertyName("penalty_fail_count")]
        public StatSimple PenaltyFailCount { get; set; }
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

    public class StatDetailNumber
    {
        [JsonPropertyName("average")]
        public double Average { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("matches")]
        public int Matches { get; set; }
    }

    public class StatDetailString
    {
        [JsonPropertyName("average")]
        public double Average { get; set; }

        [JsonPropertyName("total")]
        public string Total { get; set; }

        [JsonPropertyName("matches")]
        public int Matches { get; set; }
    }

    public class StatSimple
    {
        [JsonPropertyName("total")]
        public string Total { get; set; }
    }
}
