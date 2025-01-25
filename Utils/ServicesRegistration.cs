using fredapi.Database;
using fredapi.SignalR;
using fredapi.SportRadarService.Background;
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
        //services.AddHostedService<ApiPollingService>();
        services.AddHostedService<UpcomingMatchBackgroundService>();
        services.AddHostedService<LiveMatchBackgroundService>();
        services.AddSingleton<MongoDbService>();


        return services;
    }
}