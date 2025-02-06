using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace fredapi.Model.Historical.StatsUniqueTeamSeasonOfficialsResponse
{
    public class StatsUniqueTeamSeasonOfficialsResponse
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
        public Data Data { get; set; }
    }

    public class Data
    {
        [JsonPropertyName("season")]
        public Season Season { get; set; }

        [JsonPropertyName("officials")]
        public List<Official> Officials { get; set; }
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

    public class Official
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; } // e.g., "extendedplayer"

        [JsonPropertyName("_id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("fullname")]
        public string FullName { get; set; }

        [JsonPropertyName("birthdate")]
        public TimeInfo BirthDate { get; set; }

        [JsonPropertyName("nationality")]
        public CountryCode Nationality { get; set; }

        [JsonPropertyName("primarypositiontype")]
        public object PrimaryPositionType { get; set; }

        [JsonPropertyName("haslogo")]
        public bool HasLogo { get; set; }

        [JsonPropertyName("membersince")]
        public TimeInfo MemberSince { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("weight")]
        public int Weight { get; set; }

        [JsonPropertyName("_foot")]
        public string FootInternal { get; set; }

        [JsonPropertyName("foot")]
        public string Foot { get; set; }

        [JsonPropertyName("nickname")]
        public string Nickname { get; set; }

        [JsonPropertyName("marketvalue")]
        public int MarketValue { get; set; }

        [JsonPropertyName("shirtnumber")]
        public string ShirtNumber { get; set; }

        [JsonPropertyName("roletypeid")]
        public int RoleTypeId { get; set; }

        [JsonPropertyName("role")]
        public Role Role { get; set; }
    }

    public class Role
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; } // e.g., "playerrole"

        [JsonPropertyName("_playerid")]
        public int PlayerId { get; set; }

        [JsonPropertyName("_id")]
        public int Id { get; set; }

        [JsonPropertyName("_type")]
        public int Type { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } // e.g., "Manager"

        [JsonPropertyName("start")]
        public TimeInfo Start { get; set; }

        [JsonPropertyName("end")]
        public object End { get; set; } // may be null

        [JsonPropertyName("active")]
        public bool Active { get; set; }

        [JsonPropertyName("team")]
        public Team Team { get; set; }
    }

    public class Team
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; } // e.g., "uniqueteam"

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

        [JsonPropertyName("realcategory")]
        public RealCategory RealCategory { get; set; }
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
}
