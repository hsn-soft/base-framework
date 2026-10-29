using System;
using System.Collections.Generic;
using HsnSoft.Base.Auditing;
using HsnSoft.Base.Data;
using MongoDB.Driver;

namespace HsnSoft.Base.MongoDB;

/// <summary>
/// What the repositories of a MongoDB service do for the concepts of the framework, as MongoDB has no change tracking to do it on save: <see cref="Visible{T}"/> adds the filters of
/// <see cref="ISoftDelete"/> and the filters of the <see cref="IMongoEntityConcept"/> services (the tenants) to a query, and <see cref="Created"/>, <see cref="Modified"/> and <see cref="SoftDeleted"/> fill the audit fields
/// and what the concepts write (the tenant) before a write. The filters read the elements <c>IsDeleted</c> and <c>TenantId</c>, which is what a class map that maps the properties by name gives.
/// </summary>
public sealed class MongoEntityConcepts(IAuditPropertySetter audit, IDataFilter filters, IEnumerable<IMongoEntityConcept>? concepts = null)
{
    private readonly IMongoEntityConcept[] _concepts = [.. concepts ?? []];

    /// <summary>
    /// <paramref name="where"/> (or everything) narrowed to what the caller may see: the deleted entities are left out, and so are the ones of tenants the caller may not see (when the service has tenants).
    /// A filter that <see cref="IDataFilter"/> switched off is not applied.
    /// </summary>
    public FilterDefinition<T> Visible<T>(FilterDefinition<T>? where = null)
    {
        FilterDefinitionBuilder<T> builder = Builders<T>.Filter;
        List<FilterDefinition<T>> parts = [where ?? builder.Empty];
        if (typeof(ISoftDelete).IsAssignableFrom(typeof(T)) && filters.IsEnabled<ISoftDelete>())
        {
            parts.Add(builder.Eq(nameof(ISoftDelete.IsDeleted), false));
        }

        foreach (IMongoEntityConcept concept in _concepts)
        {
            if (concept.Visible<T>() is { } part)
            {
                parts.Add(part);
            }
        }

        return builder.And(parts);
    }

    /// <summary>Before an insert: the creation fields and what the concepts write (the current tenant of an entity without one).</summary>
    public void Created(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        audit.SetCreation(entity);
        foreach (IMongoEntityConcept concept in _concepts)
        {
            concept.Created(entity);
        }
    }

    /// <summary>
    /// The update that marks every document it is applied to as deleted (<see cref="ISoftDelete"/>, and the modification fields), for a delete that loads nothing: what <see cref="SoftDeleted"/> does to an entity, written by the database.
    /// </summary>
    public UpdateDefinition<T> Deletion<T>()
    {
        UpdateDefinitionBuilder<T> builder = Builders<T>.Update;
        AuditStamp stamp = audit.Stamp();
        List<UpdateDefinition<T>> sets =
        [
            builder.Set(nameof(ISoftDelete.IsDeleted), true),
            builder.Set(nameof(IHasDeletionTime.DeletedAt), (DateTimeOffset?)stamp.At),
            builder.Set(nameof(IDeletionAuditedObject.DeletedBy), stamp.By),
        ];
        if (typeof(IModificationAuditedObject).IsAssignableFrom(typeof(T)))
        {
            sets.Add(builder.Set(nameof(IModificationAuditedObject.UpdatedAt), stamp.At));
            sets.Add(builder.Set(nameof(IModificationAuditedObject.UpdatedBy), stamp.By));
        }

        return builder.Combine(sets);
    }

    /// <summary>The update with the modification fields (<see cref="IModificationAuditedObject"/>) written beside it, for an update that loads nothing.</summary>
    public UpdateDefinition<T> Modification<T>(UpdateDefinition<T> update)
    {
        ArgumentNullException.ThrowIfNull(update);

        if (!typeof(IModificationAuditedObject).IsAssignableFrom(typeof(T)))
        {
            return update;
        }

        UpdateDefinitionBuilder<T> builder = Builders<T>.Update;
        AuditStamp stamp = audit.Stamp();
        return builder.Combine(update, builder.Set(nameof(IModificationAuditedObject.UpdatedAt), stamp.At), builder.Set(nameof(IModificationAuditedObject.UpdatedBy), stamp.By));
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
