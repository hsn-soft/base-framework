using HsnSoft.Base.AspNetCore;
using HsnSoft.Base.Authentication;
using HsnSoft.Base.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Edx.Shared.Hosting;

/// <summary>The shell every Edx host has, in the order it needs: see <see cref="AddEdxHosting"/> and <see cref="UseEdxHosting"/>.</summary>
public static class HostingRegister
{
    /// <summary>
    /// Registers what every Edx host shares: the masker and the console logger, the request and response logs (a gateway has <c>RequestLogging:Role</c> = <c>Gateway</c>), the
    /// answers of a web api (<c>BaseResponse</c>, the exception handler), Swagger and the health checks (section <c>Health</c>), and, for a host that reads the bearer token, the JWT validation (section <c>Jwt</c>).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Holds <c>RequestLogging</c>, <c>Swagger</c> and <c>Jwt</c>.</param>
    /// <param name="swaggerTitle">The title of the Swagger document of the host.</param>
    /// <param name="swaggerName">The name of the document (<c>v1</c>; <c>gateway</c> for a gateway).</param>
    /// <param name="authenticateUsers">Whether the host reads the bearer token. A microservice does, and its endpoints need it; the commercial gateway does, to know the user in its logs; the public gateway does not.</param>
    /// <param name="swaggerBearerButton">Whether the Swagger UI has an <c>Authorize</c> button for a bearer token.</param>
    public static IServiceCollection AddEdxHosting(
        this IServiceCollection services,
        IConfiguration configuration,
        string swaggerTitle,
        string swaggerName = "v1",
        bool authenticateUsers = true,
        bool swaggerBearerButton = false)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddBaseLogging()
            .AddBaseRequestLogging(configuration)
            .AddBaseWebApi()
            .AddBaseHealthChecks(configuration);   // the checks of the packages this host has, the section Health (a check is switched off by its name)

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
    /// The middleware every Edx host has, in this order: the request log (outside the exception handler, so its response log has the status the client gets), the exception handling, Swagger,
    /// who the caller is and what the caller may do (when users are authenticated), and the health endpoints. The host maps its own endpoints after it.
    /// </summary>
    public static WebApplication UseEdxHosting(this WebApplication app, bool authenticateUsers = true)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseBaseForwardedHeaders();   // first: what follows sees the caller the proxy saw
        app.UseBaseRequestLogging();
        app.UseBaseExceptionHandling();
        app.UseBaseSwagger();
        if (authenticateUsers)
        {
            app.UseBaseAuthentication();
            app.UseBaseAuthorization();
        }

        app.MapBaseHealth();
        app.MapBaseInfo();   // GET /: the service, the instance, the environment and the version
        return app;
    }
}
