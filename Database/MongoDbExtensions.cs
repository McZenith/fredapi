using MongoDB.Driver;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace fredapi.Database;

public static class MongoDbExtensions
{
    // Generic method for all MongoDB collections
    public static IFindFluent<TDocument, TDocument> FindWithDiskUse<TDocument>(
        this IMongoCollection<TDocument> collection,
        FilterDefinition<TDocument> filter,
        int? limit = 1000,
        ILogger? logger = null)
    {
        // Create a find options with explicit options
        var findOptions = new FindOptions
        {
            AllowDiskUse = true,
            MaxTime = TimeSpan.FromSeconds(120), // Increase timeout to 2 minutes
            BatchSize = Math.Min(limit ?? 1000, 100) // Smaller batch size to reduce memory pressure
        };

        // Add cursor timeout handling for larger result sets
        if (limit.HasValue && limit.Value > 10000)
        {
            findOptions.NoCursorTimeout = true;
        }

        logger?.LogDebug("MongoDB query: {Filter}, Limit: {Limit}", 
            filter.ToString(), limit);

        // Use Find method with filter and options
        var result = collection.Find(filter, findOptions);

        // Apply limit if provided
        if (limit.HasValue && limit.Value > 0)
        {
            result = result.Limit(limit);
        }

        return result;
    }

    // Simplified approach for projections
    public static async Task<List<TProjection>> FindWithProjectionAsync<TDocument, TProjection>(
        this IMongoCollection<TDocument> collection,
        FilterDefinition<TDocument> filter,
        ProjectionDefinition<TDocument, TProjection> projection,
        int? limit = 1000,
        ILogger? logger = null)
    {
        logger?.LogDebug("MongoDB query with projection: {Filter}, Limit: {Limit}", 
            filter.ToString(), limit);

        // First get a regular find fluent
        var query = collection.Find(filter, new FindOptions
        {
            AllowDiskUse = true,
            MaxTime = TimeSpan.FromSeconds(120),
            BatchSize = Math.Min(limit ?? 1000, 100),
            NoCursorTimeout = limit.HasValue && limit.Value > 10000
        });
        
        // Apply limit if provided
        if (limit.HasValue && limit.Value > 0)
        {
            query = query.Limit(limit);
        }
        
        // Apply projection and execute as a list
        return await query.Project(projection).ToListAsync();
    }

    // Single document with projection
    public static async Task<TProjection> FindOneWithProjectionAsync<TDocument, TProjection>(
        this IMongoCollection<TDocument> collection,
        FilterDefinition<TDocument> filter,
        ProjectionDefinition<TDocument, TProjection> projection,
        ILogger? logger = null)
    {
        logger?.LogDebug("MongoDB query for single document with projection: {Filter}", 
            filter.ToString());

        var query = collection.Find(filter, new FindOptions
        {
            AllowDiskUse = true,
            MaxTime = TimeSpan.FromSeconds(120),
            BatchSize = 1
        });
        
        // Apply projection and get first result
        return await query.Project(projection).FirstOrDefaultAsync();
    }

    // Helper for single document retrieval
    public static async Task<TDocument> FindOneWithDiskUseAsync<TDocument>(
        this IMongoCollection<TDocument> collection,
        FilterDefinition<TDocument> filter,
        ILogger? logger = null)
    {
        return await collection.FindWithDiskUse(filter, 1, logger)
            .FirstOrDefaultAsync();
    }
}