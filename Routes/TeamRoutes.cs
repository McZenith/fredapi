namespace fredapi.Routes;

public static class TeamRoutes
{
    public static RouteGroupBuilder MapTeamRoutes(this RouteGroupBuilder group)
    {
        group.MapGet("/team/lastx", async (string teamId, int? count, SportRadarService.SportRadarService service) =>
                await service.GetTeamLastXAsync(teamId, count ?? 5))
            .WithName("GetTeamLastX")
            .WithDescription("Get team's last X matches");

        group.MapGet("/team/lastx/extended", async (string teamId, SportRadarService.SportRadarService service) =>
                await service.GetTeamLastXExtendedAsync(teamId))
            .WithName("GetTeamLastXExtended")
            .WithDescription("Get team's last matches with extended information");

        group.MapGet("/team/nextx", async (string teamId, int? count, SportRadarService.SportRadarService service) =>
                await service.GetTeamNextXAsync(teamId, count ?? 5))
            .WithName("GetTeamNextX")
            .WithDescription("Get team's next X matches");
        
        group.MapGet("/team/stats/form/table", async (string teamId, SportRadarService.SportRadarService service) =>
                await service.GetStatsFormTableAsync(teamId))
            .WithName("GetTeamStatsFormTable")
            .WithDescription("Get team's stats form table");

        group.MapGet("/team/versus-recent", async (string teamId1, string teamId2, int? count, SportRadarService.SportRadarService service) =>
                await service.GetTeamVersusRecentAsync(teamId1, teamId2, count ?? 10))
            .WithName("GetTeamVersusRecent")
            .WithDescription("Get recent matches between two teams");
        
        group.MapGet("/team/info", async (string teamId, SportRadarService.SportRadarService service) =>
                await service.GetStatsTeamInfoAsync(teamId))
            .WithName("GetStatsTeamInfo")
            .WithDescription("Get team's information");

        group.MapGet("/team/squad", async (string teamId, SportRadarService.SportRadarService service) =>
                await service.GetStatsTeamSquadAsync(teamId))
            .WithName("GetStatsTeamSquad")
            .WithDescription("Get team's squad information");

        return group;
    }
}
