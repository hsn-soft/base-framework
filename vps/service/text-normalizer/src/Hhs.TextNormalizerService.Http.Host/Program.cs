using Hhs.Shared.Contracts.Events;
using Hhs.Shared.Helper.Configuration;
using Hhs.Shared.Helper.Consts;
using Hhs.Shared.Helper.Retry;
using Hhs.Shared.Hosting.Extensions;
using Hhs.Shared.Hosting.Helpers;
using Hhs.Shared.Hosting.Microservices.Extensions;
using Hhs.Shared.Hosting.Microservices.Middlewares;
using Hhs.TextNormalizerService;
using Hhs.TextNormalizerService.Application;
using Hhs.TextNormalizerService.Application.Services;
using Hhs.TextNormalizerService.Domain.Configuration;
using Hhs.TextNormalizerService.Domain.Configuration.Providers.Outline;
using Hhs.TextNormalizerService.Domain.Localization;
using Hhs.TextNormalizerService.MongoDb;
using Hhs.TextNormalizerService.MongoDb.Setup;
using Hhs.TextNormalizerService.Workers;
using HsnSoft.Base.AspNetCore.Localization;
using HsnSoft.Base.Data;
using HsnSoft.Base.PuppeTeer;
using HsnSoft.Base.Serilog;
using HsnSoft.Base.Swashbuckle;
using HsnSoft.Base.Tracing;
using Microsoft.Extensions.Options;
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

// Kestrel
builder.WebHost.ConfigureKestrel((_, options) =>
{
    options.Limits.MaxRequestBufferSize = long.MaxValue;
    options.Limits.MaxRequestBodySize = long.MaxValue;

    if (!builder.Environment.IsDevelopment()) return;

    options.ListenAnyIP(7460);
});

// =======================
// ConfigureServices
// =======================

builder.Services.AddMicroserviceHosting(builder.Configuration, typeof(Program))
    .AddJwtServerAuthentication(builder.Configuration, builder.Environment, "audience-service-text-normalizer")
    .AddPermissionAuthorization()
    .AddMicroserviceUserTenantChecker()
    .AddEventBus(builder.Configuration, typeof(EventHandlersAssemblyMarker).Assembly)
    .AddHostingHealthChecks(builder.Configuration, "text-normalizer",
        checkRedis: true,
        checkBroker: true,
        checkMongo: true, mongoConnectionName: MongoDbProperties.ConnectionStringName)
    .AddServiceApplicationConfiguration(builder.Configuration, builder.Environment)
    .AddServiceMongoDatabaseConfiguration(builder.Configuration);

// ============================================================================
// DATA SEEDING
// ============================================================================
builder.Services.AddTransient<IBasicDataSeeder, MongoSeederService>();

// ============================================================================
// SPECIAL INTEGRATIONS (Puppeteer Browser Management)
// ============================================================================
// Initializes Puppeteer browser for web scraping and rendering; configures graceful shutdown
// Puppeteer browser instance is cached and reused across requests for performance
// Shutdown timeout extended by 30 seconds to allow pending operations to complete safely
builder.Services.AddHostedService<PuppeteerShutdownHostedService>();
builder.Services.AddOptions<HostOptions>()
    .Configure<IOptions<PuppeteerBrowserSettings>>((hostOptions, browserSettings) =>
    {
        hostOptions.ShutdownTimeout = TimeSpan.FromSeconds(browserSettings.Value.ShutdownDrainTimeoutSeconds);
    });

// For Provider Clients
builder.Services.AddHttpClient();

// ============================================================================
// 3. POLLING & RETRY CONFIGURATION
// ============================================================================

var outlinePollingSettings = builder.Configuration.GetSection(OutlinePollingSettings.SectionName)
    .Get<OutlinePollingSettings>() ?? new OutlinePollingSettings();

builder.Services
    .AddSingleton(outlinePollingSettings)
    .AddScoped<OutlineProviderPollingWorkerService>()
    .AddHostedService<OutlineProviderPollingWorker>();

var normalizerRetrySettings = builder.Configuration.GetSection(nameof(NormalizerRetrySettings))
    .Get<NormalizerRetrySettings>() ?? new NormalizerRetrySettings();

builder.Services
    .AddSingleton(normalizerRetrySettings)
    .AddSingleton<RetrySettingsBase>(normalizerRetrySettings)
    .AddSingleton(_ => new RetryDelayCalculator(normalizerRetrySettings.DelaySeconds))
    .AddScoped<NormalizerOperationRetryWorkerService>()
    .AddHostedService<NormalizerRetryWorker>();

// Swagger
if (!builder.Environment.IsHostProduction())
{
    SwaggerConfigurationHelper.ConfigureWithBearer(builder.Services,
        "Please enter a valid token. Token audiences contains audience-service-text-normalizer",
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
        // Initialize Puppeteer browser on startup to detect availability early
        // If unavailable, service runs in degraded mode without scraping/rendering capabilities
        try
        {
            var puppeTeer = scope.ServiceProvider.GetRequiredService<IPuppeteerBrowser>();
            await using var page = await (await puppeTeer.GetBrowserSafelyAsync()).NewPageAsync();
            await page.SetContentAsync("Initialize page test");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Puppeteer unavailable, running in degraded mode");
        }
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
    app.ConfigureLocalizedModelValidator(typeof(TextNormalizerServiceResource));

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
    app.UseEventBus(typeof(EventHandlersAssemblyMarker).Assembly, new Dictionary<string, ushort>
    {
        { nameof(CustomerContentCreatedEto), 1 }, // This event fetch count more than one
    });

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