using HsnSoft.Base.Application;
using HsnSoft.Base.AspNetCore;
using HsnSoft.Base.Authentication;
using HsnSoft.Base.Caching.StackExchangeRedis;
using HsnSoft.Base.Localization;
using HsnSoft.Base.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Abc.Shared.Hosting;

/// <summary>The shell every Abc host has, in the order it needs: see <see cref="AddAbcHosting"/> and <see cref="UseAbcHosting"/>.</summary>
public static class HostingRegister
{
    /// <summary>
    /// Registers what every Abc host shares: the masker and the console logger, the request and response logs (a gateway has <c>RequestLogging:Role</c> = <c>Gateway</c>), the
    /// answers of a web api (<c>BaseResponse</c>, the exception handler), Swagger and the health checks, and, for a host that reads the bearer token, the JWT validation (section <c>Jwt</c>).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Holds <c>RequestLogging</c>, <c>Swagger</c> and <c>Jwt</c>.</param>
    /// <param name="swaggerTitle">The title of the Swagger document of the host.</param>
    /// <param name="swaggerName">The name of the document (<c>v1</c>; <c>gateway</c> for a gateway).</param>
    /// <param name="authenticateUsers">Whether the host reads the bearer token. A microservice does, and its endpoints need it; the commercial gateway does, to know the user in its logs; the public gateway does not.</param>
    /// <param name="swaggerBearerButton">Whether the Swagger UI has an <c>Authorize</c> button for a bearer token.</param>
    public static IServiceCollection AddAbcHosting(
        this IServiceCollection services,
        IConfiguration configuration,
        string swaggerTitle,
        string swaggerName = "v1",
        bool authenticateUsers = true,
        bool swaggerBearerButton = false)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // how long a stopping host waits for its hosted services, the consumers of the event bus among them (they finish the messages they are handling): Hosting:ShutdownTimeoutSeconds, 30 s. The pod's terminationGracePeriodSeconds has to cover
        // this, the wait of the event bus for what it is publishing (EventBus:ShutdownTimeout) and the preStop.
        services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(configuration.GetValue("Hosting:ShutdownTimeoutSeconds", 30)));

        services
            .AddBaseLogging()
            .AddBaseRequestLogging(configuration)
            .AddBaseWebApi()
            .AddBaseHealthChecks()
            .AddBasePaging(configuration)
            .AddBaseLocalization(configuration, options => options.AddEmbedded(typeof(AbcTenants).Assembly));   // the shared layer of the texts; a service adds its own after it

        if (swaggerBearerButton)
        {
            services.AddBaseSwaggerWithBearer(swaggerTitle, "Enter a valid token", configuration, name: swaggerName);
        }
        else
        {
            services.AddBaseSwagger(swaggerTitle, configuration, name: swaggerName);
        }

        if (authenticateUsers)
        {
            services.AddBaseJwtAuthentication(configuration);
        }

        return services;
    }

    /// <summary>
    /// Connects the host to Redis: <c>IDistributedCache</c>, the raw connection and a readiness check (<c>ConnectionStrings:Redis</c>, and <c>Redis:InstanceName</c>, <c>abc:</c> for every Abc host so the keys
    /// of the system have one prefix). Every microservice and the commercial gateway have it; the host decides, as the public gateway does not.
    /// </summary>
    public static IServiceCollection AddAbcRedis(this IServiceCollection services, IConfiguration configuration)
        => services.AddBaseRedis(configuration);

    /// <summary>
    /// The middleware every Abc host has, in this order: the request log (outside the exception handler, so its response log has the status the client gets), the exception handling, Swagger,
    /// who the caller is and what the caller may do (when users are authenticated), and the health endpoints. The host maps its own endpoints after it.
    /// </summary>
    public static WebApplication UseAbcHosting(this WebApplication app, bool authenticateUsers = true)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseBaseRequestLogging();
        app.UseBaseExceptionHandling();
        app.UseBaseSwagger();
        if (authenticateUsers)
        {
            app.UseBaseAuthentication();
            app.UseBaseAuthorization();
        }

        // after the authentication: the language claim of the token is read
        app.UseBaseLocalization();
        app.MapBaseHealth();
        return app;
    }
}
