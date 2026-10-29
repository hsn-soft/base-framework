using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HsnSoft.Base.Authorization;

public static class PermissionServiceCollectionExtensions
{
    /// <summary>
    /// Turns on permission authorization: the policies <c>[PermissionAuthorize]</c> asks for, the handler, the checker and the store of the grants in the distributed cache (an <c>IDistributedCache</c> must be registered by the host:
    /// <c>AddBaseRedis</c>). The service that manages the grants writes the store, every other one reads it. <c>ICurrentUser</c> is the one <c>AddBaseJwtAuthentication</c> registers.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Holds the optional <c>Permissions</c> section (<see cref="PermissionOptions"/>).</param>
    public static IServiceCollection AddBasePermissions(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<PermissionOptions>(configuration.GetSection(PermissionOptions.SectionName));
        services.AddMemoryCache();
        services.AddAuthorization();
        services.Replace(ServiceDescriptor.Singleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>());
        services.AddScoped<IAuthorizationHandler, PermissionRequirementHandler>();
        services.TryAddSingleton<IPermissionStore, DistributedPermissionStore>();
        services.TryAddScoped<IPermissionChecker, PermissionChecker>();
        return services;
    }

    /// <summary>Says in which scope the grants of a caller are kept (the tenant, for a system with tenants): see <see cref="IPermissionScopeProvider"/>.</summary>
    public static IServiceCollection AddPermissionScope<TProvider>(this IServiceCollection services)
        where TProvider : class, IPermissionScopeProvider
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IPermissionScopeProvider, TProvider>();
        return services;
    }
}
