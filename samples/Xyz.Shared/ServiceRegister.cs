using HsnSoft.Base.AspNetCore;
using HsnSoft.Base.Authentication;
using HsnSoft.Base.Caching.StackExchangeRedis;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Serilog;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xyz.Shared.RateLimiting;

namespace Xyz.Shared;

/// <summary>
/// How the Xyz product composes the framework. The registrations and the middleware order are decided here, once, so a host
/// cannot get them wrong.
/// </summary>
public static class ServiceRegister
{
    public const string AdminPolicy = "admin";

    /// <summary>The shell of every Xyz service: error handling, OpenAPI, health checks, JWT authentication and the Redis cache.</summary>
    public static IServiceCollection AddXyzApi(this IServiceCollection services, IConfiguration configuration)
        => services
            .AddBaseLogging(typeof(ServiceRegister).Assembly)   // the contracts say what is secret: the gateway hides it in the bodies it logs too
            .AddBaseRequestLogging(configuration)
            .AddBaseWebApi()
            .AddBaseOpenApi()
            .AddBaseHealthChecks()
            .AddBaseJwtAuthentication(configuration)
            .AddBaseRedis(configuration)
            .AddHttpContextAccessor()
            .AddSingleton<IEventHeaderProvider, HttpContextEventHeaderProvider>();   // who started a chain travels with its events

    /// <summary>
    /// Every Xyz service logs the same way: the console gets everything, Graylog (or the log file) gets Information and above,
    /// and every log carries <c>Solution = Xyz</c> (the first part of the assembly name) so it can be found in Graylog.
    /// </summary>
    public static ILoggingBuilder AddXyzLogging(this ILoggingBuilder logging, IConfiguration configuration, string serviceName)
        => logging.AddBaseSerilog(configuration, serviceName);

    /// <summary>Reads the <c>RateLimit</c> section for <see cref="RedisRateLimitMiddleware"/>; use it with <c>UseXyzApi</c>'s hook.</summary>
    public static IServiceCollection AddXyzRateLimiting(this IServiceCollection services, IConfiguration configuration)
        => services.Configure<RateLimitOptions>(configuration.GetSection("RateLimit"));

    /// <summary>Adds the "admin" policy: the caller must carry the <see cref="Roles.Admin"/> role.</summary>
    public static IServiceCollection AddXyzAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder().AddPolicy(AdminPolicy, policy => policy.RequireRole(Roles.Admin));
        return services;
    }

    /// <summary>
    /// The middleware order of every Xyz service: exceptions first (so they cover everything after), then who the caller is,
    /// then what the caller may do. <paramref name="beforeAuthorization"/> places middleware that needs the user but must also
    /// see the requests authorization will reject (the gateway's rate limit).
    /// </summary>
    public static WebApplication UseXyzApi(this WebApplication app, Action<WebApplication>? beforeAuthorization = null)
    {
        app.UseBaseRequestLogging();   // outside the exception handler: the response log has the status the client gets
        app.UseBaseExceptionHandling();
        app.MapBaseOpenApi();
        app.UseBaseAuthentication();
        beforeAuthorization?.Invoke(app);
        app.UseBaseAuthorization();
        app.MapBaseHealth();
        return app;
    }
}
