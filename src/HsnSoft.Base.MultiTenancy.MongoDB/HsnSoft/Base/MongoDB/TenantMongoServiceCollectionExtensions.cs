using System;
using HsnSoft.Base.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HsnSoft.Base.MongoDB;

public static class TenantMongoServiceCollectionExtensions
{
    /// <summary>Gives the repositories of the service the tenant filter and the tenant of a new document (<c>AddBaseMultiTenancy</c> is registered too).</summary>
    public static IServiceCollection AddBaseMultiTenancyMongo(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddBaseMultiTenancy();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IMongoEntityConcept, TenantMongoConcept>());
        return services;
    }
}
