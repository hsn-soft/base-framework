using System;
using System.Threading;
using HsnSoft.Base.Domain.Entities;
using MongoDB.Bson.Serialization;

namespace HsnSoft.Base.MongoDB;

/// <summary>
/// How the base classes of the entities are stored. The driver wants the members of a base class (<c>Id</c>, the audit fields) mapped in the map of that base class, not in the map of the entity that derives from it, so they are
/// registered here once: call <see cref="RegisterBases{TKey}"/> before the maps of the entities (<c>AddMongoRepository</c> does it as well, which is early enough as long as nothing was stored or read yet); the map of an entity then only
/// has its own members (<c>map.MapProperty(product =&gt; product.Name)</c>) and what an <c>IMultiTenant</c> entity adds (<c>TenantId</c>).
/// </summary>
public static class MongoEntityMaps
{
    private static readonly Lock s_lock = new();

    /// <summary>Registers the maps of <see cref="Entity{TKey}"/>, <see cref="CreationAuditedEntity{TKey}"/>, <see cref="AuditedEntity{TKey}"/> and <see cref="FullAuditedEntity{TKey}"/> for the key type; it can be called again.</summary>
    public static void RegisterBases<TKey>()
    {
        lock (s_lock)
        {
            Register<Entity<TKey>>(map => map.MapIdProperty(entity => entity.Id));
            Register<CreationAuditedEntity<TKey>>(map =>
            {
                map.MapProperty(entity => entity.CreatedAt);
                map.MapProperty(entity => entity.CreatedBy);
            });
            Register<AuditedEntity<TKey>>(map =>
            {
                map.MapProperty(entity => entity.UpdatedAt);
                map.MapProperty(entity => entity.UpdatedBy);
            });
            Register<FullAuditedEntity<TKey>>(map =>
            {
                map.MapProperty(entity => entity.IsDeleted);
                map.MapProperty(entity => entity.DeletedAt);
                map.MapProperty(entity => entity.DeletedBy);
            });
        }
    }

    private static void Register<T>(Action<BsonClassMap<T>> map)
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(T)))
        {
            BsonClassMap.RegisterClassMap(map);
        }
    }
}
