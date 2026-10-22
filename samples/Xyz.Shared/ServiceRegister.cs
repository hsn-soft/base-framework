using HsnSoft.Base.AspNetCore;
using HsnSoft.Base.Authentication;
using HsnSoft.Base.Caching.StackExchangeRedis;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            .AddBaseWebApi()
            .AddBaseOpenApi()
            .AddBaseHealthChecks()
            .AddBaseJwtAuthentication(configuration)
            .AddBaseRedis(configuration);

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
        app.UseBaseExceptionHandling();
        app.MapBaseOpenApi();
        app.UseBaseAuthentication();
        beforeAuthorization?.Invoke(app);
        app.UseBaseAuthorization();
        app.MapBaseHealth();
        return app;
    }
}
