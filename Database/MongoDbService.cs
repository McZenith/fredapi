namespace fredapi.Database;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
}

public class MongoDbService
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<MongoDbService> _logger;
    private readonly MongoDbSettings _mongoDBSettings;
    private readonly MongoClient _mongoClient;

    public MongoDbService(IOptions<MongoDbSettings> mongoDBSettings, ILogger<MongoDbService> logger)
    {
        _mongoDBSettings = mongoDBSettings.Value;
        _logger = logger;

        try
        {
            _logger.LogInformation("Attempting to connect to MongoDB...");

            // Configure MongoDB client settings with retry options
            var settings = MongoClientSettings.FromConnectionString(_mongoDBSettings.ConnectionString);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(60);
            settings.ConnectTimeout = TimeSpan.FromSeconds(60);
            settings.SocketTimeout = TimeSpan.FromSeconds(120);
            settings.MaxConnectionPoolSize = 200;
            settings.RetryReads = true;
            settings.RetryWrites = true;
            settings.WaitQueueSize = 500;
            settings.WaitQueueTimeout = TimeSpan.FromSeconds(60);
            settings.MaxConnectionIdleTime = TimeSpan.FromMinutes(5);

            // Connect to MongoDB
            _mongoClient = new MongoClient(settings);

            // Test the connection by getting the database
            _database = _mongoClient.GetDatabase(_mongoDBSettings.DatabaseName);

            // Run a simple command to verify the connection works
            var result = _database.RunCommand<BsonDocument>(new BsonDocument("ping", 1));

            _logger.LogInformation($"Successfully connected to MongoDB database: {_mongoDBSettings.DatabaseName}");
        }
        catch (MongoConnectionException ex)
        {
            _logger.LogError(ex, $"Failed to connect to MongoDB: {ex.Message}");

            if (ex.InnerException != null)
            {
                _logger.LogError($"Inner exception: {ex.InnerException.Message}");
            }

            throw; // Re-throw to allow the caller to handle it
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An error occurred while initializing MongoDB connection: {ex.Message}");
            throw; // Re-throw to allow the caller to handle it
        }
    }

    public IMongoCollection<T> GetCollection<T>(string name)
    {
        _logger.LogInformation("Accessing collection: {Name}", name);
        return _database.GetCollection<T>(name);
    }

    public async Task<List<T>> GetAllAsync<T>(string collectionName)
    {
        var collection = _database.GetCollection<T>(collectionName);
        return await collection.FindWithDiskUse(new BsonDocument()).ToListAsync();
    }

    public async Task<T> GetByIdAsync<T>(string collectionName, string id)
    {
        var collection = _database.GetCollection<T>(collectionName);
        var filter = Builders<T>.Filter.Eq("Id", id);
        return await collection.FindWithDiskUse(filter).FirstOrDefaultAsync();
    }

    public async Task CreateAsync<T>(string collectionName, T document)
    {
        var collection = _database.GetCollection<T>(collectionName);
        await collection.InsertOneAsync(document);
    }

    public async Task UpdateAsync<T>(string collectionName, string id, T document)
    {
        var collection = _database.GetCollection<T>(collectionName);
        var filter = Builders<T>.Filter.Eq("Id", id);
        await collection.ReplaceOneAsync(filter, document);
    }

    public async Task DeleteAsync<T>(string collectionName, string id)
    {
        var collection = _database.GetCollection<T>(collectionName);
        var filter = Builders<T>.Filter.Eq("Id", id);
        await collection.DeleteOneAsync(filter);
    }

    public async Task CreateTTLIndexAsync<T>(string collectionName, string fieldName, TimeSpan expireAfter)
    {
        var collection = _database.GetCollection<T>(collectionName);

        // Check if index already exists
        using var cursor = await collection.Indexes.ListAsync();
        var indexes = await cursor.ToListAsync();

        var indexExists = indexes.Any(index =>
            index["name"].AsString.Contains(fieldName) &&
            index.Contains("expireAfterSeconds")
        );

        if (!indexExists)
        {
            var indexKeysDefinition = Builders<T>.IndexKeys.Ascending(fieldName);
            var indexOptions = new CreateIndexOptions
            {
                ExpireAfter = expireAfter,
                Background = true
            };
            var indexModel = new CreateIndexModel<T>(indexKeysDefinition, indexOptions);
            await collection.Indexes.CreateOneAsync(indexModel);

            _logger.LogInformation("Created TTL index on {FieldName} with expiration after {ExpireAfter}",
                fieldName, expireAfter);
        }
        else
        {
            _logger.LogInformation("TTL index already exists on {FieldName}", fieldName);
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

    public async Task CreateIndexAsync<T>(string collectionName, string fieldName, bool descending = false)
    {
        try
        {
            var collection = _database.GetCollection<T>(collectionName);

            // Check if index already exists
            using var cursor = await collection.Indexes.ListAsync();
            var indexes = await cursor.ToListAsync();
            var indexExists = indexes.Any(index => index["name"].AsString.Contains(fieldName));

            if (!indexExists)
            {
                var indexKeysDefinition = descending
                    ? Builders<T>.IndexKeys.Descending(fieldName)
                    : Builders<T>.IndexKeys.Ascending(fieldName);

                var indexOptions = new CreateIndexOptions
                {
                    Background = true,
                    Name = $"{fieldName}_{(descending ? "desc" : "asc")}_index"
                };

                var indexModel = new CreateIndexModel<T>(indexKeysDefinition, indexOptions);
                await collection.Indexes.CreateOneAsync(indexModel);

                _logger.LogInformation("Created index on {FieldName} ({Direction}) for collection {CollectionName}",
                    fieldName, descending ? "descending" : "ascending", collectionName);
            }
            else
            {
                _logger.LogInformation("Index on {FieldName} already exists for collection {CollectionName}",
                    fieldName, collectionName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating index on {FieldName} for collection {CollectionName}",
                fieldName, collectionName);
        }
    }
}