using Hhs.ContentService;
using Hhs.ContentService.Application;
using Hhs.ContentService.Application.Services;
using Hhs.ContentService.Domain.Configuration;
using Hhs.ContentService.Domain.Localization;
using Hhs.ContentService.EntityFrameworkCore;
using Hhs.ContentService.EntityFrameworkCore.Setup;
using Hhs.Shared.Helper.Configuration;
using Hhs.Shared.Helper.Consts;
using Hhs.Shared.Helper.Retry;
using Hhs.Shared.Hosting.Extensions;
using Hhs.Shared.Hosting.Helpers;
using Hhs.Shared.Hosting.Microservices.Extensions;
using Hhs.Shared.Hosting.Microservices.Middlewares;
using HsnSoft.Base.AspNetCore.Localization;
using HsnSoft.Base.Data;
using HsnSoft.Base.Serilog;
using HsnSoft.Base.Swashbuckle;
using HsnSoft.Base.Tracing;
using Serilog;
using Hhs.Shared.Contracts.Events;

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

    options.ListenAnyIP(7450);
});

// =======================
// ConfigureServices
// =======================

builder.Services.AddMicroserviceHosting(builder.Configuration, typeof(Program))
    .AddJwtServerAuthentication(builder.Configuration, builder.Environment, "audience-service-content")
    .AddPermissionAuthorization()
    .AddMicroserviceUserTenantChecker()
    .AddEventBus(builder.Configuration, typeof(EventHandlersAssemblyMarker).Assembly)
    .AddHostingHealthChecks(builder.Configuration, "content",
        checkRedis: true,
        checkBroker: true,
        checkPostgresql: true,
        postgresqlConnectionName: EfCoreDbProperties.ConnectionStringName)
    .AddServiceApplicationConfiguration(builder.Configuration)
    .AddServiceEfCoreDatabaseConfiguration(builder.Configuration, !builder.Environment.IsHostProduction());

// ============================================================================
// DATA SEEDING
// ============================================================================
// Configures default data seeder for database initialization
builder.Services.AddTransient<IBasicDataSeeder, EfCoreSeederService>();

// ============================================================================
// BACKGROUND WORKERS (Retry Logic & Event Recovery)
// ============================================================================
// Retry Worker: Configures background service for retrying failed events
var contentRetrySettings = builder.Configuration.GetSection(nameof(ContentRetrySettings))
    .Get<ContentRetrySettings>() ?? new ContentRetrySettings();

builder.Services
    .AddSingleton(contentRetrySettings)
    .AddSingleton<RetrySettingsBase>(contentRetrySettings)
    .AddSingleton(_ => new RetryDelayCalculator(contentRetrySettings.DelaySeconds))
    .AddScoped<ContentOperationRetryWorkerService>();

// Swagger
if (!builder.Environment.IsHostProduction())
{
    SwaggerConfigurationHelper.ConfigureWithBearer(builder.Services,
        "Please enter a valid token. Token audiences contains audience-service-content",
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
    app.ConfigureLocalizedModelValidator(typeof(ContentServiceResource));

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
        { nameof(NormalizerResultPublishedEto), 5 }, // This event fetch count more than one
        { nameof(VideoGenerationResultPublishedEto), 5 }, // This event fetch count more than one
        { nameof(StepFailedEto), 1 } // This event fetch count more than one
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