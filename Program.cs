using fredapi.Database;
using fredapi.Routes;
using fredapi.SignalR;
using fredapi.Utils;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.ResponseCompression;
using MessagePack;

var builder = WebApplication.CreateBuilder(args);

// Add response compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Optimal;
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Optimal;
});

builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole(options => { options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss.fff] "; });
    logging.AddDebug();
});

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .SetIsOriginAllowed((_) => true)
            .AllowCredentials()
            .WithExposedHeaders("Content-Disposition"); // Add if needed
    });
});

// Add Memory Cache
builder.Services.AddMemoryCache();

// SignalR configuration with MessagePack - optimized
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.KeepAliveInterval = TimeSpan.FromSeconds(10);       // Reduced from 15 to detect disconnections faster
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);    // Good value
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);         // Good value
    options.MaximumReceiveMessageSize = 102400;                  // 100KB - reasonable
    options.StreamBufferCapacity = 10;                           // Good value
    options.MaximumParallelInvocationsPerClient = 4;             // Increased from 2 for more parallel operations
})
.AddMessagePackProtocol(options => {
    // Add MessagePack optimizations - smaller payloads, faster serialization
    options.SerializerOptions = MessagePackSerializerOptions
        .Standard.WithCompression(MessagePackCompression.Lz4BlockArray)
        .WithSecurity(MessagePackSecurity.UntrustedData);
});

// MongoDB configuration
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDb"));

builder.Services.Configure<MongoDbSettings>(options =>
{
    options.ConnectionString = Environment.GetEnvironmentVariable("MONGODB_URI") ??
                             builder.Configuration.GetSection("MongoDb:ConnectionString").Value ??
                             throw new InvalidOperationException("MongoDB Connection String is not configured");
    options.DatabaseName = builder.Configuration.GetSection("MongoDb:DatabaseName").Value ?? "SportsDb";
});

builder.Services.AddSportRadarService();

var app = builder.Build();

// Enable response compression
app.UseResponseCompression();

// Create MongoDB indexes for optimizing sort operations
using (var scope = app.Services.CreateScope())
{
    var mongoDbService = scope.ServiceProvider.GetRequiredService<MongoDbService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // Create index on MatchTime field (descending) for EnrichedSportMatches collection
        await mongoDbService.CreateIndexAsync<MongoEnrichedMatch>(
            "EnrichedSportMatches",
            "MatchTime",
            descending: true);

        logger.LogInformation("Successfully created MongoDB indexes at startup");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error creating MongoDB indexes at startup");
    }
}

// Order is important for middleware
app.UseRouting();
app.UseCors(); // After UseRouting
app.UseWebSockets();

// SignalR endpoint with optimized settings
app.MapHub<LiveMatchHub>("/livematchhub", options =>
{
    options.Transports = 
        HttpTransportType.WebSockets | 
        HttpTransportType.ServerSentEvents | 
        HttpTransportType.LongPolling;
    options.WebSockets.CloseTimeout = TimeSpan.FromSeconds(10);  // Increased from 5 for more graceful closures
    options.LongPolling.PollTimeout = TimeSpan.FromSeconds(60);  // Reduced from 90 to avoid server resource strain
    options.ApplicationMaxBufferSize = 100 * 1024;               // Good value
    options.TransportMaxBufferSize = 100 * 1024;                 // Good value
});

// Monitoring endpoint for SignalR connections
app.MapGet("/admin/signalr-stats", () =>
{
    return Results.Ok(new { 
        ConnectedClients = LiveMatchHub.GetConnectedClientCount(),
        Time = DateTime.UtcNow 
    });
});

// Health check endpoint
app.MapGet("/health/database", async (MongoDbService mongoService) =>
{
    var isConnected = await mongoService.IsConnected();
    return isConnected
        ? Results.Ok(new { Status = "Connected", Database = "SportsDb" })
        : Results.StatusCode(503);
});

// API routes
var sportRadarGroup = app.MapGroup("/api").WithOpenApi().WithTags("SportRadar");
sportRadarGroup.MapSeasonRoutes();
sportRadarGroup.MapTeamRoutes();
sportRadarGroup.MapMatchRoutes();
sportRadarGroup.MapMatchesEndpoints();
sportRadarGroup.MapMatchOddsEndpoint();
sportRadarGroup.MapMatchDetailsEndpoint();
sportRadarGroup.MapArbitrageRoutes();
sportRadarGroup.MapLiveMatchDataRoutes();
sportRadarGroup.MapPredictionResultsRoutes();

app.Run();