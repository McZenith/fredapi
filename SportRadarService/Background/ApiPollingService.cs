using fredapi.SportRadarService.TokenService;

namespace fredapi.SportRadarService.Background;

public class ApiPollingService(
    IServiceProvider serviceProvider,
    ILogger<ApiPollingService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
                await tokenService.GetSportRadarToken();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while fetching and caching data");
            }

            await Task.Delay(TimeSpan.FromHours(3), stoppingToken);
        }
    }
    
}