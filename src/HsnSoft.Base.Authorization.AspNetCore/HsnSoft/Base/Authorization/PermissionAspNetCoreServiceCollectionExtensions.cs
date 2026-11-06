using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HsnSoft.Base.Authorization;

public static class PermissionAspNetCoreServiceCollectionExtensions
{
    /// <summary>
    /// Turns on permission authorization for the endpoints: the policies <c>[PermissionAuthorize]</c> asks for, the handler, and what the checker needs (<see cref="PermissionServiceCollectionExtensions.AddBasePermissionChecker"/>: the checker and the store of the grants in the
    /// distributed cache; an <c>IDistributedCache</c> must be registered by the host: <c>AddBaseRedis</c>). <c>ICurrentUser</c> is the one <c>AddBaseJwtAuthentication</c> registers.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Holds the optional <c>Permissions</c> section (<see cref="PermissionOptions"/>).</param>
    public static IServiceCollection AddBasePermissions(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddBasePermissionChecker(configuration);
        services.AddAuthorization();
        services.Replace(ServiceDescriptor.Singleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>());
        services.AddScoped<IAuthorizationHandler, PermissionRequirementHandler>();
        return services;
    }
}
