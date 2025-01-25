namespace fredapi.Database;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Bson;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
}

public class MongoDbService
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<MongoDbService> _logger;
    private readonly MongoClient _client;
    private readonly string _databaseName;

    public MongoDbService(IOptions<MongoDbSettings> mongoDBSettings, ILogger<MongoDbService> logger)
    {
        _logger = logger;
        _databaseName = mongoDBSettings.Value.DatabaseName;

        try
        {
            _logger.LogInformation("Attempting to connect to MongoDB...");
            _client = new MongoClient(mongoDBSettings.Value.ConnectionString);
            _database = _client.GetDatabase(_databaseName);

            // Verify connection by pinging the database
            _database.RunCommand((Command<BsonDocument>)"{ping:1}");
            
            _logger.LogInformation("Successfully connected to MongoDB database: {DatabaseName}", _databaseName);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to connect to MongoDB: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    public IMongoCollection<T> GetCollection<T>(string collectionName)
    {
        try
        {
            _logger.LogInformation("Accessing collection: {CollectionName}", collectionName);
            var collection = _database.GetCollection<T>(collectionName);
            return collection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accessing collection {CollectionName}: {ErrorMessage}", 
                collectionName, ex.Message);
            throw;
        }
    }

    public async Task<bool> IsConnected()
    {
        try
        {
            await _database.RunCommandAsync((Command<BsonDocument>)"{ping:1}");
            return true;
        }
        catch
        {
            return false;
        }
    }
}