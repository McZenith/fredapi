namespace fredapi.Routes;

public static class SeasonRoutes
{
    public static RouteGroupBuilder MapSeasonRoutes(this RouteGroupBuilder group)
    {
        group.MapGet("/season/metadata", async (string seasonId, SportRadarService.SportRadarService service) =>
                await service.GetSeasonMetadataAsync(seasonId))
            .WithName("GetSeasonMetadata")
            .WithDescription("Get season metadata information");

        group.MapGet("/season/goals", async (string seasonId, SportRadarService.SportRadarService service) =>
                await service.GetSeasonGoalsAsync(seasonId))
            .WithName("GetSeasonGoals")
            .WithDescription("Get season goals statistics");
        
        group.MapGet("/season/dynamic/table", async (string seasonId, SportRadarService.SportRadarService service) =>
                await service.GetSeasonDynamicTableAsync(seasonId))
            .WithName("GetSeasonDynamicTable")
            .WithDescription("Get season dynamic table data");

        group.MapGet("/match/stats/match/table", async (string matchId, SportRadarService.SportRadarService service) =>
                await service.GetStatsSeasonMatchTableSpliceAsync(matchId))
            .WithName("GetStatsMatchTableSplice")
            .WithDescription("Get match table splice data for a specific match");

        group.MapGet("/season/live/table", async (string seasonId, SportRadarService.SportRadarService service) =>
                await service.GetSeasonLiveTableAsync(seasonId))
            .WithName("GetSeasonLiveTable")
            .WithDescription("Get season live table data");
        
        group.MapGet("/season/stats/overunder", async (string seasonId, SportRadarService.SportRadarService service) =>
                await service.GetStatsSeasonOverUnderAsync(seasonId))
            .WithName("GetStatsSeasonOverUnder")
            .WithDescription("Get stats season over under data");

        group.MapGet("/season/topcards", async (string seasonId, SportRadarService.SportRadarService service) =>
                await service.GetSeasonTopCardsAsync(seasonId))
            .WithName("GetSeasonTopCards")
            .WithDescription("Get season top cards information");

        group.MapGet("/season/tables", async (string seasonId, SportRadarService.SportRadarService service) =>
                await service.GetSeasonTablesAsync(seasonId))
            .WithName("GetSeasonTables")
            .WithDescription("Get season tables information");
        
        group.MapGet("/brackets/{tournamentId}", async (string tournamentId, SportRadarService.SportRadarService service) =>
                await service.GetBracketsAsync(tournamentId))
            .WithName("GetBrackets")
            .WithDescription("Get the brackets for a specific tournament.");

        // Season Routes
    group.MapGet("/season/teampositionhistory/{seasonId}/{teamId}/{positionId}", async (string seasonId, string teamId, string positionId, SportRadarService.SportRadarService service) =>
            await service.GetStatsSeasonTeamPositionHistoryAsync(seasonId, teamId, positionId))
            .WithName("GetStatsSeasonTeamPositionHistory")
            .WithDescription("Get team's position history for the season");

    group.MapGet("/season/teamscoringconceding/{seasonId}/{teamId}", async (string seasonId, string teamId, SportRadarService.SportRadarService service) =>
            await service.GetStatsSeasonTeamscoringConcedingAsync(seasonId, teamId))
            .WithName("GetStatsSeasonTeamscoringConceding")
            .WithDescription("Get team's scoring and conceding stats for the season");

    group.MapGet("/season/teamfixtures/{seasonId}/{teamId}", async (string seasonId, string teamId, SportRadarService.SportRadarService service) =>
            await service.GetStatsSeasonTeamFixturesAsync(seasonId, teamId))
            .WithName("GetStatsSeasonTeamFixtures")
            .WithDescription("Get team's fixtures for the season");

    group.MapGet("/season/teamdisciplinary/{seasonId}/{teamId}", async (string seasonId, string teamId, SportRadarService.SportRadarService service) =>
            await service.GetStatsSeasonTeamDisciplinaryAsync(seasonId, teamId))
            .WithName("GetStatsSeasonTeamDisciplinary")
            .WithDescription("Get team's disciplinary stats for the season");

    group.MapGet("/season/fixtures/{seasonId}", async (string seasonId, SportRadarService.SportRadarService service) =>
            await service.GetStatsSeasonFixturesAsync(seasonId))
            .WithName("GetStatsSeasonFixtures")
            .WithDescription("Get fixtures for the season");

    group.MapGet("/season/uniqueteamstats/{seasonId}", async (string seasonId, SportRadarService.SportRadarService service) =>
            await service.GetStatsSeasonUniqueTeamStatsAsync(seasonId))
            .WithName("GetStatsSeasonUniqueTeamStats")
            .WithDescription("Get unique team stats for the season");

    group.MapGet("/season/topgoals/{seasonId}", async (string seasonId, SportRadarService.SportRadarService service) =>
            await service.GetStatsSeasonTopGoalsAsync(seasonId))
            .WithName("GetStatsSeasonTopGoals")
            .WithDescription("Get top goalscorers for the season");

        return group;
    }
}