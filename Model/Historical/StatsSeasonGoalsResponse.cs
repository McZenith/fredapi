using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace fredapi.Model.Historical.StatsSeasonGoals
{
    public class StatsSeasonGoalsResponse
    {
        [JsonPropertyName("queryUrl")]
        public string QueryUrl { get; set; }

        [JsonPropertyName("doc")]
        public List<StatsSeasonGoalsDoc> Doc { get; set; }
    }

    public class StatsSeasonGoalsDoc
    {
        [JsonPropertyName("event")]
        public string Event { get; set; }

        [JsonPropertyName("_dob")]
        public long Dob { get; set; }

        [JsonPropertyName("_maxage")]
        public int MaxAge { get; set; }

        [JsonPropertyName("data")]
        public StatsSeasonGoalsData Data { get; set; }
    }

    public class StatsSeasonGoalsData
    {
        [JsonPropertyName("season")]
        public Season Season { get; set; }

        [JsonPropertyName("tables")]
        public List<StatisticsTable> Tables { get; set; }

        // Although "cups" is an empty array in the sample JSON, you may define a Cup class if needed.
        [JsonPropertyName("cups")]
        public List<object> Cups { get; set; }

        [JsonPropertyName("teams")]
        public List<TeamGoals> Teams { get; set; }

        [JsonPropertyName("totals")]
        public Totals Totals { get; set; }
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

    public class StatisticsTable
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }

        [JsonPropertyName("_id")]
        public string Id { get; set; }

        [JsonPropertyName("tournamentid")]
        public int TournamentId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("abbr")]
        public string Abbr { get; set; }

        [JsonPropertyName("groupname")]
        public string GroupName { get; set; }

        // Note: maxrounds appears as a string ("42") in the sample.
        [JsonPropertyName("maxrounds")]
        public string MaxRounds { get; set; }

        [JsonPropertyName("seasonid")]
        public string SeasonId { get; set; }

        [JsonPropertyName("seasontype")]
        public string SeasonType { get; set; }

        [JsonPropertyName("seasontypename")]
        public string SeasonTypeName { get; set; }

        [JsonPropertyName("seasontypeunique")]
        public string SeasonTypeUnique { get; set; }

        [JsonPropertyName("start")]
        public TimeInfo Start { get; set; }

        [JsonPropertyName("end")]
        public TimeInfo End { get; set; }

        [JsonPropertyName("roundbyround")]
        public bool RoundByRound { get; set; }

        // "order" is null in the sample; you can change the type if needed.
        [JsonPropertyName("order")]
        public object Order { get; set; }
    }

    public class TeamGoals
    {
        [JsonPropertyName("team")]
        public Team Team { get; set; }

        /// <summary>
        /// Goals scored in different time slices. The keys are the time ranges (e.g., "0-15", "16-30", …).
        /// </summary>
        [JsonPropertyName("scored")]
        public Dictionary<string, int> Scored { get; set; }

        [JsonPropertyName("scoredsum")]
        public int ScoredSum { get; set; }

        /// <summary>
        /// Goals conceded in different time slices.
        /// </summary>
        [JsonPropertyName("conceded")]
        public Dictionary<string, int> Conceded { get; set; }

        [JsonPropertyName("concededsum")]
        public int ConcededSum { get; set; }

        [JsonPropertyName("firstgoal")]
        public int FirstGoal { get; set; }

        [JsonPropertyName("lastgoal")]
        public int LastGoal { get; set; }

        [JsonPropertyName("average_time_first_goal")]
        public double AverageTimeFirstGoal { get; set; }

        [JsonPropertyName("matches")]
        public int Matches { get; set; }

        // The sample JSON provides penalty counts as quoted numbers.
        [JsonPropertyName("penalty_success_count")]
        public string PenaltySuccessCount { get; set; }

        // This property may not be present for every team.
        [JsonPropertyName("penalty_fail_count")]
        public string PenaltyFailCount { get; set; }
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

    public class Totals
    {
        /// <summary>
        /// Total goals scored in each time slice.
        /// </summary>
        [JsonPropertyName("scored")]
        public Dictionary<string, int> Scored { get; set; }

        [JsonPropertyName("scoredsum")]
        public int ScoredSum { get; set; }

        [JsonPropertyName("average_time_first_goal")]
        public double AverageTimeFirstGoal { get; set; }

        [JsonPropertyName("matches")]
        public int Matches { get; set; }
    }
}
