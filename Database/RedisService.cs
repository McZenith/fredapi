using System.Text.Json;
using fredapi.Model.ApiResponse;
using StackExchange.Redis;

namespace fredapi.Database;

public interface IRedisService
{
    Task<T?> GetDataAsync<T>(string key) where T : class;
    Task<string?> GetStringAsync(string key);
    Task<bool> SetDataAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class;
    Task<bool> SetStringAsync(string key, string value, TimeSpan? expiry = null);
    Task SaveMatchesDataAsync(List<Event> matches);
}

public class RedisService : IRedisService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly ILogger<RedisService> _logger;

    public RedisService(IConfiguration configuration, ILogger<RedisService> logger)
    {
        _logger = logger;

        var redisHost = configuration["Redis:Host"];
        var redisPort = configuration["Redis:Port"];
        var redisPassword = configuration["Redis:Password"];

        if (string.IsNullOrEmpty(redisHost) || string.IsNullOrEmpty(redisPort) || string.IsNullOrEmpty(redisPassword))
        {
            throw new ArgumentException("Redis configuration is missing required values");
        }

        var options = new ConfigurationOptions
        {
            EndPoints = { $"{redisHost}:{redisPort}" },
            Password = redisPassword,
            AbortOnConnectFail = false
        };

        _redis = ConnectionMultiplexer.Connect(options);
        _db = _redis.GetDatabase();
    }

    public async Task<T?> GetDataAsync<T>(string key) where T : class
    {
        try
        {
            var value = await _db.StringGetAsync(key);
            if (!value.HasValue)
                return null;

            return JsonSerializer.Deserialize<T>(value!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting data from Redis for key: {Key}", key);
            throw;
        }
    }

    public async Task<string?> GetStringAsync(string key)
    {
        try
        {
            var value = await _db.StringGetAsync(key);
            return value.HasValue ? value.ToString() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting string from Redis for key: {Key}", key);
            throw;
        }
    }

    public async Task<bool> SetDataAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
    {
        try
        {
            var serializedValue = JsonSerializer.Serialize(value);
            return await _db.StringSetAsync(key, serializedValue, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting data in Redis for key: {Key}", key);
            throw;
        }
    }

    public async Task<bool> SetStringAsync(string key, string value, TimeSpan? expiry = null)
    {
        try
        {
            return await _db.StringSetAsync(key, value, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting string in Redis for key: {Key}", key);
            throw;
        }
    }

    public Task SaveMatchesDataAsync(List<Event> matches)
    {
        var db = _redis.GetDatabase();
        ITransaction transaction = db.CreateTransaction();
    
        // Create a pipeline
        var pipeline = db.CreateBatch();
        try 
        {
            foreach (var match in matches)
            {
                var matchDate = DateTimeOffset.FromUnixTimeMilliseconds(match.EstimateStartTime)
                    .UtcDateTime.ToString("yyyy-MM-dd");
                var key = $"match:{matchDate}:{match.EventId}";
                var value = JsonSerializer.Serialize(match);

                // Add commands to pipeline without awaiting
                pipeline.StringSetAsync(key, value);
                pipeline.KeyExpireAsync(key, TimeSpan.FromHours(25));
            }
        
            // Execute all commands in pipeline at once
            pipeline.Execute();
        
            _logger.LogInformation("Successfully cached {count} matches", matches.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while saving matches to Redis");
            throw;
        }

        return Task.CompletedTask;
    }
}