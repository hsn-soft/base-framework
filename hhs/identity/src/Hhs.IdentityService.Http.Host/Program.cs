using Hhs.IdentityService;
using Hhs.IdentityService.Application;
using Hhs.IdentityService.Application.Contracts.AuthDomain.Interfaces;
using Hhs.IdentityService.Domain.Localization;
using Hhs.IdentityService.EntityFrameworkCore;
using Hhs.IdentityService.EntityFrameworkCore.Setup;
using Hhs.Shared.Contracts.Cache;
using Hhs.Shared.Contracts.Cache.ServicePermissions;
using Hhs.Shared.Contracts.Events;
using Hhs.Shared.Helper.Consts;
using Hhs.Shared.Helper.Utils;
using Hhs.Shared.Hosting.Extensions;
using Hhs.Shared.Hosting.Helpers;
using Hhs.Shared.Hosting.Microservices.Extensions;
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

    options.ListenAnyIP(7410);
});

// =======================
// ConfigureServices
// =======================

builder.Services.AddMicroserviceHosting(builder.Configuration, typeof(Program))
    .AddJwtServerAuthentication(builder.Configuration, builder.Environment, "audience-service-identity")
    .AddCustomAuthorization(IdentityServicePermissions.GetAll())
    .AddMicroserviceUserTenantChecker()
    .AddEventBus(builder.Configuration, typeof(EventHandlersAssemblyMarker).Assembly)
    .AddHostingHealthChecks(builder.Configuration, "identity",
        checkRedis: true,
        checkBroker: true,
        checkPostgresql: true,
        postgresqlConnectionName: EfCoreDbProperties.ConnectionStringName);

// app services
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IServicePermissionProvider, ApplicationPermissionProvider>();

builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// database services
builder.Services.AddServiceEfCoreDatabaseConfiguration(builder.Configuration, !builder.Environment.IsHostProduction());

// override DefaultBasicDataSeeder
builder.Services.AddTransient<IBasicDataSeeder, EfCoreSeederService>();

// Swagger
if (!builder.Environment.IsHostProduction())
{
    SwaggerConfigurationHelper.ConfigureWithBearer(builder.Services,
        "Please enter a valid token. Token audiences contains audience-service-identity",
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

    if (!app.Environment.IsHostProduction())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.RoutePrefix = "swagger";
            c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{ApplicationIdentifier.AppName} API");
        });
    }

    app.UseMicroserviceHosting();

    app.ConfigureLocalizedModelValidator(typeof(IdentityServiceResource));
    app.UseMiddleware<BaseLocalizationMiddleware>();

    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();
    app.UseUserTenantChecker();

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
    app.UseEventBus(typeof(EventHandlersAssemblyMarker).Assembly, new Dictionary<string, ushort> { { nameof(CachePermissionGrantsChangedEto), 1 } });

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