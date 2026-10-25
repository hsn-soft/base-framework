using System;
using System.Collections.Generic;
using HsnSoft.Base.Auditing;
using HsnSoft.Base.Data;
using HsnSoft.Base.MultiTenancy;
using MongoDB.Driver;

namespace HsnSoft.Base.MongoDB;

/// <summary>
/// What the repositories of a MongoDB service do for the concepts of the framework, as MongoDB has no change tracking to do it on save: <see cref="Visible{T}"/> adds the filters of
/// <see cref="ISoftDelete"/> and <see cref="IMultiTenant"/> to a query, and <see cref="Created"/>, <see cref="Modified"/> and <see cref="SoftDeleted"/> fill the audit fields and the tenant before a write.
/// The filters read the elements <c>IsDeleted</c> and <c>TenantId</c>, which is what a class map that maps the properties by name gives.
/// </summary>
public sealed class MongoEntityConcepts(IAuditPropertySetter audit, ICurrentTenant tenant, IDataFilter filters)
{
    /// <summary>
    /// <paramref name="where"/> (or everything) narrowed to what the caller may see: the deleted entities are left out, and so are the ones of tenants the caller may not see
    /// (nothing is seen without a tenant, unless it is the system tenant). A filter that <see cref="IDataFilter"/> switched off is not applied.
    /// </summary>
    public FilterDefinition<T> Visible<T>(FilterDefinition<T>? where = null)
    {
        FilterDefinitionBuilder<T> builder = Builders<T>.Filter;
        List<FilterDefinition<T>> parts = [where ?? builder.Empty];
        if (typeof(ISoftDelete).IsAssignableFrom(typeof(T)) && filters.IsEnabled<ISoftDelete>())
        {
            parts.Add(builder.Eq(nameof(ISoftDelete.IsDeleted), false));
        }

        if (typeof(IMultiTenant).IsAssignableFrom(typeof(T)) && filters.IsEnabled<IMultiTenant>() && !tenant.IsSystemTenant)
        {
            parts.Add(builder.In(nameof(IMultiTenant.TenantId), tenant.AllowedTenantIds));
        }

        return builder.And(parts);
    }

    /// <summary>Before an insert: the creation fields and, for an <see cref="IMultiTenant"/> entity without a tenant, the current tenant.</summary>
    public void Created(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        audit.SetCreation(entity);
        if (entity is IMultiTenant { TenantId: var tenantId } multiTenant && tenantId == Guid.Empty && tenant.Id is { } current)
        {
            multiTenant.AssignTenant(current);
        }
    }

    /// <summary>Before a replace: the modification fields.</summary>
    public void Modified(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        audit.SetModification(entity);
    }

    /// <summary>
    /// Before a delete: an <see cref="ISoftDelete"/> entity is marked as deleted and <see langword="true"/> is returned, so the repository replaces the document instead of removing it;
    /// <see langword="false"/> for any other, which is removed.
    /// </summary>
    public bool SoftDeleted(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity is not ISoftDelete)
        {
            return false;
        }

        audit.SetDeletion(entity);
        return true;
    }
}
