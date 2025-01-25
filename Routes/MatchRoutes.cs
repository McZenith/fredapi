namespace fredapi.Routes;

public static class MatchRoutes
{
    public static RouteGroupBuilder MapMatchRoutes(this RouteGroupBuilder group)
    {
        group.MapGet("/match/upcoming", async (SportRadarService.SportRadarService service) =>
                await service.GetUpcomingMatchesAsync())
            .WithName("GetUpcomingMatches")
            .WithDescription("Get upcoming matches data");

        group.MapGet("/match/details_extended", async (string matchId, SportRadarService.SportRadarService service) =>
                await service.GetMatchDetailsExtendedAsync(matchId))
            .WithName("GetMatchDetailsExtended")
            .WithDescription("Get detailed match information");
        
        group.MapGet("/match/details", async (string matchId, SportRadarService.SportRadarService service) =>
                await service.GetMatchDetailsAsync(matchId))
            .WithName("GetMatchDetails")
            .WithDescription("Get simple match information");

        group.MapGet("/match/info", async (string matchId, SportRadarService.SportRadarService service) =>
                await service.GetMatchInfoAsync(matchId))
            .WithName("GetMatchInfo")
            .WithDescription("Get basic match information");

        group.MapGet("/match/odds", async (string matchId, SportRadarService.SportRadarService service) =>
                await service.GetMatchOddsAsync(matchId))
            .WithName("GetMatchOdds")
            .WithDescription("Get match odds information");
        
        group.MapGet("/match/insights", async (string matchId, SportRadarService.SportRadarService service) =>
                await service.GetMatchInsightsAsync(matchId))
            .WithName("GetMatchInsights")
            .WithDescription("Get match insights");

        group.MapGet("/matches/date", async (string date, SportRadarService.SportRadarService service) =>
                await service.GetSportMatchesAsync(date))
            .WithName("GetSportMatches")
            .WithDescription("Get matches for a specific date");

        group.MapGet("/matches/live", async (SportRadarService.SportRadarService service) =>
                await service.GetLiveMatchesAsync())
            .WithName("GetLiveMatches")
            .WithDescription("Get live matches for a specific date");

        group.MapGet("/matches/select", async (int leagueId, int? type, SportRadarService.SportRadarService service) =>
                await service.GetMatchSelectAsync(leagueId, type ?? 1))
            .WithName("GetMatchSelect")
            .WithDescription("Get match selection for a league");

        group.MapGet("/phrases/{matchId}", async (string matchId, SportRadarService.SportRadarService service) =>
                await service.GetMatchPhrasesAsync(matchId))
            .WithName("GetMatchPhrases")
            .WithDescription("Get match phrases for a specific match.");

        group.MapGet("/situation/{matchId}", async (string matchId, SportRadarService.SportRadarService service) =>
                await service.GetMatchSituationAsync(matchId))
            .WithName("GetMatchSituation")
            .WithDescription("Get match situation for a specific match.");

        group.MapGet("/squads/{matchId}", async (string matchId, SportRadarService.SportRadarService service) =>
                await service.GetMatchSquadsAsync(matchId))
            .WithName("GetMatchSquads")
            .WithDescription("Get match squads for a specific match.");
        
        group.MapGet("/timeline/{matchId}", async (string matchId, SportRadarService.SportRadarService service) =>
                await service.GetMatchTimelineAsync(matchId))
            .WithName("GetMatchTimeline")
            .WithDescription("Get match timeline for a specific match.");
        
        group.MapGet("/timeline/delta/{matchId}", async (string matchId, SportRadarService.SportRadarService service) =>
                await service.GetMatchTimelineDeltaAsync(matchId))
            .WithName("GetMatchTimelineDelta")
            .WithDescription("Get match timeline delta for a specific match.");
        // Match Routes
        group.MapGet("/match/funfacts/{matchId}", async (string matchId, SportRadarService.SportRadarService service) =>
                await service.GetMatchFunFactsAsync(matchId))
            .WithName("GetMatchFunFacts")
            .WithDescription("Get match fun facts");
        
        group.MapGet("/match/form/{matchId}", async (string matchId, SportRadarService.SportRadarService service) =>
                await service.GetStatsMatchFormAsync(matchId))
            .WithName("GetMatchForm")
            .WithDescription("Get match form statistics");

        group.MapGet("/match/phrasesdelta/{matchId}", async (string matchId, SportRadarService.SportRadarService service) =>
                await service.GetMatchPhrasesDeltaAsync(matchId))
            .WithName("GetMatchPhrasesDelta")
            .WithDescription("Get match phrases delta");
        
        return group;
    }
}
