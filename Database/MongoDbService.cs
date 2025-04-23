using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace fredapi.Database;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public int ConnectionTimeout { get; set; } = 60;
    public int OperationTimeout { get; set; } = 120;
    public int MaxConnectionPoolSize { get; set; } = 200;
    public int WaitQueueSize { get; set; } = 500;
}

public class MongoDbService
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<MongoDbService> _logger;
    private readonly MongoClient _mongoClient;
    
    // Cache for collections to avoid repeated GetCollection calls
    private readonly Dictionary<string, object> _collectionCache = new();

    public MongoDbService(IOptions<MongoDbSettings> mongoDBSettings, ILogger<MongoDbService> logger)
    {
        var settings = mongoDBSettings.Value;
        _logger = logger;

        try
        {
            _logger.LogInformation("Initializing MongoDB connection...");

            // Configure MongoDB client settings with optimized options
            var clientSettings = MongoClientSettings.FromConnectionString(settings.ConnectionString);
            clientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(settings.ConnectionTimeout);
            clientSettings.ConnectTimeout = TimeSpan.FromSeconds(settings.ConnectionTimeout);
            clientSettings.SocketTimeout = TimeSpan.FromSeconds(settings.OperationTimeout);
            clientSettings.MaxConnectionPoolSize = settings.MaxConnectionPoolSize;
            clientSettings.RetryReads = true;
            clientSettings.RetryWrites = true;
            clientSettings.WaitQueueSize = settings.WaitQueueSize;
            clientSettings.WaitQueueTimeout = TimeSpan.FromSeconds(settings.ConnectionTimeout);
            clientSettings.MaxConnectionIdleTime = TimeSpan.FromMinutes(5);
            
            // Connect to MongoDB
            _mongoClient = new MongoClient(clientSettings);
            _database = _mongoClient.GetDatabase(settings.DatabaseName);

            // Verify connection
            var result = _database.RunCommand<BsonDocument>(new BsonDocument("ping", 1));
            _logger.LogInformation("Successfully connected to MongoDB database: {Database}", settings.DatabaseName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MongoDB connection failed: {Message}", ex.Message);
            if (ex.InnerException != null)
            {
                _logger.LogError("Inner exception: {Message}", ex.InnerException.Message);
            }
            throw;
        }
    }

    public IMongoCollection<T> GetCollection<T>(string name)
    {
        // Use cached collection if available
        var cacheKey = $"{name}:{typeof(T).Name}";
        
        if (!_collectionCache.TryGetValue(cacheKey, out var cachedCollection))
        {
            _logger.LogDebug("Accessing collection: {Name} for type {Type}", name, typeof(T).Name);
            cachedCollection = _database.GetCollection<T>(name);
            _collectionCache[cacheKey] = cachedCollection;
        }
        
        return (IMongoCollection<T>)cachedCollection;
    }

    public async Task<List<T>> GetAllAsync<T>(string collectionName, int? limit = 1000)
    {
        var collection = GetCollection<T>(collectionName);
        return await collection.FindWithDiskUse(
            Builders<T>.Filter.Empty, 
            limit, 
            _logger
        ).ToListAsync();
    }

    public async Task<T> GetByIdAsync<T>(string collectionName, string id)
    {
        var collection = GetCollection<T>(collectionName);
        var filter = Builders<T>.Filter.Eq("_id", id);  // Changed from "Id" to "_id" for standard MongoDB naming
        return await collection.FindOneWithDiskUseAsync(filter, _logger);
    }

    // New method to get with projection (reduces network traffic)
    public async Task<TProjection> GetByIdWithProjectionAsync<T, TProjection>(
        string collectionName, 
        string id, 
        ProjectionDefinition<T, TProjection> projection)
    {
        var collection = GetCollection<T>(collectionName);
        var filter = Builders<T>.Filter.Eq("_id", id);
        return await collection.FindOneWithProjectionAsync(filter, projection, _logger);
    }

    // Optimized create method
    public async Task CreateAsync<T>(string collectionName, T document)
    {
        var collection = GetCollection<T>(collectionName);
        await collection.InsertOneAsync(document);
    }

    // Optimized bulk insert
    public async Task CreateManyAsync<T>(string collectionName, IEnumerable<T> documents)
    {
        var collection = GetCollection<T>(collectionName);
        await collection.InsertManyAsync(documents, new InsertManyOptions { IsOrdered = false });
    }

    // Optimized update with specific modifications (better than full replacement)
    public async Task UpdateFieldsAsync<T>(
        string collectionName, 
        string id, 
        UpdateDefinition<T> update,
        bool upsert = false)
    {
        var collection = GetCollection<T>(collectionName);
        var filter = Builders<T>.Filter.Eq("_id", id);
        await collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = upsert });
    }

    // Original update method (full document replacement)
    public async Task UpdateAsync<T>(string collectionName, string id, T document)
    {
        var collection = GetCollection<T>(collectionName);
        var filter = Builders<T>.Filter.Eq("_id", id);
        await collection.ReplaceOneAsync(filter, document);
    }

    public async Task DeleteAsync<T>(string collectionName, string id)
    {
        var collection = GetCollection<T>(collectionName);
        var filter = Builders<T>.Filter.Eq("_id", id);
        await collection.DeleteOneAsync(filter);
    }

    // Optimized TTL index creation
    public async Task CreateTTLIndexAsync<T>(string collectionName, string fieldName, TimeSpan expireAfter)
    {
        var collection = GetCollection<T>(collectionName);
        var indexName = $"{fieldName}_ttl_index";

        try
        {
            // More efficient index check using direct index name lookup
            var indexExists = await IndexExistsAsync<T>(collectionName, indexName);
            
            if (!indexExists)
            {
                var indexKeysDefinition = Builders<T>.IndexKeys.Ascending(fieldName);
                var indexOptions = new CreateIndexOptions
                {
                    ExpireAfter = expireAfter,
                    Background = true,
                    Name = indexName
                };
                var indexModel = new CreateIndexModel<T>(indexKeysDefinition, indexOptions);
                await collection.Indexes.CreateOneAsync(indexModel);

                _logger.LogInformation("Created TTL index on {FieldName} with expiration after {ExpireAfter}",
                    fieldName, expireAfter);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating TTL index on {FieldName}", fieldName);
        }
    }

    // Helper to check if an index exists by name
    private async Task<bool> IndexExistsAsync<T>(string collectionName, string indexName)
    {
        var collection = GetCollection<T>(collectionName);
        using var cursor = await collection.Indexes.ListAsync();
        var indexes = await cursor.ToListAsync();
        return indexes.Any(index => index["name"].AsString == indexName);
    }

    public async Task<bool> IsConnected()
    {
        try
        {
            // Using runCommand to verify connection
            await _database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MongoDB connection check failed");
            return false;
        }
    }

    public async Task CreateIndexAsync<T>(string collectionName, string fieldName, bool descending = false)
    {
        try
        {
            var indexName = $"{fieldName}_{(descending ? "desc" : "asc")}_index";
            var indexExists = await IndexExistsAsync<T>(collectionName, indexName);
            
            if (!indexExists)
            {
                var collection = GetCollection<T>(collectionName);
                var indexKeysDefinition = descending
                    ? Builders<T>.IndexKeys.Descending(fieldName)
                    : Builders<T>.IndexKeys.Ascending(fieldName);

                var indexOptions = new CreateIndexOptions
                {
                    Background = true,
                    Name = indexName
                };

                var indexModel = new CreateIndexModel<T>(indexKeysDefinition, indexOptions);
                await collection.Indexes.CreateOneAsync(indexModel);

                _logger.LogInformation("Created index on {FieldName} ({Direction})",
                    fieldName, descending ? "descending" : "ascending");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating index on {FieldName}", fieldName);
        }
    }
    
    // New method for compound indexes
    public async Task CreateCompoundIndexAsync<T>(
        string collectionName, 
        IEnumerable<IndexKeysDefinition<T>> indexKeys,
        string indexName)
    {
        try
        {
            var indexExists = await IndexExistsAsync<T>(collectionName, indexName);
            
            if (!indexExists)
            {
                var collection = GetCollection<T>(collectionName);
                var options = new CreateIndexOptions { Name = indexName, Background = true };
                var model = new CreateIndexModel<T>(Builders<T>.IndexKeys.Combine(indexKeys), options);
                await collection.Indexes.CreateOneAsync(model);
                
                _logger.LogInformation("Created compound index {IndexName}", indexName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating compound index {IndexName}", indexName);
        }
    }
}