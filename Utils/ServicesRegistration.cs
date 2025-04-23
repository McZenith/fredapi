using fredapi.Database;
using fredapi.SportRadarService.Background;
using fredapi.SportRadarService.Background.ArbitrageLiveMatchBackgroundService;
using fredapi.SportRadarService.TokenService;
using fredapi.SportRadarService.Transformers;

namespace fredapi.Utils;

public static class ServicesRegistration
{
    public static IServiceCollection AddSportRadarService(this IServiceCollection services)
    {
        services.AddHttpClient<SportRadarService.SportRadarService>();
        services.AddScoped<SportMatchesPredictionTransformer>();

        services.AddScoped<ISportRadarTokenService, SportRadarTokenService>();
        //services.AddScoped<IRedisService, RedisService>();
        //services.AddSingleton<ISubscriberTracker, SubscriberTracker>();
        services.AddScoped<ITokenService, TokenService>();
        //services.AddHostedService<UpcomingMatchBackgroundService>();
        //services.AddHostedService<EnrichedStatsBackgroundService>();
        //services.AddHostedService<LiveMatchBackgroundService>();
        services.AddHostedService<ArbitrageLiveMatchBackgroundService>();
        //services.AddHostedService<UpcomingArbitrageBackgroundService>();
        services.AddHostedService<UpcomingMatchEnrichmentService>();
        services.AddHostedService<PredictionDataBackgroundService>();
        services.AddSingleton<MongoDbService>();
        
        return services;
    }
}