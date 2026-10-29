using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HsnSoft.Base.MultiTenancy;

public static class TenantAuthenticationServiceCollectionExtensions
{
    /// <summary>Makes the claims of the token of a request the tenant of <see cref="ICurrentTenant"/> (<see cref="MultiTenancyServiceCollectionExtensions.AddBaseMultiTenancy"/> is registered too).</summary>
    public static IServiceCollection AddBaseTenantClaims(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddBaseMultiTenancy();
        services.AddHttpContextAccessor();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITenantSource, HttpContextTenantSource>());
        return services;
    }
}
