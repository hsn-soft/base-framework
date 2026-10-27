using System;
using HsnSoft.Base.Data;
using HsnSoft.Base.MultiTenancy;
using MongoDB.Driver;

namespace HsnSoft.Base.MongoDB;

/// <summary>
/// The tenants of the repositories of a MongoDB service: a query of an <see cref="IMultiTenant"/> entity is narrowed to the tenants the caller may see (the system tenant sees every tenant; without a tenant nothing of a tenant is seen),
/// unless <see cref="IDataFilter"/> switched the filter off, and a new document gets the current tenant when it has none. It reads the element <c>TenantId</c>.
/// </summary>
internal sealed class TenantMongoConcept(ICurrentTenant tenant, IDataFilter filters) : IMongoEntityConcept
{
    public FilterDefinition<T>? Visible<T>()
    {
        if (!typeof(IMultiTenant).IsAssignableFrom(typeof(T)) || !filters.IsEnabled<IMultiTenant>() || tenant.IsSystemTenant)
        {
            return null;
        }

        return Builders<T>.Filter.In(nameof(IMultiTenant.TenantId), tenant.AllowedTenantIds);
    }

    public void Created(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity is IMultiTenant multiTenant && tenant.Id is { } current)
        {
            TenantAssigner.Assign(multiTenant, current);
        }
    }
}
