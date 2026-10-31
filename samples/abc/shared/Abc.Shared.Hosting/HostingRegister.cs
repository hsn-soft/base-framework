using HsnSoft.Base.AspNetCore;
using HsnSoft.Base.Authentication;
using HsnSoft.Base.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Abc.Shared.Hosting;

/// <summary>The shell every Abc host has, in the order it needs: see <see cref="AddAbcHosting"/> and <see cref="UseAbcHosting"/>.</summary>
public static class HostingRegister
{
    /// <summary>
    /// Registers what every Abc host shares: the masker and the console logger, the request and response logs (a gateway has <c>RequestLogging:Role</c> = <c>Gateway</c>), the
    /// answers of a web api (<c>BaseResponse</c>, the exception handler), Swagger and the health checks (section <c>Health</c>). Who the caller is and what the caller may do are the choice of the host: <c>AddAbcAuthentication</c> and <c>AddAbcAuthorization</c> (<c>AddEdxAuthentication</c> and <c>AddEdxAuthorization</c>).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Holds <c>RequestLogging</c>, <c>Swagger</c> and <c>Jwt</c>.</param>
    /// <param name="swaggerTitle">The title of the Swagger document of the host.</param>
    /// <param name="swaggerName">The name of the document (<c>v1</c>; <c>gateway</c> for a gateway).</param>
    /// <param name="swaggerBearerButton">Whether the Swagger UI has an <c>Authorize</c> button for a bearer token.</param>
    public static IServiceCollection AddAbcHosting(
        this IServiceCollection services,
        IConfiguration configuration,
        string swaggerTitle,
        string swaggerName = "v1",
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

        return services;
    }

    /// <summary>
    /// Reads the bearer token and knows who the caller is (section <c>Jwt</c>: the issuer, the <c>Audience</c> of this host, the public key of the auth server). A host that does not read tokens (the public gateway, a host that is open to its callers) does not call it, and does not call
    /// <c>UseBaseAuthentication</c>.
    /// </summary>
    public static IServiceCollection AddAbcAuthentication(this IServiceCollection services, IConfiguration configuration)
        => services.AddBaseJwtAuthentication(configuration);

    /// <summary>
    /// The standard authorization: the policies <c>[Authorize]</c> asks for. A host that does not authorize (a gateway only reads the token) does not call it, and does not call <c>UseBaseAuthorization</c>.
    /// </summary>
    public static IServiceCollection AddAbcAuthorization(this IServiceCollection services)
        => services.AddBaseAuthorization();

    /// <summary>
    /// The middleware every Abc host has, in this order: the proxies it believes (<c>Hosting:ForwardedHeaders</c>), the request log (outside the exception handler, so its response log has the status the client gets), the exception handling, Swagger,
    /// and the health endpoints and the root. The host adds who the caller is (<c>UseBaseAuthentication</c>) and what the caller may do (<c>UseBaseAuthorization</c>) after it, when it has them, and maps its own endpoints.
    /// </summary>
    public static WebApplication UseAbcHosting(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseBaseForwardedHeaders();   // first: what follows sees the caller the proxy saw
        app.UseBaseRequestLogging();
        app.UseBaseExceptionHandling();
        app.UseBaseSwagger();
        app.MapBaseHealth();
        app.MapBaseInfo();   // GET /: the service, the instance, the environment and the version
        return app;
    }
}
