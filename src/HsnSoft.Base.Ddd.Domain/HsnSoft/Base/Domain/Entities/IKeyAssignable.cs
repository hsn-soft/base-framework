using System;
using System.Collections.Generic;

namespace HsnSoft.Base.Domain.Entities;

/// <summary>
/// An entity whose key the persistence may give when the caller gave none: the key of an entity is set by the entity only, so the persistence reaches it through this interface (implemented explicitly by <see cref="Entity{TKey}"/>), and only while the key is still empty.
/// A key the caller gave is never changed.
/// </summary>
public interface IKeyAssignable<in TKey>
{
    /// <summary>Gives the key when it is still empty (<c>default</c>), and says whether it did.</summary>
    bool AssignKeyIfEmpty(TKey key);
}

/// <summary>The keys the persistence makes for an entity that has none: a <see cref="Guid"/> (version 7: the first 48 bits are the time in milliseconds, so keys made later sort after keys made earlier, to the millisecond, and an index of keys stays compact). Another type of key (a number) is made by the database, not here.</summary>
public static class EntityKeys
{
    /// <summary>Gives the entity a new <see cref="Guid"/> when its key is empty (<c>Guid.Empty</c>); an entity that has a key, or a key that is not a <see cref="Guid"/>, is left as it is.</summary>
    public static void EnsureKey<TKey>(IEntity<TKey> entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (typeof(TKey) == typeof(Guid) && entity is IKeyAssignable<TKey> assignable && EqualityComparer<TKey>.Default.Equals(entity.Id, default!))
        {
            assignable.AssignKeyIfEmpty((TKey)(object)Guid.CreateVersion7());
        }
    }
}
