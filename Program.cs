using fredapi.Database;
using fredapi.Routes;
using fredapi.SignalR;
using fredapi.Utils;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http.Connections;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole(options => { options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss.fff] "; });
    logging.AddDebug();
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .SetIsOriginAllowed((host) => true)
            .AllowCredentials();
    });
});

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
    options.MaximumReceiveMessageSize = 102400;
}).AddMessagePackProtocol();

// MongoDB configuration
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDb"));

builder.Services.Configure<MongoDbSettings>(options =>
{
    // Try environment variable first, then fall back to configuration
    options.ConnectionString = Environment.GetEnvironmentVariable("MONGODB_URI") ??
                               builder.Configuration.GetSection("MongoDb:ConnectionString").Value ??
                               throw new InvalidOperationException("MongoDB Connection String is not configured");
    options.DatabaseName = builder.Configuration.GetSection("MongoDb:DatabaseName").Value ?? "SportsDb";
});

builder.Services.AddSportRadarService();
builder.Services.AddOpenApi();


var app = builder.Build();

if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.UseCors();
app.UseWebSockets();
app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<LiveMatchHub>("/livematchhub", options =>
    {
        options.Transports = HttpTransportType.WebSockets | 
                             HttpTransportType.ServerSentEvents | 
                             HttpTransportType.LongPolling;
    });
});


app.MapGet("/health/database", async (MongoDbService mongoService) =>
{
    var isConnected = await mongoService.IsConnected();
    return isConnected
        ? Results.Ok(new { Status = "Connected", Database = "SportsDb" })
        : Results.StatusCode(503);
});

var sportRadarGroup = app.MapGroup("/api").WithOpenApi().WithTags("SportRadar");
// Register routes
sportRadarGroup.MapSeasonRoutes();
sportRadarGroup.MapTeamRoutes();
sportRadarGroup.MapMatchRoutes();

app.Run();