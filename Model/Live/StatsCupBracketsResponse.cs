using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace fredapi.Model.Live.StatsCupBracketsResponse
{
    public class StatsCupBracketsResponse
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
        [JsonPropertyName("bracket")]
        public Bracket Bracket { get; set; }

        [JsonPropertyName("cup")]
        public Cup Cup { get; set; }

        [JsonPropertyName("sportid")]
        public int SportId { get; set; }

        [JsonPropertyName("season")]
        public Season Season { get; set; }

        [JsonPropertyName("roundsets")]
        public Dictionary<string, RoundsetContainer> Roundsets { get; set; }

        [JsonPropertyName("stadiums")]
        public Dictionary<string, Stadium> Stadiums { get; set; }

        [JsonPropertyName("translations")]
        public Translations Translations { get; set; }
    }

    public class Bracket
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }

        [JsonPropertyName("_id")]
        public int Id { get; set; }

        [JsonPropertyName("_typeid")]
        public int TypeId { get; set; }

        [JsonPropertyName("state_id")]
        public int StateId { get; set; }

        [JsonPropertyName("state_name")]
        public string StateName { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("cup_id")]
        public int CupId { get; set; }

        [JsonPropertyName("tournament_id")]
        public int TournamentId { get; set; }
    }

    public class Cup
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }

        [JsonPropertyName("_id")]
        public string Id { get; set; }

        [JsonPropertyName("tournamentid")]
        public string TournamentId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("abbr")]
        public string Abbr { get; set; }

        [JsonPropertyName("seasonid")]
        public string SeasonId { get; set; }

        [JsonPropertyName("seasontype")]
        public string SeasonType { get; set; }

        [JsonPropertyName("seasontypename")]
        public string SeasonTypeName { get; set; }

        [JsonPropertyName("seasontypeunique")]
        public string SeasonTypeUnique { get; set; }

        [JsonPropertyName("start")]
        public string Start { get; set; }  // In this cup object the start and end are simple strings

        [JsonPropertyName("end")]
        public string End { get; set; }
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

    // The container for a roundset – note that each property name (key) in the roundsets dictionary
    // (for example "124605") maps to a RoundsetContainer.
    public class RoundsetContainer
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }

        [JsonPropertyName("_id")]
        public int Id { get; set; }

        [JsonPropertyName("firstmatchdate")]
        public TimeInfo FirstMatchDate { get; set; }

        [JsonPropertyName("lastmatchdate")]
        public TimeInfo LastMatchDate { get; set; }

        [JsonPropertyName("roundsettype")]
        public string RoundsetType { get; set; }

        [JsonPropertyName("roundsetnumber")]
        public int RoundsetNumber { get; set; }

        [JsonPropertyName("rounds")]
        public Dictionary<string, RoundsetRoundContainer> Rounds { get; set; }
    }

    public class RoundsetRoundContainer
    {
        [JsonPropertyName("round")]
        public BracketRound Round { get; set; }

        [JsonPropertyName("blocks")]
        public Dictionary<string, Block> Blocks { get; set; }
    }

    public class BracketRound
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }

        [JsonPropertyName("_id")]
        public int Id { get; set; }

        [JsonPropertyName("cuproundnumber")]
        public int? CupRoundNumber { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("shortname")]
        public string ShortName { get; set; }

        [JsonPropertyName("longname")]
        public string LongName { get; set; }

        [JsonPropertyName("firstmatchdate")]
        public TimeInfo FirstMatchDate { get; set; }

        [JsonPropertyName("lastmatchdate")]
        public TimeInfo LastMatchDate { get; set; }
    }

    public class Block
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }

        [JsonPropertyName("_id")]
        public int Id { get; set; }

        [JsonPropertyName("_cuproundid")]
        public int CupRoundId { get; set; }

        [JsonPropertyName("_typeid")]
        public int TypeId { get; set; }

        [JsonPropertyName("blocknumber")]
        public int BlockNumber { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("requiredlegs")]
        public int RequiredLegs { get; set; }

        [JsonPropertyName("maxtotallegs")]
        public int MaxTotalLegs { get; set; }

        [JsonPropertyName("hasbye")]
        public bool HasBye { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }

        [JsonPropertyName("teamidhome")]
        public int TeamIdHome { get; set; }

        [JsonPropertyName("teamidaway")]
        public int TeamIdAway { get; set; }

        [JsonPropertyName("teamidwinner")]
        public int? TeamIdWinner { get; set; }

        [JsonPropertyName("homescore")]
        public int Homescore { get; set; }

        [JsonPropertyName("awayscore")]
        public int Awayscore { get; set; }

        [JsonPropertyName("swappedteams")]
        public bool SwappedTeams { get; set; }

        [JsonPropertyName("parents")]
        public List<int> Parents { get; set; }

        [JsonPropertyName("children")]
        public List<int> Children { get; set; }

        [JsonPropertyName("matches")]
        public Dictionary<string, Match> Matches { get; set; }
    }

    public class Match
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }

        [JsonPropertyName("_id")]
        public int Id { get; set; }

        [JsonPropertyName("_sid")]
        public int Sid { get; set; }

        [JsonPropertyName("_rcid")]
        public int Rcid { get; set; }

        [JsonPropertyName("_tid")]
        public int Tid { get; set; }

        [JsonPropertyName("_utid")]
        public int Utid { get; set; }

        [JsonPropertyName("time")]
        public TimeInfo Time { get; set; }

        [JsonPropertyName("round")]
        public int Round { get; set; }

        [JsonPropertyName("roundname")]
        public RoundName RoundName { get; set; }

        [JsonPropertyName("cuproundmatchnumber")]
        public string CupRoundMatchNumber { get; set; }

        [JsonPropertyName("cuproundnumberofmatches")]
        public string CupRoundNumberOfMatches { get; set; }

        [JsonPropertyName("week")]
        public int Week { get; set; }

        [JsonPropertyName("result")]
        public Result Result { get; set; }

        [JsonPropertyName("periods")]
        public Periods Periods { get; set; }

        [JsonPropertyName("_seasonid")]
        public int SeasonId { get; set; }

        [JsonPropertyName("teams")]
        public Teams Teams { get; set; }

        [JsonPropertyName("neutralground")]
        public bool NeutralGround { get; set; }

        [JsonPropertyName("comment")]
        public string Comment { get; set; }

        [JsonPropertyName("status")]
        public object Status { get; set; }

        [JsonPropertyName("tobeannounced")]
        public bool ToBeAnnounced { get; set; }

        [JsonPropertyName("postponed")]
        public bool Postponed { get; set; }

        [JsonPropertyName("canceled")]
        public bool Canceled { get; set; }

        [JsonPropertyName("inlivescore")]
        public bool InLiveScore { get; set; }

        [JsonPropertyName("stadiumid")]
        public int StadiumId { get; set; }

        [JsonPropertyName("bestof")]
        public object BestOf { get; set; }

        [JsonPropertyName("walkover")]
        public bool Walkover { get; set; }

        [JsonPropertyName("retired")]
        public bool Retired { get; set; }

        [JsonPropertyName("disqualified")]
        public bool Disqualified { get; set; }
    }

    public class RoundName
    {
        [JsonPropertyName("_doc")]
        public string Doc { get; set; }

        [JsonPropertyName("_id")]
        public int Id { get; set; }

        [JsonPropertyName("displaynumber")]
        public object DisplayNumber { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("shortname")]
        public string ShortName { get; set; }

        [JsonPropertyName("cuproundnumber")]
        public object CupRoundNumber { get; set; }

        [JsonPropertyName("statisticssortorder")]
        public int StatisticssortOrder { get; set; }
    }

    public class Result
    {
        [JsonPropertyName("home")]
        public int? Home { get; set; }

        [JsonPropertyName("away")]
        public int? Away { get; set; }

        [JsonPropertyName("period")]
        public string Period { get; set; }

        [JsonPropertyName("winner")]
        public string Winner { get; set; }
    }

    public class Periods
    {
        [JsonPropertyName("p1")]
        public Score P1 { get; set; }

        [JsonPropertyName("ft")]
        public Score FT { get; set; }

        // Some matches have an additional period, e.g., "ap"
        [JsonPropertyName("ap")]
        public Score AP { get; set; }
    }

    public class Score
    {
        [JsonPropertyName("home")]
        public int Home { get; set; }

        [JsonPropertyName("away")]
        public int Away { get; set; }
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
        public string State { get; set; }

        [JsonPropertyName("cc")]
        public CountryCode CC { get; set; }

        [JsonPropertyName("capacity")]
        public string Capacity { get; set; }

        [JsonPropertyName("hometeams")]
        public List<UniqueTeam> HomeTeams { get; set; }

        [JsonPropertyName("googlecoords")]
        public string GoogleCoords { get; set; }

        [JsonPropertyName("pitchsize")]
        public object PitchSize { get; set; }
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

    public class Translations
    {
        [JsonPropertyName("bye")]
        public string Bye { get; set; }

        [JsonPropertyName("retired")]
        public string Retired { get; set; }

        [JsonPropertyName("walkover")]
        public string Walkover { get; set; }
    }
}
