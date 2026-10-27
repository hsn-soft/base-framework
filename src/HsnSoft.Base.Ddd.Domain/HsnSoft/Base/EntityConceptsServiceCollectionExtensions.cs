using System;
using HsnSoft.Base.Auditing;
using HsnSoft.Base.Data;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base;

public static class EntityConceptsServiceCollectionExtensions
{
    /// <summary>
    /// Registers what the persistence needs for the concepts of an entity: the audit fields (<see cref="IAuditPropertySetter"/> and the clock, <see cref="AuditingServiceCollectionExtensions.AddBaseAuditing"/>), the filters of soft delete
    /// (<see cref="IDataFilter"/>, <see cref="DataServiceCollectionExtensions.AddBaseDataFilter"/>). Tenants are not here: a service that has them adds <c>AddBaseMultiTenancy</c> itself. The database providers (<c>AddBaseEntityFramework</c>, <c>AddBaseMongo</c>, ...)
    /// call it; whatever the host registered before is kept (<see cref="Security.ICurrentUser"/> comes from the JWT authentication; without one, the user fields stay empty).
    /// </summary>
    public static IServiceCollection AddBaseEntityConcepts(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddBaseAuditing();
        services.AddBaseDataFilter();
        return services;
    }
}
