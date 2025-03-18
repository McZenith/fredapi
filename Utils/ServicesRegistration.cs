using fredapi.Database;
using fredapi.SignalR;
using fredapi.SportRadarService.Background;
using fredapi.SportRadarService.Background.ArbitrageLiveMatchBackgroundService;
using fredapi.SportRadarService.Background.UpcomingArbitrageBackgroundService;
using fredapi.SportRadarService.TokenService;

namespace fredapi.Utils;

public static class ServicesRegistration
{
    public static IServiceCollection AddSportRadarService(this IServiceCollection services)
    {
        services.AddHttpClient<SportRadarService.SportRadarService>();
        services.AddScoped<ISportRadarTokenService, SportRadarTokenService>();
        services.AddScoped<IRedisService, RedisService>();
        //services.AddSingleton<ISubscriberTracker, SubscriberTracker>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddHostedService<ApiPollingService>();
        //services.AddHostedService<UpcomingMatchBackgroundService>();
        //services.AddHostedService<EnrichedStatsBackgroundService>();
        //services.AddHostedService<LiveMatchBackgroundService>();
        //services.AddHostedService<ArbitrageLiveMatchBackgroundService>();
        //services.AddHostedService<UpcomingArbitrageBackgroundService>();
        services.AddHostedService<UpcomingMatchEnrichmentService>();
        services.AddSingleton<MongoDbService>();


        return services;
    }
}