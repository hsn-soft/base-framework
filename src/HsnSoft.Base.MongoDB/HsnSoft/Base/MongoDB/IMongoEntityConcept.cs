using MongoDB.Driver;

namespace HsnSoft.Base.MongoDB;

/// <summary>
/// A concept a package adds to the repositories of a MongoDB service, beside the audit fields and soft delete that <see cref="MongoEntityConcepts"/> does itself: the tenants of
/// <c>HsnSoft.Base.MultiTenancy.MongoDB</c> are one. Register it as a singleton; <see cref="MongoEntityConcepts"/> asks every one on each query and each insert.
/// </summary>
public interface IMongoEntityConcept
{
    /// <summary>The filter this concept puts on a query of <typeparamref name="T"/>, or <see langword="null"/> when it has none for it (the entity does not have the concept, or its filter is switched off).</summary>
    FilterDefinition<T>? Visible<T>();

    /// <summary>Before an insert: what this concept writes into <paramref name="entity"/> (the tenant of a new entity).</summary>
    void Created(object entity);
}
