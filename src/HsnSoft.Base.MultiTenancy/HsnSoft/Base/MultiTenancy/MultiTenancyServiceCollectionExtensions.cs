using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HsnSoft.Base.MultiTenancy;

public static class MultiTenancyServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ICurrentTenant"/> (what a service with tenants asks for the tenant of the request). It reads the tenant from the <see cref="ITenantSource"/> services the host registered
    /// (the token of a request, the envelope of a message); what the host registered before is kept.
    /// </summary>
    public static IServiceCollection AddBaseMultiTenancy(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<ICurrentTenant, CurrentTenant>();
        return services;
    }
}
