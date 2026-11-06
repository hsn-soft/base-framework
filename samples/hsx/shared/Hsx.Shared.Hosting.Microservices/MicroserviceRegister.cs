using HsnSoft.Base.Authorization;
using HsnSoft.Base.MultiTenancy;
using Hsx.Shared.Helper;
using Hsx.Shared.Hosting.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hsx.Shared.Hosting.Microservices;

/// <summary>The shell of every Hsx microservice: the shared hosting with the bearer token required and a Swagger that can send it. The service adds its own parts (its stores, its application services).</summary>
public static class MicroserviceRegister
{
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Holds <c>RequestLogging</c>, <c>Swagger</c> and <c>Jwt</c>.</param>
    /// <param name="swaggerTitle">The title of the service's Swagger document.</param>
    /// <remarks>What a service may do without (its texts in several languages, Redis) it chooses itself: <c>AddBaseLocalization</c> and <c>UseBaseLocalization</c>, <c>AddBaseRedis</c>.</remarks>
    public static IServiceCollection AddHsxMicroservice(this IServiceCollection services, IConfiguration configuration, string swaggerTitle)
        => services.AddHsxHttpHosting(configuration, swaggerTitle, swaggerBearerButton: true);

    /// <summary>
    /// Turns on <c>[PermissionAuthorize]</c> in the service: what the caller (the user, and its roles) was granted is read from the store the identity service writes (section <c>Permissions</c>; the host has Redis, <c>AddBaseRedis</c>), under the tenant of the
    /// request, because the roles of a tenant are its own (<see cref="HsxPermissionScopes"/>). The tenant, the tenants under it and the scopes it may see are what the claims of the token say (<c>AddBaseTenantClaims</c>, which this call adds: the Hsx system is multi-tenant, so every service that checks permissions needs them, and the host does not repeat it).
    /// </summary>
    public static IServiceCollection AddHsxPermissions(this IServiceCollection services, IConfiguration configuration)
        => services
            .AddBaseTenantClaims()
            .AddBasePermissions(configuration)
            .AddPermissionScope<TenantPermissionScope>();

    /// <summary>The shared middleware (see <see cref="HttpHostingRegister.UseHsxHttpHosting"/>); the service then maps its controllers with <c>MapControllers()</c>.</summary>
    public static WebApplication UseHsxMicroservice(this WebApplication app)
        => app.UseHsxHttpHosting();
}
