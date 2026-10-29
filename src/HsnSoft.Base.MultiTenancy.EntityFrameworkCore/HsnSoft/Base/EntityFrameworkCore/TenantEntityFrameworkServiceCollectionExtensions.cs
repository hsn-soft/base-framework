using System;
using HsnSoft.Base.MultiTenancy;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HsnSoft.Base.EntityFrameworkCore;

public static class TenantEntityFrameworkServiceCollectionExtensions
{
    /// <summary>Registers <see cref="TenantEntityInterceptor"/> (the database provider puts it on the context with the other interceptors) and <c>AddBaseMultiTenancy</c>.</summary>
    public static IServiceCollection AddBaseMultiTenancyEntityFramework(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddBaseMultiTenancy();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IInterceptor, TenantEntityInterceptor>());
        return services;
    }
}
