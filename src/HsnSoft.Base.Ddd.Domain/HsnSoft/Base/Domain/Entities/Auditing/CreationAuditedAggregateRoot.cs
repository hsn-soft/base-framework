using System;
using HsnSoft.Base.Auditing;

namespace HsnSoft.Base.Domain.Entities.Auditing;

[Serializable]
public abstract class CreationAuditedAggregateRoot<TKey> : AggregateRoot<TKey>, ICreationAuditedObject
{
    public DateTime CreationTime { get; set; }

    public Guid? CreatorId { get; set; }

    protected CreationAuditedAggregateRoot()
    {
    }

    protected CreationAuditedAggregateRoot(TKey id)
        : base(id)
    {
    }
}