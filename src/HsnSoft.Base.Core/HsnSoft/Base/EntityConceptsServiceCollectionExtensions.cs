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
    /// Registers what the persistence needs for the concepts of an entity: the audit fields (<see cref="IAuditPropertySetter"/>), soft delete and tenants' filters (<see cref="IDataFilter"/>), the
    /// tenant of the request (<see cref="ICurrentTenant"/>) and the clock. The database providers (<c>AddBasePostgres</c>, <c>AddBaseMongo</c>, ...) call it; whatever the host registered before is kept
    /// (<see cref="Security.ICurrentUser"/> comes from the JWT authentication; without one, the user fields stay empty).
    /// </summary>
    public static IServiceCollection AddBaseEntityConcepts(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IDataFilter, DataFilter>();
        services.TryAddSingleton<ICurrentTenant, CurrentTenant>();
        services.TryAddSingleton<IAuditPropertySetter, AuditPropertySetter>();
        return services;
    }
}
