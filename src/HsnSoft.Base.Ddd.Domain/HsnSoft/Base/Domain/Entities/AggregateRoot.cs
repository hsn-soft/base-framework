using System;

namespace HsnSoft.Base.Domain.Entities;

[Serializable]
public abstract class AggregateRoot<TKey> : BasicAggregateRoot<TKey>,
    IHasConcurrencyStamp
{
    public  string ConcurrencyStamp { get; set; }

    protected AggregateRoot()
    {
        ConcurrencyStamp = Guid.CreateVersion7().ToString("N");
    }

    protected AggregateRoot(TKey id)
        : base(id)
    {
        ConcurrencyStamp = Guid.CreateVersion7().ToString("N");
    }
}