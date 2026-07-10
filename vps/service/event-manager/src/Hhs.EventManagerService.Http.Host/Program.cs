using Hhs.EventManagerService;
using Hhs.EventManagerService.Application;
using Hhs.EventManagerService.Application.Services;
using Hhs.EventManagerService.Domain.Configuration;
using Hhs.EventManagerService.Domain.Localization;
using Hhs.EventManagerService.MongoDb;
using Hhs.EventManagerService.MongoDb.Setup;
using Hhs.EventManagerService.Workers;
using Hhs.Shared.Helper.Configuration;
using Hhs.Shared.Helper.Constants;
using Hhs.Shared.Hosting.Extensions;
using Hhs.Shared.Hosting.Helpers;
using Hhs.Shared.Hosting.Microservices.Extensions;
using Hhs.Shared.Hosting.Microservices.Middlewares;
using HsnSoft.Base.AspNetCore.Localization;
using HsnSoft.Base.Data;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Serilog;
using HsnSoft.Base.Swashbuckle;
using HsnSoft.Base.Tracing;
using Serilog;

// Load .env from current dir, project root, or bin output path — whichever exists first (file is gitignored)
string[] envCandidates =
[
    Path.Combine(Directory.GetCurrentDirectory(), ".env"),
    Path.Combine(AppContext.BaseDirectory, ".env"),
    Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".env"),
];
string? envFile = Array.Find(envCandidates, File.Exists);
if (envFile != null) DotNetEnv.Env.Load(envFile);

var builder = WebApplication.CreateBuilder(args);

// App info
AppInfoResolver.Resolve(typeof(EventHandlersAssemblyMarker));

// Config
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

// Serilog
Log.Logger = SerilogConfigurationHelper.ConfigureConsoleLogger(builder.Configuration, "Host");
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(Log.Logger);

// Graceful shutdown — bounds how long IHostedService.StopAsync implementations (retry/polling workers,
// sink refresh, etc.) get before their cancellation token fires. Must stay comfortably under the
// container's terminationGracePeriodSeconds/stop_grace_period, which also has to cover the RabbitMQ
// consumer/publisher drain that happens afterward during DI container disposal (EventBusRabbitMq.Dispose()).
builder.Services.AddOptions<HostOptions>().Configure(hostOptions => hostOptions.ShutdownTimeout = TimeSpan.FromSeconds(30));

// Kestrel
builder.WebHost.ConfigureKestrel((_, options) =>
{
    options.Limits.MaxRequestBufferSize = long.MaxValue;
    options.Limits.MaxRequestBodySize = long.MaxValue;

    if (!builder.Environment.IsDevelopment()) return;

    options.ListenAnyIP(7480);
});

// =======================
// ConfigureServices
// =======================

builder.Services.AddMicroserviceHosting(builder.Configuration, typeof(Program))
    .AddJwtServerAuthentication(builder.Configuration, builder.Environment, "audience-service-event-manager")
    .AddPermissionAuthorization()
    .AddMicroserviceUserTenantChecker()
    .AddEventBus(builder.Configuration, typeof(EventHandlersAssemblyMarker).Assembly)
    .AddHostingHealthChecks(builder.Configuration, "event-manager",
        checkRedis: true,
        checkBroker: true,
        checkMongo: true, mongoConnectionName: MongoDbProperties.ConnectionStringName)
    .AddServiceApplicationConfiguration(builder.Configuration)
    .AddServiceMongoDatabaseConfiguration(builder.Configuration);

// ============================================================================
// DATA SEEDING
// ============================================================================
// Configures default data seeder for MongoDB initialization
builder.Services.AddTransient<IBasicDataSeeder, MongoSeederService>();

// ============================================================================
// BACKGROUND WORKERS (Retry Logic & Event Recovery)
// ============================================================================
// Retry Worker: Configures background service for retrying failed events
var eventManagerRetrySettings = builder.Configuration.GetSection("RetryPolicy")
    .Get<EventManagerRetrySettings>() ?? new EventManagerRetrySettings();

builder.Services
    .AddSingleton(eventManagerRetrySettings)
    .AddSingleton<RetrySettingsBase>(eventManagerRetrySettings)
    .AddScoped<EventOperationRetryWorkerService>()
    .AddHostedService<EventManagerRetryWorker>();

// Swagger
if (!builder.Environment.IsHostProduction())
{
    SwaggerConfigurationHelper.ConfigureWithBearer(builder.Services,
        "Please enter a valid token. Token audiences contains audience-service-event-manager",
        $"{ApplicationIdentifier.AppName} API");
}

// =======================
// Build
// =======================
try
{
    Log.Information("Configuring web host ({ApplicationContext})...", ApplicationIdentifier.AppName);

    var app = builder.Build();

    Log.Information("Starting web host ({ApplicationContext})...", ApplicationIdentifier.AppName);

    using (var scope = app.Services.CreateScope())
    {
        // optional init - seed
    }

    // =======================
    // Configure (Middleware)
    // =======================

    app.UseCustomExceptionHandler();

    if (!app.Environment.IsHostProduction())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.RoutePrefix = "swagger";
            c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{ApplicationIdentifier.AppName} API");
        });
    }

    app.UseMiddleware<BaseLocalizationMiddleware>();
    app.ConfigureLocalizedModelValidator(typeof(EventManagerServiceResource));

    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();
    app.UseMiddleware<UserTenantCheckerMiddleware>();

    #region Endpoints

    string buildNumber = Environment.GetEnvironmentVariable("BUILD_NUMBER");
    string appVersion = !string.IsNullOrWhiteSpace(buildNumber) ? $"v1.0.{buildNumber}" : "v1.0.0";
    app.MapGet("/", () => Results.Text($"{NameConsts.SolutionName.ToUpper()} | {ApplicationIdentifier.AppName} | {ApplicationIdentifier.AppId} | {builder.Environment.EnvironmentName} | {appVersion}"))
        .AllowAnonymous();

    app.MapControllers();

    #endregion

    // HealthChecks
    app.UseHostingHealthChecks();

    // EventBus
    app.UseEventBus(typeof(EventHandlersAssemblyMarker).Assembly, new Dictionary<string, ushort> { { nameof(FailedEto), 1 } });

    // Shutdown hook
    var lifetime = app.Lifetime;
    lifetime.ApplicationStopping.Register(() => { Console.WriteLine("Stopping web host ({0})...", ApplicationIdentifier.AppName); });

    await app.RunAsync();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Program terminated unexpectedly ({ApplicationContext})!", ApplicationIdentifier.AppName);
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}