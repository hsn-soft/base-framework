using System;
using HsnSoft.Base.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HsnSoft.Base.EventBus;

public static class TenantEventBusServiceCollectionExtensions
{
    /// <summary>Makes the tenant travel with the messages: written into the headers of what the service publishes, and the tenant of the handler of what it receives (<c>AddBaseMultiTenancy</c> is registered too).</summary>
    public static IServiceCollection AddBaseTenantEventBus(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddBaseMultiTenancy();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IEventHeaderProvider, TenantEventHeaderProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IEventHandlerScope, TenantEventHandlerScope>());
        return services;
    }
}
