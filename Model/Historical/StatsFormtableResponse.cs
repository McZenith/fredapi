using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace fredapi.Model.Historical.StatsFormtable
{
    // Root response object
    public class StatsFormtableResponse
    {
        [JsonPropertyName("queryUrl")]
        public string QueryUrl { get; set; }
        
        [JsonPropertyName("doc")]
        public List<StatsFormtableDoc> Doc { get; set; }
    }

    public class StatsFormtableDoc
    {
        [JsonPropertyName("event")]
        public string Event { get; set; }
        
        [JsonPropertyName("_dob")]
        public long Dob { get; set; }
        
        [JsonPropertyName("_maxage")]
        public int MaxAge { get; set; }
        
        [JsonPropertyName("data")]
        public StatsFormtableData Data { get; set; }
    }

    public class StatsFormtableData
    {
        [JsonPropertyName("matchtype")]
        public List<MatchType> MatchType { get; set; }
        
        [JsonPropertyName("tabletype")]
        public List<TableType> TableType { get; set; }
        
        [JsonPropertyName("season")]
        public Season Season { get; set; }
        
        [JsonPropertyName("winpoints")]
        public int WinPoints { get; set; }
        
        [JsonPropertyName("losspoints")]
        public int LossPoints { get; set; }
        
        [JsonPropertyName("currentround")]
        public int CurrentRound { get; set; }
        
        [JsonPropertyName("teams")]
        public List<TeamStats> Teams { get; set; }
    }

    public class MatchType
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }
        
        [JsonPropertyName("_id")]
        public int Id { get; set; }
        
        [JsonPropertyName("settypeid")]
        public int SetTypeId { get; set; }
        
        [JsonPropertyName("column")]
        public string Column { get; set; }
    }

    public class TableType
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }
        
        [JsonPropertyName("_id")]
        public int Id { get; set; }
        
        [JsonPropertyName("column")]
        public string Column { get; set; }
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

    public class TeamStats
    {
        [JsonPropertyName("team")]
        public Team Team { get; set; }
        
        // The "position" object only has three properties.
        [JsonPropertyName("position")]
        public Position Position { get; set; }
        
        // The following statistics objects use a five-property line:
        [JsonPropertyName("played")]
        public StatLine Played { get; set; }
        
        [JsonPropertyName("win")]
        public StatLine Win { get; set; }
        
        [JsonPropertyName("draw")]
        public StatLine Draw { get; set; }
        
        [JsonPropertyName("loss")]
        public StatLine Loss { get; set; }
        
        [JsonPropertyName("goalsfor")]
        public StatLine GoalsFor { get; set; }
        
        [JsonPropertyName("goalsagainst")]
        public StatLine GoalsAgainst { get; set; }
        
        [JsonPropertyName("goaldifference")]
        public StatLine GoalDifference { get; set; }
        
        [JsonPropertyName("points")]
        public StatLine Points { get; set; }
        
        [JsonPropertyName("form")]
        public Form Form { get; set; }
        
        [JsonPropertyName("nextopponent")]
        public NextOpponent NextOpponent { get; set; }
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

    public class Position
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }
        
        [JsonPropertyName("home")]
        public int Home { get; set; }
        
        [JsonPropertyName("away")]
        public int Away { get; set; }
    }

    /// <summary>
    /// Represents statistics with five properties.
    /// </summary>
    public class StatLine
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }
        
        [JsonPropertyName("totalhome")]
        public int TotalHome { get; set; }
        
        [JsonPropertyName("totalaway")]
        public int TotalAway { get; set; }
        
        [JsonPropertyName("home")]
        public int Home { get; set; }
        
        [JsonPropertyName("away")]
        public int Away { get; set; }
    }

    public class Form
    {
        [JsonPropertyName("total")]
        public List<FormEntry> Total { get; set; }
        
        [JsonPropertyName("home")]
        public List<FormEntry> Home { get; set; }
        
        [JsonPropertyName("away")]
        public List<FormEntry> Away { get; set; }
    }

    public class FormEntry
    {
        [JsonPropertyName("typeid")]
        public string TypeId { get; set; }
        
        [JsonPropertyName("value")]
        public string Value { get; set; }
        
        [JsonPropertyName("homematch")]
        public bool HomeMatch { get; set; }
        
        [JsonPropertyName("neutralground")]
        public bool NeutralGround { get; set; }
        
        [JsonPropertyName("matchid")]
        public int MatchId { get; set; }
    }

    public class NextOpponent
    {
        [JsonPropertyName("team")]
        public Team Team { get; set; }
        
        [JsonPropertyName("date")]
        public TimeInfo Date { get; set; }
        
        [JsonPropertyName("matchdifficultyrating")]
        public MatchDifficultyRating MatchDifficultyRating { get; set; }
    }

    public class MatchDifficultyRating
    {
        [JsonPropertyName("home")]
        public int? Home { get; set; }
        
        [JsonPropertyName("away")]
        public int? Away { get; set; }
    }
}
