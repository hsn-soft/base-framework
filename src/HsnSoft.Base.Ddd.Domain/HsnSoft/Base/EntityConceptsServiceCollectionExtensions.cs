using System;
using HsnSoft.Base.Auditing;
using HsnSoft.Base.Data;
using HsnSoft.Base.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HsnSoft.Base;

public static class EntityConceptsServiceCollectionExtensions
{
    /// <summary>
    /// Registers what the persistence needs for the concepts of an entity: the audit fields (<see cref="IAuditPropertySetter"/> and the clock, <see cref="AuditingServiceCollectionExtensions.AddBaseAuditing"/>), soft delete and tenants'
    /// filters (<see cref="IDataFilter"/>, <see cref="DataServiceCollectionExtensions.AddBaseDataFilter"/>) and the tenant of the request (<see cref="ICurrentTenant"/>). The database providers (<c>AddBaseEntityFramework</c>, <c>AddBaseMongo</c>, ...)
    /// call it; whatever the host registered before is kept (<see cref="Security.ICurrentUser"/> comes from the JWT authentication; without one, the user fields stay empty).
    /// </summary>
    public static IServiceCollection AddBaseEntityConcepts(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddBaseAuditing();
        services.AddBaseDataFilter();
        services.TryAddSingleton<ICurrentTenant, CurrentTenant>();
        return services;
    }
}
