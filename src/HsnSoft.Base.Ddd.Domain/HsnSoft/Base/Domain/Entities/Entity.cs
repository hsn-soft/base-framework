namespace HsnSoft.Base.Domain.Entities;

/// <summary>The base of an entity: it has an <see cref="Id"/> that only the entity (its factory method) sets.</summary>
public abstract class Entity<TKey> : IEntity<TKey>
{
    protected Entity()
    {
        Id = default!;
    }

    protected Entity(TKey id) => Id = id;

    public TKey Id { get; protected set; }

    public override string ToString() => $"[ENTITY: {GetType().Name}] Id = {Id}";
}
