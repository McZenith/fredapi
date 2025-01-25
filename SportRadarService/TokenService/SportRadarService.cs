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

public class SportRadarTokenService : ISportRadarTokenService, IDisposable
{
    private readonly IWebDriver _driver;
    private readonly ILogger<SportRadarTokenService> _logger;
    private bool _disposed;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private const int TIMEOUT_SECONDS = 30;

    public SportRadarTokenService(ILogger<SportRadarTokenService> logger)
    {
        _logger = logger;

        var options = new ChromeOptions();
        options.AddArguments(
            "--no-sandbox",
            "--disable-dev-shm-usage",
            "--headless=new",  // Using new headless mode
            "--window-size=1920,1080",
            "--disable-gpu",
            "--ignore-certificate-errors",
            "--enable-javascript",
            "--disable-extensions",
            "--disable-setuid-sandbox",
            "--disable-web-security",
            "--blink-settings=imagesEnabled=false",
            "--memory-pressure-off",
            "--js-flags=--max-old-space-size=128",
            "--disable-background-networking",
            "--disable-background-timer-throttling",
            "--disable-backgrounding-occluded-windows",
            "--disable-breakpad",
            "--disable-client-side-phishing-detection",
            "--disable-default-apps",
            "--disable-dev-shm-usage",
            "--disable-features=TranslateUI",
            "--disable-hang-monitor",
            "--disable-ipc-flooding-protection",
            "--disable-popup-blocking",
            "--disable-prompt-on-repost",
            "--disable-renderer-backgrounding",
            "--force-color-profile=srgb",
            "--metrics-recording-only",
            "--no-first-run",
            "--password-store=basic",
            "--use-mock-keychain",
            "--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
        );

        options.PageLoadStrategy = PageLoadStrategy.Eager;
        
        var service = ChromeDriverService.CreateDefaultService();
        service.HideCommandPromptWindow = true;

        _driver = new ChromeDriver(service, options);
        _driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(TIMEOUT_SECONDS);
        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(TIMEOUT_SECONDS);
    }

    public async Task<string> ExtractAuthTokenAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            var urls = new[]
            {
                "https://www.sportybet.com/ng/sport/football/live/Argentina/Primera_LFP/CA_Belgrano_vs_CA_Huracan/sr:match:56742701",
                "https://www.sportybet.com/ng/sport/football/live/Italy/Serie_C,_Group_C/Potenza_Calcio_vs_Audace_Cerignola/sr:match:51548795?navigatedFrom=live"
            };

            foreach (var url in urls)
            {
                try
                {
                    var token = await ExtractTokenFromUrl(url);
                    if (!string.IsNullOrEmpty(token))
                    {
                        return token;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Failed to extract token from {url}, trying next URL");
                    continue;
                }
            }

            throw new Exception("Failed to extract token from all URLs");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<string> ExtractTokenFromUrl(string url)
    {
        var taskCompletionSource = new TaskCompletionSource<string>();
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(TIMEOUT_SECONDS));

        try
        {
            // Get DevTools session
            IDevTools devTools = (IDevTools)_driver;
            var session = devTools.GetDevToolsSession();
            var domains = new DevToolsSessionDomains(session);
            
            // Enable network tracking
            await domains.Network.Enable(new OpenQA.Selenium.DevTools.V131.Network.EnableCommandSettings());

            // Subscribe to network events
            domains.Network.RequestWillBeSent += (_, e) =>
            {
                if (e.Request.Url.Contains("sportradar.com"))
                {
                    var match = Regex.Match(e.Request.Url, @"[\?&](?:t|T)=([^&]+)");
                    if (match.Success)
                    {
                        taskCompletionSource.TrySetResult(match.Groups[1].Value);
                    }
                }
            };

            // Navigate and wait for page load
            await _driver.Navigate().GoToUrlAsync(url);

            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(TIMEOUT_SECONDS));
            wait.Until(driver => ((IJavaScriptExecutor)driver)
                .ExecuteScript("return document.readyState").Equals("complete"));

            // Trigger dynamic content loading
            var jsExecutor = (IJavaScriptExecutor)_driver;
            jsExecutor.ExecuteScript(@"
                window.scrollTo(0, document.body.scrollHeight/2);
                setTimeout(() => window.scrollTo(0, 0), 500);
            ");

            // Wait for either success or timeout
            var completedTask = await Task.WhenAny(taskCompletionSource.Task, timeoutTask);
            
            if (completedTask == timeoutTask)
            {
                throw new TimeoutException("No SportRadar API call found in network traffic");
            }

            var token = await taskCompletionSource.Task;
            return !string.IsNullOrEmpty(token) ? "?T=" + token : throw new Exception("Empty token extracted");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error extracting SportRadar auth token from {url}");
            throw;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _driver?.Quit();
            _driver?.Dispose();
            _semaphore?.Dispose();
            _disposed = true;
        }
    }

    ~SportRadarTokenService()
    {
        Dispose();
    }
}