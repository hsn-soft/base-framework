using Hhs.AdministrationService;
using Hhs.AdministrationService.Application;
using Hhs.AdministrationService.Application.Contracts.Events;
using Hhs.AdministrationService.Application.Services;
using Hhs.AdministrationService.Domain.Localization;
using Hhs.AdministrationService.Domain.Configuration;
using Hhs.AdministrationService.EntityFrameworkCore;
using Hhs.AdministrationService.EntityFrameworkCore.Setup;
using Hhs.AdministrationService.Workers;
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

    options.ListenAnyIP(7400);
});

// =======================
// ConfigureServices
// =======================

builder.Services.AddMicroserviceHosting(builder.Configuration, typeof(Program))
    .AddJwtServerAuthentication(builder.Configuration, builder.Environment, "audience-service-administration")
    .AddPermissionAuthorization()
    .AddMicroserviceUserTenantChecker()
    .AddEventBus(builder.Configuration, typeof(EventHandlersAssemblyMarker).Assembly)
    .AddHostingHealthChecks(builder.Configuration, "administration",
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
var administrationRetrySettings = builder.Configuration.GetSection(nameof(AdministrationRetrySettings))
    .Get<AdministrationRetrySettings>() ?? new AdministrationRetrySettings();

builder.Services
    .AddSingleton(administrationRetrySettings)
    .AddSingleton(_ => new RetryDelayCalculator(administrationRetrySettings.DelaySeconds))
    .AddScoped<AdministrationOperationRetryWorkerService>()
    .AddHostedService<AdministrationRetryWorker>();

// Swagger
if (!builder.Environment.IsHostProduction())
{
    SwaggerConfigurationHelper.ConfigureWithBearer(builder.Services,
        "Please enter a valid token. Token audiences contains audience-service-administration",
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
    app.ConfigureLocalizedModelValidator(typeof(AdministrationServiceResource));

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
    app.UseEventBus(typeof(EventHandlersAssemblyMarker).Assembly, new Dictionary<string, ushort> { { nameof(SynchAllPermissionToCacheDbEto), 1 } });

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