using HealthChecks.UI.Client;
using Hhs.Commercial.Web;
using Hhs.Shared.Helper.Constants;
using Hhs.Shared.Hosting.Extensions;
using Hhs.Shared.Hosting.Helpers;
using Hhs.Shared.Hosting.Middlewares;
using HsnSoft.Base.AspNetCore.Tracing;
using HsnSoft.Base.Serilog;
using HsnSoft.Base.Tracing;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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

// Network Security
builder.WebHost.ConfigureKestrel((_, options) =>
{
    options.Limits.MaxRequestBufferSize = long.MaxValue;
    options.Limits.MaxRequestBodySize = long.MaxValue;

    if (!builder.Environment.IsDevelopment()) return;

    options.ListenAnyIP(7300);
    options.ListenAnyIP(7301, listenOptions => { listenOptions.UseHttps("../../etc/dev-cert/localhost.pfx", "e8202f07-66e5-4619-be07-72ba76fde97f"); });
});
builder.Services.AddDefaultCorsSettings("default");

builder.Services.AddCommonAspNetCoreHosting(builder.Configuration);
builder.Services.AddHttpClient();
builder.Services.AddRazorPages();

#region Tracing for Public Network Apps

builder.Services.AddRequestResponseLogger();

#endregion

var hcBuilder = builder.Services.AddHealthChecks();
hcBuilder.AddCheck("self-check", () => HealthCheckResult.Healthy(), tags: ["dependencies"]);

// =======================
// Build
// =======================
try
{
    Log.Information("Configuring web host ({ApplicationContext})...", ApplicationIdentifier.AppName);

    var app = builder.Build();

    Log.Information("Starting web host ({ApplicationContext})...", ApplicationIdentifier.AppName);

    // =======================
    // Configure (Middleware)
    // =======================

    app.UseForwardedHeaders();

    #region Tracing for Public Network Apps

    app.UseCorrelationId();
    app.UseMiddleware<ApiRequestResponseLoggingMiddleware>();

    #endregion

    app.UseCustomExceptionHandler();

    if (!app.Environment.IsHostProduction())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseExceptionHandler("/Home/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseMiddleware<SearchEngineAgentMiddleware>(); // robot.txt

    app.UseStaticFiles(); // direct download files

    // no-localization

    app.UseRouting();

    app.UseDefaultCorsSettings("default");

    // app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();
    // no-tenant-checker

    #region Endpoints

    if (!app.Environment.IsHostProduction())
    {
        app.MapRazorPages();
    }
    else
    {
        string buildNumber = Environment.GetEnvironmentVariable("BUILD_NUMBER");
        string appVersion = !string.IsNullOrWhiteSpace(buildNumber) ? $"v1.0.{buildNumber}" : "v1.0.0";
        app.MapGet("/", () => Results.Text($"{NameConsts.SolutionName.ToUpper()} | {ApplicationIdentifier.AppName} | {ApplicationIdentifier.AppId} | {builder.Environment.EnvironmentName} | {appVersion}"))
            .AllowAnonymous();
    }

    #endregion

    // HealthChecks
    app.UseHealthChecks("/StartupCheck", new HealthCheckOptions { Predicate = r => r.Tags.Contains("dependencies"), ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse });
    app.UseHealthChecks("/LivenessCheck", new HealthCheckOptions { Predicate = r => r.Name.Equals("self-check") });
    app.UseHealthChecks("/ReadinessCheck", new HealthCheckOptions { Predicate = r => r.Tags.Contains("dependencies"), ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse });

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