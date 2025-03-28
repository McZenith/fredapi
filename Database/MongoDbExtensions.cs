using MongoDB.Driver;

namespace fredapi.Database;

public static class MongoDbExtensions
{
    // Generic method for all MongoDB collections
    public static IFindFluent<TDocument, TDocument> FindWithDiskUse<TDocument>(
        this IMongoCollection<TDocument> collection,
        FilterDefinition<TDocument> filter,
        int? limit = 1000)
    {
        // Create a find options with explicit options
        var findOptions = new FindOptions
        {
            AllowDiskUse = true,
            MaxTime = TimeSpan.FromSeconds(120), // Increase timeout to 2 minutes
            BatchSize = 100 // Use smaller batch size to reduce memory pressure
        };

        // Use Find method with filter and options
        var result = collection.Find(filter, findOptions);

        // Apply limit if provided
        if (limit.HasValue)
        {
            result = result.Limit(limit);
        }

        return result;
    }
}