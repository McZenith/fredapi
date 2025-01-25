using System.Text.RegularExpressions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.DevTools;
using OpenQA.Selenium.Support.UI;
using DevToolsSessionDomains = OpenQA.Selenium.DevTools.V131.DevToolsSessionDomains;


namespace fredapi.SportRadarService.TokenService;

public interface ISportRadarTokenService
{
    Task<string> ExtractAuthTokenAsync();
}

public class SportRadarTokenService : ISportRadarTokenService
{
    private readonly IWebDriver _driver;
    private readonly ILogger<SportRadarTokenService> _logger;
    private bool _disposed;

    public SportRadarTokenService(ILogger<SportRadarTokenService> logger)
    {
        _logger = logger;
        
        var options = new ChromeOptions();
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--headless");
        // Additional options to better emulate a real browser
        options.AddArgument("--window-size=1920,1080");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--ignore-certificate-errors");
        options.AddArgument("--enable-javascript");
        // Set a proper user agent
        options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        
        _driver = new ChromeDriver(options);
    }

    public async Task<string> ExtractAuthTokenAsync()
    {
        try
        {
            var url = "https://www.sportybet.com/ng/sport/football/live/Argentina/Primera_LFP/CA_Belgrano_vs_CA_Huracan/sr:match:56742701";
            var taskCompletionSource = new TaskCompletionSource<string>();
            var timeoutTask = Task.Delay(15000); // Increased timeout for headless mode

            // Get DevTools session
            IDevTools devTools = (IDevTools)_driver;
            var session = devTools.GetDevToolsSession();
            var domains = new DevToolsSessionDomains(session);
            
            // Enable network tracking
            await domains.Network.Enable(new OpenQA.Selenium.DevTools.V131.Network.EnableCommandSettings());

            // Subscribe to network events
            domains.Network.RequestWillBeSent += (_, e) =>
            {
                if (e.Request.Url.StartsWith("https://lmt.fn.sportradar.com/common/"))
                {
                    var match = Regex.Match(e.Request.Url, @"\?T=(.*)$");
                    if (match.Success)
                    {
                        taskCompletionSource.TrySetResult("?T=" + match.Groups[1].Value);
                    }
                }
            };

            // Navigate to the page
            await _driver.Navigate().GoToUrlAsync(url);

            // Wait for page load
            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
            wait.Until(driver => ((IJavaScriptExecutor)driver)
                .ExecuteScript("return document.readyState").Equals("complete"));

            // Execute some scrolling to trigger dynamic content loading
            ((IJavaScriptExecutor)_driver).ExecuteScript("window.scrollTo(0, document.body.scrollHeight/2);");
            await Task.Delay(1000);
            ((IJavaScriptExecutor)_driver).ExecuteScript("window.scrollTo(0, 0);");

            // Wait for either success or timeout
            var completedTask = await Task.WhenAny(taskCompletionSource.Task, timeoutTask);
            
            if (completedTask == timeoutTask)
            {
                throw new Exception("No SportRadar API call found in network traffic after timeout");
            }

            return await taskCompletionSource.Task;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting SportRadar auth token");
            throw;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _driver.Quit();
            _driver.Dispose();
            _disposed = true;
        }
    }
}