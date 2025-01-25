namespace fredapi.SportRadarService.TokenService;

public class TokenService(ISportRadarTokenService tokenService) : ITokenService
{
    public static string? ApiToken { get; set; }
    public async Task GetSportRadarToken()
    {
        ApiToken = await tokenService.ExtractAuthTokenAsync();
    }
}

public interface ITokenService
{
    Task GetSportRadarToken();
}