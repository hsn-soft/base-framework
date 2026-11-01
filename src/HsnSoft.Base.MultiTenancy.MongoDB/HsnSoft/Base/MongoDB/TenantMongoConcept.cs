using System;
using HsnSoft.Base.Data;
using HsnSoft.Base.MultiTenancy;
using MongoDB.Driver;

namespace HsnSoft.Base.MongoDB;

/// <summary>
/// The tenants of the repositories of a MongoDB service: a query of an <see cref="IMultiTenant"/> entity is narrowed to the tenants the caller may see (the system tenant sees every tenant; without a tenant nothing of a tenant is seen) and a
/// query of an <see cref="IScopeSubscription"/> entity to the scope keys the caller's token lists (without any nothing of a scope is seen), unless <see cref="IDataFilter"/> switched the filter off, and a new document gets the current tenant when it has none. It reads the element <c>TenantId</c>.
/// </summary>
internal sealed class TenantMongoConcept(ICurrentTenant tenant, IDataFilter filters) : IMongoEntityConcept
{
    public FilterDefinition<T>? Visible<T>()
    {
        bool ofTenant = typeof(IMultiTenant).IsAssignableFrom(typeof(T));
        bool ofScope = typeof(IScopeSubscription).IsAssignableFrom(typeof(T));
        if (ofTenant && ofScope)
        {
            // one isolation or the other, never both (a tenant owns its users and roles, a subscription owns its content)
            throw new InvalidOperationException($"{typeof(T).Name} is both IMultiTenant and IScopeSubscription: an entity is isolated by its tenant or by its scope, not by both.");
        }

        if (tenant.IsSystemTenant)
        {
            return null;
        }

        if (ofTenant && filters.IsEnabled<IMultiTenant>())
        {
            return Builders<T>.Filter.In(nameof(IMultiTenant.TenantId), tenant.AllowedTenantIds);
        }

        return ofScope && filters.IsEnabled<IScopeSubscription>() ? Builders<T>.Filter.In(nameof(IScopeSubscription.ScopeKey), tenant.AllowedScopeKeys) : null;
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
