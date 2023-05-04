using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace HsnSoft.Base.Domain.Entities;

[Serializable]
public abstract class BasicAggregateRoot : Entity,
    IAggregateRoot,
    IGeneratesDomainEvents
{
    private readonly ICollection<DomainEventRecord> _domainEvents = new Collection<DomainEventRecord>();

    public virtual IEnumerable<DomainEventRecord> GetDomainEvents()
    {
        return _domainEvents;
    }

    public virtual void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    protected virtual void AddDomainEvent(object eventData)
    {
        _domainEvents.Add(new DomainEventRecord(eventData, EventOrderGenerator.GetNext()));
    }
}

[Serializable]
public abstract class BasicAggregateRoot<TKey> : Entity<TKey>,
    IAggregateRoot<TKey>,
    IGeneratesDomainEvents
{
    private readonly ICollection<DomainEventRecord> _domainEvents = new Collection<DomainEventRecord>();

    protected BasicAggregateRoot()
    {

    }

    protected BasicAggregateRoot(TKey id)
        : base(id)
    {

    }

    public virtual IEnumerable<DomainEventRecord> GetDomainEvents()
    {
        return _domainEvents;
    }

    public virtual void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    protected virtual void AddDomainEvent(object eventData)
    {
        _domainEvents.Add(new DomainEventRecord(eventData, EventOrderGenerator.GetNext()));
    }
}