using fredapi.SportRadarService.Background.ArbitrageLiveMatchBackgroundService;

namespace fredapi.Utils;

public class MarketValidator
{
    private readonly ILogger<dynamic> _logger;
    
    public MarketValidator(ILogger<dynamic> logger)
    {
        _logger = logger;
    }

    public bool ValidateMarket(MarketData market, int? outcomeCount = null)
{
    try
    {
        if (!ValidateBasicStructure(market))
            return false;

        var actualOutcomeCount = outcomeCount ?? market.Outcomes.Count;
        
        return market.Desc?.ToLower() switch
        {
            // Match Outcomes
            var m when m.Contains("1x2") => ValidateMatchOutcomes(market),
            var m when m.Contains("double chance") => ValidateDoubleChance(market),
            var m when m.Contains("draw no bet") => ValidateDrawNoBet(market),
            
            // Goals Markets
            var m when m.Contains("over/under") => ValidateOverUnder(market),
            var m when m.Contains("team total goals") => ValidateTeamTotalGoals(market),
            
            // Both Teams Markets
            var m when m.Contains("gg/ng") || m.Contains("btts") => ValidateBothTeamsToScore(market),
            
            // Half Markets
            var m when m.Contains("1st half") || m.Contains("2nd half") => ValidateHalfMarket(market),
            
            // Next Goal Markets - Updated matching
            var m when (m.EndsWith("goal") || m.Contains("goalscorer")) && 
                      market.Specifier?.StartsWith("goalnr=") == true => ValidateNextGoal(market),
            
            // Combo Markets
            var m when m.Contains("1x2 & gg/ng") => Validate1X2AndBTTS(market),
            var m when m.Contains("1x2 & over/under") => Validate1X2AndOverUnder(market),
            
            // Asian Markets
            var m when m.Contains("asian handicap") => ValidateAsianHandicap(market),
            var m when m.Contains("asian total") => ValidateAsianTotal(market),
            
            // Corner Markets
            var m when m.Contains("corner match") => ValidateCornerMatch(market),
            var m when m.Contains("total corners") => ValidateTotalCorners(market),
            
            _ => false
        };
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Error validating market {MarketId}", market.Id);
        return false;
    }
}
    private bool ValidateBasicStructure(MarketData market)
{
    if (market == null || 
        string.IsNullOrEmpty(market.Desc) || 
        market.Outcomes == null)
        return false;

    // First filter for active outcomes
    var activeOutcomes = market.Outcomes
        .Where(o => o != null && 
                    o.IsActive == 1 && 
                    !string.IsNullOrEmpty(o.Id) && 
                    !string.IsNullOrEmpty(o.Desc) && 
                    !string.IsNullOrEmpty(o.Odds) &&
                    decimal.TryParse(o.Odds, out var odds) && 
                    odds > 1.0m)
        .ToList();

    // If no active outcomes or market is suspended/settled
    if (!activeOutcomes.Any() || market.Status == 2 || market.Status == 3)
        return false;

    // Store active outcomes back to market
    market.Outcomes = activeOutcomes;
    return true;
}

private bool ValidateMatchOutcomes(MarketData market)
{
    // Must have exactly 3 active outcomes
    if (market.Outcomes.Count != 3)
        return false;

    var descriptions = market.Outcomes.Select(o => o.Desc.ToLower()).ToList();
    
    // Must have all three required outcomes
    if (!descriptions.Contains("home") || 
        !descriptions.Contains("draw") || 
        !descriptions.Contains("away"))
        return false;

    // All outcomes must have valid odds
    return market.Outcomes.All(o => 
        decimal.TryParse(o.Odds, out var odds) && odds > 1.0m);
}

private bool ValidateOverUnder(MarketData market)
{
    // Must have exactly 2 active outcomes
    if (market.Outcomes.Count != 2)
        return false;

    // Must have valid total specifier
    if (string.IsNullOrEmpty(market.Specifier) || 
        !market.Specifier.StartsWith("total=") || 
        !decimal.TryParse(market.Specifier.Substring(6), out var total))
        return false;

    var descriptions = market.Outcomes.Select(o => o.Desc.ToLower()).ToList();
    
    // Must have both over and under
    var hasOver = descriptions.Any(d => d.Contains("over"));
    var hasUnder = descriptions.Any(d => d.Contains("under"));
    
    if (!hasOver || !hasUnder)
        return false;

    // All outcomes must have valid odds
    return market.Outcomes.All(o => 
        decimal.TryParse(o.Odds, out var odds) && odds > 1.0m);
}

private bool ValidateNextGoal(MarketData market)
{
    // Basic validation
    if (market.Outcomes == null || !market.Outcomes.Any())
        return false;

    // Must have valid goal number specifier
    if (string.IsNullOrEmpty(market.Specifier) || 
        !market.Specifier.StartsWith("goalnr=") || 
        !int.TryParse(market.Specifier.Substring(7), out var goalNumber))
        return false;

    var descriptions = market.Outcomes.Select(o => o.Desc.ToLower()).ToList();
    
    // Check if it's first half (H1)
    bool isFirstHalf = true;
    
    if (isFirstHalf)
    {
        // For first half, only consider home and away
        var activeOutcomes = market.Outcomes
            .Where(o => o.IsActive == 1 && 
                       !o.Desc.ToLower().Contains("none") &&
                       decimal.TryParse(o.Odds, out var odds) && 
                       odds > 1.0m)
            .ToList();

        // Must have exactly 2 outcomes (home and away)
        if (activeOutcomes.Count != 2)
            return false;

        var activeDescriptions = activeOutcomes.Select(o => o.Desc.ToLower()).ToList();
        
        // Must have both home and away
        if (!activeDescriptions.Contains("home") || 
            !activeDescriptions.Contains("away"))
            return false;

        // Update the market outcomes to only include home and away
        market.Outcomes = activeOutcomes;
        return true;
    }
    else
    {
        // For other periods, use standard validation with all three outcomes
        if (market.Outcomes.Count != 3)
            return false;

        // Must have all three required outcomes
        if (!descriptions.Contains("home") || 
            !descriptions.Contains("none") || 
            !descriptions.Contains("away"))
            return false;

        // All outcomes must have valid odds
        return market.Outcomes.All(o => 
            decimal.TryParse(o.Odds, out var odds) && odds > 1.0m);
    }
}
private bool Validate1X2AndBTTS(MarketData market)
{
    // Must have exactly 6 active outcomes
    if (market.Outcomes.Count != 6)
        return false;

    var descriptions = market.Outcomes.Select(o => o.Desc.ToLower()).ToList();
    
    // Must have all required combinations
    var requiredOutcomes = new[]
    {
        "home & yes", "home & no",
        "draw & yes", "draw & no",
        "away & yes", "away & no"
    };

    if (!requiredOutcomes.All(description => descriptions.Contains(description)))
        return false;

    // All outcomes must have valid odds
    return market.Outcomes.All(o => 
        decimal.TryParse(o.Odds, out var odds) && odds > 1.0m);
}

private bool ValidateHalfMarket(MarketData market)
{
    var marketDesc = market.Desc.ToLower();
    
    if (marketDesc.Contains("correct score"))
    {
        // All outcomes must be valid scores or "other"
        return market.Outcomes.All(o => 
            o.Desc.Contains(":") || 
            o.Desc.ToLower() == "other") &&
            market.Outcomes.All(o => 
                decimal.TryParse(o.Odds, out var odds) && odds > 1.0m);
    }
    
    if (marketDesc.Contains("result"))
    {
        // Must have exactly 3 active outcomes
        if (market.Outcomes.Count != 3)
            return false;

        var descriptions = market.Outcomes.Select(o => o.Desc.ToLower()).ToList();
        
        // Must have home, draw, away
        if (!descriptions.Contains("home") || 
            !descriptions.Contains("draw") || 
            !descriptions.Contains("away"))
            return false;

        // All outcomes must have valid odds
        return market.Outcomes.All(o => 
            decimal.TryParse(o.Odds, out var odds) && odds > 1.0m);
    }

    return false;
}


    private bool ValidateDoubleChance(MarketData market)
    {
        if (market.Outcomes.Count != 3)
            return false;

        var descriptions = market.Outcomes.Select(o => o.Desc.ToLower()).ToList();
        return descriptions.Contains("home or draw") && 
               descriptions.Contains("home or away") && 
               descriptions.Contains("draw or away");
    }

    private bool ValidateDrawNoBet(MarketData market)
    {
        if (market.Outcomes.Count != 2)
            return false;

        var descriptions = market.Outcomes.Select(o => o.Desc.ToLower()).ToList();
        return descriptions.Contains("home") && 
               descriptions.Contains("away");
    }

 

    private bool ValidateTeamTotalGoals(MarketData market)
    {
        if (market.Outcomes.Count != 2 || string.IsNullOrEmpty(market.Specifier))
            return false;

        if (!market.Specifier.StartsWith("total=") || 
            !decimal.TryParse(market.Specifier.Substring(6), out var total))
            return false;

        var descriptions = market.Outcomes.Select(o => o.Desc.ToLower()).ToList();
        return descriptions.Any(d => d.Contains("over")) && 
               descriptions.Any(d => d.Contains("under"));
    }

    private bool ValidateBothTeamsToScore(MarketData market)
    {
        if (market.Outcomes.Count != 2)
            return false;

        var descriptions = market.Outcomes.Select(o => o.Desc.ToLower()).ToList();
        return descriptions.Contains("yes") && descriptions.Contains("no");
    }

    private bool Validate1X2AndOverUnder(MarketData market)
    {
        if (market.Outcomes.Count != 6 || string.IsNullOrEmpty(market.Specifier))
            return false;

        if (!market.Specifier.StartsWith("total=") || 
            !decimal.TryParse(market.Specifier.Substring(6), out var total))
            return false;

        var descriptions = market.Outcomes.Select(o => o.Desc.ToLower()).ToList();
        return descriptions.Contains("home & over") &&
               descriptions.Contains("home & under") &&
               descriptions.Contains("draw & over") &&
               descriptions.Contains("draw & under") &&
               descriptions.Contains("away & over") &&
               descriptions.Contains("away & under");
    }

    private bool ValidateAsianHandicap(MarketData market) =>
        market.Outcomes.Count == 2 && !string.IsNullOrEmpty(market.Specifier);

    private bool ValidateAsianTotal(MarketData market) =>
        market.Outcomes.Count == 2 && !string.IsNullOrEmpty(market.Specifier);

    private bool ValidateCornerMatch(MarketData market) =>
        market.Outcomes.Count == 3;

    private bool ValidateTotalCorners(MarketData market) =>
        market.Outcomes.Count == 2 && !string.IsNullOrEmpty(market.Specifier);
}