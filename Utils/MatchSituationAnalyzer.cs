using fredapi.Model.MatchSituationStats;

namespace fredapi.Utils;

public static class MatchSituationAnalyzer
{
    public static AggregatedStats AnalyzeMatchSituation(MatchSituationStats stats)
    {
        if (stats?.Data == null || !stats.Data.Any())
        {
            return new AggregatedStats
            {
                TotalTime = 0,
                Home = new TeamAggregatedStats(),
                Away = new TeamAggregatedStats(),
                DominantTeam = "Unknown",
                MatchMomentum = "Unknown"
            };
        }

        var aggregatedStats = new AggregatedStats
        {
            TotalTime = stats.Data.Max(x => x.Time),
            Home = AnalyzeTeamStats(stats.Data.Select(x => x.Home).ToList()),
            Away = AnalyzeTeamStats(stats.Data.Select(x => x.Away).ToList())
        };

        // Determine dominant team
        var homeTotal = aggregatedStats.Home.TotalAttacks + aggregatedStats.Home.TotalDangerousAttacks;
        var awayTotal = aggregatedStats.Away.TotalAttacks + aggregatedStats.Away.TotalDangerousAttacks;
        aggregatedStats.DominantTeam = homeTotal > awayTotal ? "Home" : "Away";

        // Analyze match momentum (last 5 minutes)
        var last5Minutes = stats.Data.OrderByDescending(x => x.Time).Take(5).ToList();
        var homeMomentum = last5Minutes.Sum(x => x.Home.Attack + x.Home.Dangerous);
        var awayMomentum = last5Minutes.Sum(x => x.Away.Attack + x.Away.Dangerous);
        aggregatedStats.MatchMomentum = homeMomentum > awayMomentum ? "Home" : "Away";

        return aggregatedStats;
    }

    private static TeamAggregatedStats AnalyzeTeamStats(List<TeamStats> teamStats)
    {
        if (teamStats == null || !teamStats.Any())
        {
            return new TeamAggregatedStats();
        }

        var totalAttacks = teamStats.Sum(x => x.Attack);
        var totalDangerous = teamStats.Sum(x => x.Dangerous);
        var totalSafe = teamStats.Sum(x => x.Safe);
        var totalAttackCount = teamStats.Sum(x => x.AttackCount);
        var totalDangerousCount = teamStats.Sum(x => x.DangerousCount);
        var totalSafeCount = teamStats.Sum(x => x.SafeCount);

        var total = totalAttacks + totalDangerous + totalSafe;

        return new TeamAggregatedStats
        {
            TotalAttacks = totalAttacks,
            TotalDangerousAttacks = totalDangerous,
            TotalSafeAttacks = totalSafe,
            TotalAttackCount = totalAttackCount,
            TotalDangerousCount = totalDangerousCount,
            TotalSafeCount = totalSafeCount,
            AttackPercentage = total > 0 ? (double)totalAttacks / total * 100 : 0,
            DangerousAttackPercentage = total > 0 ? (double)totalDangerous / total * 100 : 0,
            SafeAttackPercentage = total > 0 ? (double)totalSafe / total * 100 : 0,
            Last5Minutes = teamStats.OrderByDescending(x => x.Attack + x.Dangerous)
                                  .Take(5)
                                  .ToList()
        };
    }
}