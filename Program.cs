using fredapi.Database;
using fredapi.Routes;
using fredapi.SignalR;
using fredapi.Utils;
using Microsoft.AspNetCore.Http.Connections;

var builder = WebApplication.CreateBuilder(args);

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
            .SetIsOriginAllowed((host) => true)
            .AllowCredentials()
            .WithExposedHeaders("Content-Disposition"); // Add if needed
    });
});

// SignalR configuration with MessagePack
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
    options.MaximumReceiveMessageSize = 102400;
    options.StreamBufferCapacity = 10;
    options.MaximumParallelInvocationsPerClient = 2;
}).AddMessagePackProtocol();

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
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment()) 
{
    app.MapOpenApi();
}

// Order is important for middleware
app.UseRouting();
app.UseCors(); // After UseRouting
app.UseWebSockets();

// SignalR endpoint
app.MapHub<LiveMatchHub>("/livematchhub", options =>
{
    options.Transports = 
        HttpTransportType.WebSockets | 
        HttpTransportType.ServerSentEvents | 
        HttpTransportType.LongPolling;
    options.WebSockets.CloseTimeout = TimeSpan.FromSeconds(5);
    options.LongPolling.PollTimeout = TimeSpan.FromSeconds(90);
    options.ApplicationMaxBufferSize = 100 * 1024; // 100KB
    options.TransportMaxBufferSize = 100 * 1024;
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

app.Run();