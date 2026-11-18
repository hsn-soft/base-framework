using System.Collections.Generic;

namespace HsnSoft.Base.Domain.Entities;

/// <summary>The base of an entity: it has an <see cref="Id"/> that only the entity (its factory method) sets.</summary>
public abstract class Entity<TKey> : IEntity<TKey>, IKeyAssignable<TKey>
{
    protected Entity()
    {
        Id = default!;
    }

    protected Entity(TKey id) => Id = id;

    public TKey Id { get; protected set; }

    // the persistence gives a key to an entity that has none (EntityKeys): not part of what a caller of the entity sees
    bool IKeyAssignable<TKey>.AssignKeyIfEmpty(TKey key)
    {
        if (!EqualityComparer<TKey>.Default.Equals(Id, default!))
        {
            return false;
        }

        Id = key;
        return true;
    }

    public override string ToString() => $"[ENTITY: {GetType().Name}] Id = {Id}";
}
