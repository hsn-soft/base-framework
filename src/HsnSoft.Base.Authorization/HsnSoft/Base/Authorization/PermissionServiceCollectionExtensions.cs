using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HsnSoft.Base.Authorization;

public static class PermissionServiceCollectionExtensions
{
    /// <summary>
    /// Registers what asking a permission needs, with no web framework: the checker and the store of the grants in the distributed cache (an <c>IDistributedCache</c> must be registered by the host: <c>AddBaseRedis</c>). The service that manages the grants
    /// writes the store, every other one reads it. <c>ICurrentUser</c> is the one the host registers (<c>AddBaseJwtAuthentication</c> for a web api). An endpoint that is protected with <c>[PermissionAuthorize]</c> uses <c>AddBasePermissions</c> of
    /// <c>HsnSoft.Base.Authorization.AspNetCore</c> instead, which calls this one.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Holds the optional <c>Permissions</c> section (<see cref="PermissionOptions"/>).</param>
    public static IServiceCollection AddBasePermissionChecker(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<PermissionOptions>(configuration.GetSection(PermissionOptions.SectionName));
        services.AddMemoryCache();
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
