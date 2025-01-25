using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Text.Json;
using static System.DateTimeOffset;

namespace fredapi.SportRadarService;

public class SportRadarService 
{
    private readonly ILogger<SportRadarService> _logger;

    public SportRadarService(HttpClient httpClient,
        ILogger<SportRadarService> logger)
    {
        var timestamp = UtcNow.ToUnixTimeSeconds();
        var etag = $"\"{timestamp}\"";
        httpClient.DefaultRequestHeaders.IfNoneMatch.Clear();
        httpClient.DefaultRequestHeaders.IfNoneMatch.Add(new EntityTagHeaderValue(etag));
        _logger = logger;
    }

    public async Task<IResult> GetUpcomingMatchesAsync()
    {
        try
        {
            var response = await MakeRequestWithRetryAsync(
                $"https://lmt.fn.sportradar.com/demolmt/en/Etc:UTC/gismo/sport_matches/1/{DateTime.Today.AddDays(1):yyyy-MM-dd}/1/{TokenService.TokenService.ApiToken}");
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }
   
    public async Task<IResult> GetMatchDetailsAsync(string matchId)
    {
        try
        {
            ValidateId(matchId, "Match");

            var url = $"https://widgets.fn.sportradar.com/common/en/Etc:UTC/gismo/match_details/{matchId}{TokenService.TokenService.ApiToken}";


            var response = await MakeRequestWithRetryAsync(url);

            return response != null ? Results.Ok(response) : Results.StatusCode(304);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }


    public async Task<IResult> GetMatchDetailsExtendedAsync(string matchId)
    {
        try
        {
            ValidateId(matchId, "Match");

            var url = $"https://widgets.fn.sportradar.com/common/en/Etc:UTC/gismo/match_detailsextended/{matchId}{TokenService.TokenService.ApiToken}";


            var response = await MakeRequestWithRetryAsync(url);

            return response != null ? Results.Ok(response) : Results.StatusCode(304);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<IResult> GetMatchInfoAsync(string matchId)
    {
        try
        {
            ValidateId(matchId, "Match");

            var url = $"https://widgets.fn.sportradar.com/common/en/Etc:UTC/gismo/match_info/{matchId}{TokenService.TokenService.ApiToken}";
            var response = await MakeRequestWithRetryAsync(url);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<IResult> GetMatchOddsAsync(string matchId)
    {
        try
        {
            ValidateId(matchId, "Match");

            var url = $"https://widgets.fn.sportradar.com/common/en/Etc:UTC/gismo/match_bookmakerodds/{matchId}{TokenService.TokenService.ApiToken}";
            var response = await MakeRequestWithRetryAsync(url);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<IResult> GetSeasonDynamicTableAsync(string seasonId)
    {
        try
        {
            ValidateId(seasonId, "Season");

            var url = $"https://widgets.fn.sportradar.com/common/en/Etc:UTC/gismo/season_dynamictable/{seasonId}{TokenService.TokenService.ApiToken}";
            var response = await MakeRequestWithRetryAsync(url);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<IResult> GetStatsFormTableAsync(string seasonId)
    {
        try
        {
            ValidateId(seasonId, "Form");

            var url = $"https://widgets.fn.sportradar.com/common/en/Etc:UTC/gismo/stats_formtable/{seasonId}{TokenService.TokenService.ApiToken}";
            var response = await MakeRequestWithRetryAsync(url);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<IResult> GetSeasonLiveTableAsync(string seasonId)
    {
        try
        {
            ValidateId(seasonId, "Form");
            
            var url = $"https://widgets.fn.sportradar.com/common/en/Etc:UTC/gismo/season_livetable/{seasonId}{TokenService.TokenService.ApiToken}";
            var response = await MakeRequestWithRetryAsync(url);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<IResult> GetStatsSeasonOverUnderAsync(string seasonId)
    {
        try
        {
            ValidateId(seasonId, "Season");
            var url = $"https://widgets.fn.sportradar.com/common/en/Etc:UTC/gismo/stats_season_overunder/{seasonId}{TokenService.TokenService.ApiToken}";
            var response = await MakeRequestWithRetryAsync(url);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<IResult> GetStatsSeasonTeamscoringConcedingAsync(string seasonId, string teamId)
    {
        try
        {
            ValidateId(seasonId, "Season");
            ValidateId(teamId, "Team");
            var url = $"https://widgets.fn.sportradar.com/common/en/Etc:UTC/gismo/stats_season_teamscoringconceding/{seasonId}/{teamId}/-1{TokenService.TokenService.ApiToken}";
            var response = await MakeRequestWithRetryAsync(url);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<IResult> GetStatsSeasonTeamPositionHistoryAsync(string seasonId, string teamId, string positionId)
    {
        try
        {
            ValidateId(seasonId, "Season");
            ValidateId(teamId, "Team");
            ValidateId(positionId, "Position");
            var url =
                $"https://widgets.fn.sportradar.com/common/en/Etc:UTC/gismo/stats_season_teampositionhistory/{seasonId}/{teamId}/{positionId}{TokenService.TokenService.ApiToken}";
            var response = await MakeRequestWithRetryAsync(url);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<IResult> GetStatsSeasonTeamFixturesAsync(string seasonId, string teamId)
    {
        try
        {
            ValidateId(seasonId, "Season");
            ValidateId(teamId, "Team");
            var url = $"https://widgets.fn.sportradar.com/common/en/Etc:UTC/gismo/stats_season_teamfixtures/{seasonId}/{teamId}{TokenService.TokenService.ApiToken}";
            var response = await MakeRequestWithRetryAsync(url);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<IResult> GetStatsSeasonTeamDisciplinaryAsync(string seasonId, string teamId)
    {
        try
        {
            ValidateId(seasonId, "Season");
            ValidateId(teamId, "Team");
            var url = $"https://widgets.fn.sportradar.com/common/en/Etc:UTC/gismo/stats_season_teamdisciplinary/{seasonId}/{teamId}{TokenService.TokenService.ApiToken}";
            var response = await MakeRequestWithRetryAsync(url);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<IResult> GetStatsSeasonFixturesAsync(string seasonId)
    {
        try
        {
            ValidateId(seasonId, "Season");
            var url = $"https://widgets.fn.sportradar.com/common/en/Etc:UTC/gismo/stats_season_fixtures/{seasonId}{TokenService.TokenService.ApiToken}";
            var response = await MakeRequestWithRetryAsync(url);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<IResult> GetStatsSeasonUniqueTeamStatsAsync(string seasonId)
    {
        try
        {
            ValidateId(seasonId, "Season");
            var url = $"https://widgets.fn.sportradar.com/common/en/Etc:UTC/gismo/stats_season_uniqueteamstats/{seasonId}{TokenService.TokenService.ApiToken}";
            var response = await MakeRequestWithRetryAsync(url);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<IResult> GetStatsSeasonTopGoalsAsync(string seasonId)
    {
        try
        {
            ValidateId(seasonId, "Season");
            var url = $"https://widgets.fn.sportradar.com/common/en/Etc:UTC/gismo/stats_season_topgoals/{seasonId}{TokenService.TokenService.ApiToken}";
            var response = await MakeRequestWithRetryAsync(url);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<IResult> GetStatsTeamInfoAsync(string teamId)
    {
        try
        {
            ValidateId(teamId, "Team");
            var url = $"https://widgets.fn.sportradar.com/common/en/Etc:UTC/gismo/stats_team_info/{teamId}{TokenService.TokenService.ApiToken}";
            var response = await MakeRequestWithRetryAsync(url);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<IResult> GetStatsTeamSquadAsync(string teamId)
    {
        try
        {
            ValidateId(teamId, "Team");
            var url = $"https://widgets.fn.sportradar.com/common/en/Etc:UTC/gismo/stats_team_squad/{teamId}{TokenService.TokenService.ApiToken}";
            var response = await MakeRequestWithRetryAsync(url);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<IResult> GetMatchFunFactsAsync(string matchId)
    {
        try
        {
            ValidateId(matchId, "Match");
            var url = $"https://widgets.fn.sportradar.com/common/en/Etc:UTC/gismo/match_funfacts/{matchId}{TokenService.TokenService.ApiToken}";
            var response = await MakeRequestWithRetryAsync(url);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<IResult> GetStatsMatchFormAsync(string matchId)
    {
        try
        {
            ValidateId(matchId, "Match");
            var url = $"https://lmt.fn.sportradar.com/common/en/Etc:UTC/gismo/stats_match_form/{matchId}{TokenService.TokenService.ApiToken}";
            var response = await MakeRequestWithRetryAsync(url);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<IResult> GetStatsSeasonMatchTableSpliceAsync(string seasonId)
    {
        try
        {
            ValidateId(seasonId, "Season");
            var url = $"https://widgets.fn.sportradar.com/common/en/Etc:UTC/gismo/stats_match_tableslice/{seasonId}{TokenService.TokenService.ApiToken}";
            var response = await MakeRequestWithRetryAsync(url);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<IResult> GetBracketsAsync(string tournamentId)
    {
        try
        {
            ValidateId(tournamentId, "Tournament");
            var requestId = tournamentId.Split(':').Last();
            var url = $"https://widgets.fn.sportradar.com/common/en/Etc:UTC/gismo/stats_cup_brackets/gm-{requestId}{TokenService.TokenService.ApiToken}";
            var response = await MakeRequestWithRetryAsync(url);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<IResult> GetMatchPhrasesAsync(string matchId)
    {
        try
        {
            ValidateId(matchId, "Match");
            var url = $"https://widgets.fn.sportradar.com/common/en/Etc:UTC/gismo/match_phrases/{matchId}{TokenService.TokenService.ApiToken}";
            var response = await MakeRequestWithRetryAsync(url, maxRetries: 2);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<IResult> GetMatchSituationAsync(string matchId)
    {
        try
        {
            ValidateId(matchId, "Match");
            var url = $"https://lmt.fn.sportradar.com/common/en/Etc:UTC/gismo/stats_match_situation/{matchId}{TokenService.TokenService.ApiToken}";
            var response = await MakeRequestWithRetryAsync(url, maxRetries: 2);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<IResult> GetMatchSquadsAsync(string matchId)
    {
        try
        {
            ValidateId(matchId, "Match");
            var url = $"https://lmt.fn.sportradar.com/common/en/Etc:UTC/gismo/match_squads/{matchId}{TokenService.TokenService.ApiToken}";
            var response = await MakeRequestWithRetryAsync(url);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<IResult> GetMatchTimelineAsync(string matchId)
    {
        try
        {
            ValidateId(matchId, "Match");
            var url = $"https://lmt.fn.sportradar.com/common/en/Etc:UTC/gismo/match_timeline/{matchId}{TokenService.TokenService.ApiToken}";
            var response = await MakeRequestWithRetryAsync(url, maxRetries: 2);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<IResult> GetMatchTimelineDeltaAsync(string matchId)
    {
        try
        {
            ValidateId(matchId, "Match");
            var url = $"https://lmt.fn.sportradar.com/common/en/Etc:UTC/gismo/match_timelinedelta/{matchId}{TokenService.TokenService.ApiToken}";
            var response = await MakeRequestWithRetryAsync(url);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<IResult> GetSeasonMetadataAsync(string seasonId)
    {
        try
        {
            ValidateId(seasonId, "Season");
            var requestId = seasonId.Split(':').Last();
            var url = $"https://lmt.fn.sportradar.com/common/en/Etc:UTC/gismo/stats_season_meta/{requestId}{TokenService.TokenService.ApiToken}";
            var response = await MakeRequestWithRetryAsync(url);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<IResult> GetSeasonGoalsAsync(string seasonId)
    {
        try
        {
            ValidateId(seasonId, "Season");
            var requestId = seasonId.Split(':').Last();
            var url = $"https://lmt.fn.sportradar.com/common/en/Etc:UTC/gismo/stats_season_goals/{requestId}{TokenService.TokenService.ApiToken}";
            var response = await MakeRequestWithRetryAsync(url);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<IResult> GetSeasonTopCardsAsync(string seasonId)
    {
        try
        {
            ValidateId(seasonId, "Season");
            var requestId = seasonId.Split(':').Last();
            var url = $"https://lmt.fn.sportradar.com/common/en/Etc:UTC/gismo/stats_season_topcards/{requestId}{TokenService.TokenService.ApiToken}";
            var response = await MakeRequestWithRetryAsync(url);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<IResult> GetTeamLastXAsync(string teamId, int count = 5)
    {
        try
        {
            ValidateId(teamId, "Team");
            var url = $"https://widgets.fn.sportradar.com/common/en/Etc:UTC/gismo/stats_team_lastx/{teamId}/{count}{TokenService.TokenService.ApiToken}";
            var response = await MakeRequestWithRetryAsync(url);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<IResult> GetTeamLastXExtendedAsync(string teamId)
    {
        try
        {
            ValidateId(teamId, "Team");
            var url = $"https://widgets.fn.sportradar.com/common/en/Etc:UTC/gismo/stats_team_lastxextended/{teamId}{TokenService.TokenService.ApiToken}";
            var response = await MakeRequestWithRetryAsync(url);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<IResult> GetMatchPhrasesDeltaAsync(string matchId)
    {
        try
        {
            ValidateId(matchId, "Match");
            var url = $"https://lmt.fn.sportradar.com/common/en/Etc:UTC/gismo/match_phrasesdelta/{matchId}{TokenService.TokenService.ApiToken}";
            var response = await MakeRequestWithRetryAsync(url);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }
    
    public async Task<IResult> GetTeamNextXAsync(string teamId, int count = 5)
    {
        try
        {
            ValidateId(teamId, "Team");
            var url = $"https://widgets.fn.sportradar.com/common/en/Etc:UTC/gismo/stats_team_nextx/{teamId}/{count}{TokenService.TokenService.ApiToken}";
            var response = await MakeRequestWithRetryAsync(url);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<IResult> GetTeamVersusRecentAsync(string teamId1, string teamId2, int count = 10)
    {
        try
        {
            ValidateId(teamId1, "Team 1");
            ValidateId(teamId2, "Team 2");
            var url = $"https://widgets.fn.sportradar.com/common/en/Etc:UTC/gismo/stats_team_versusrecent/{teamId1}/{teamId2}/{count}{TokenService.TokenService.ApiToken}";
            var response = await MakeRequestWithRetryAsync(url);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<IResult> GetMatchInsightsAsync(string matchId)
    {
        try
        {
            ValidateId(matchId, "Match");
            var url = $"https://widgets.fn.sportradar.com/demolmt/en/Etc:UTC/gismo/match_insights/{matchId}{TokenService.TokenService.ApiToken}";
            var response = await MakeRequestWithRetryAsync(url);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<IResult> GetSportMatchesAsync(string date)
    {
        try
        {
            if (!DateTime.TryParse(date, out _))
            {
                throw new ArgumentException("Invalid date format");
            }

            var url = $"https://lmt.fn.sportradar.com/demolmt/en/Etc:UTC/gismo/sport_matches/1/{date}{TokenService.TokenService.ApiToken}";
            var response = await MakeRequestWithRetryAsync(url);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<IResult> GetLiveMatchesAsync()
    {
        try
        {
            var response = await MakeRequestWithRetryAsync(
                $"https://lmt.fn.sportradar.com/demolmt/en/Etc:UTC/gismo/sport_matches/1/{DateTime.Today:yyyy-MM-dd}/1{TokenService.TokenService.ApiToken}");
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<IResult> GetMatchSelectAsync(int leagueId, int type = 1)
    {
        try
        {
            var url = $"https://widgets.fn.sportradar.com/betsrotw/en/Etc:UTC/gismo/match_select/{leagueId}/{type}{TokenService.TokenService.ApiToken}";
            var response = await MakeRequestWithRetryAsync(url);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public async Task<IResult> GetSeasonTablesAsync(string seasonId)
    {
        try
        {
            ValidateId(seasonId, "Season");
            var requestId = seasonId.Split(':').Last();
            var url = $"https://widgets.fn.sportradar.com/common/en/Etc:UTC/gismo/stats_season_tables/{requestId}{TokenService.TokenService.ApiToken}";
            var response = await MakeRequestWithRetryAsync(url);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    private async Task<JsonDocument?> MakeRequestWithRetryAsync(string url, int maxRetries = 3, int timeout = 20000)
    {
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            SslOptions = new SslClientAuthenticationOptions
            {
                // Enable TLS 1.2 and 1.3
                EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13,
            },
    
            // Adjust connection timeout if needed
            ConnectTimeout = TimeSpan.FromSeconds(timeout),
    
            // Keep connections alive
            KeepAlivePingPolicy = HttpKeepAlivePingPolicy.WithActiveRequests,
            KeepAlivePingDelay = TimeSpan.FromSeconds(timeout),
            KeepAlivePingTimeout = TimeSpan.FromSeconds(timeout),
        };
        
        handler.MaxConnectionsPerServer = 20;
        handler.PooledConnectionLifetime = TimeSpan.FromMinutes(2);
        handler.PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1);

        using var client = new HttpClient(handler);

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);

                // Add headers
                request.Headers.TryAddWithoutValidation("accept", "*/*");
                request.Headers.TryAddWithoutValidation("accept-language", "en-US,en;q=0.9");
                request.Headers.TryAddWithoutValidation("if-modified-since", "Mon, 04 Nov 2024 01:22:44 GMT");
                request.Headers.TryAddWithoutValidation("if-none-match",
                    $"\"{DateTime.UtcNow.Ticks}\"");
                request.Headers.TryAddWithoutValidation("origin", "https://www.sportybet.com");
                request.Headers.TryAddWithoutValidation("priority", "u=1, i");
                request.Headers.TryAddWithoutValidation("referer", "https://www.sportybet.com/");
                request.Headers.TryAddWithoutValidation("sec-ch-ua",
                    "\"Chromium\";v=\"130\", \"Google Chrome\";v=\"130\", \"Not?A_Brand\";v=\"99\"");
                request.Headers.TryAddWithoutValidation("sec-ch-ua-mobile", "?0");
                request.Headers.TryAddWithoutValidation("sec-ch-ua-platform", "\"macOS\"");
                request.Headers.TryAddWithoutValidation("sec-fetch-dest", "empty");
                request.Headers.TryAddWithoutValidation("sec-fetch-mode", "cors");
                request.Headers.TryAddWithoutValidation("sec-fetch-site", "cross-site");
                request.Headers.TryAddWithoutValidation("user-agent",
                    "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36");

                var response = await client.SendAsync(request);
                if (response.StatusCode == HttpStatusCode.NotModified)
                {
                    _logger.LogInformation("Response not modified.");
                    return null;
                }

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Request failed. Status code: {StatusCode}, Content: {Content}",
                        response.StatusCode, errorContent);
                    continue;
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonDocument.Parse(content);
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex, "Attempt {Attempt} of {MaxRetries} failed", attempt, maxRetries);
                // Use different backoff times based on the endpoint type
                var backoffTime = timeout <= 3000 ? 300 : 1000;
                await Task.Delay(backoffTime * attempt);
            }
        }

        throw new HttpRequestException($"Request failed after {maxRetries} attempts.");
    }

    private static void ValidateId(string? id, string type)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException($"{type} ID is required");
        }
    }

    private IResult HandleError(Exception ex)
    {
        _logger.LogError(ex, "API request failed");

        return ex switch
        {
            ArgumentException => Results.BadRequest(new { error = ex.Message }),
            HttpRequestException => Results.StatusCode(503),
            _ => Results.StatusCode(500)
        };
    }
}