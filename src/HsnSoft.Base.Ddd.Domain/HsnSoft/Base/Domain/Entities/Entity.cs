using System;

namespace HsnSoft.Base.Domain.Entities;

[Serializable]
public abstract class Entity<TKey> : IEntity<TKey>
{
    public TKey Id { get; protected set; }

    protected Entity()
    {
    }

    protected Entity(TKey id) => Id = id;

    public object[] GetKeys() => [Id];

    public override string ToString() => $"[ENTITY: {GetType().Name}] Id = {Id}";
}