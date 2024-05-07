using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace HsnSoft.Base.Domain.Entities;

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

    public  IEnumerable<DomainEventRecord> GetDomainEvents()
    {
        return _domainEvents;
    }

    public  void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    protected  void AddDomainEvent(object eventData)
    {
        _domainEvents.Add(new DomainEventRecord(eventData, EventOrderGenerator.GetNext()));
    }
}