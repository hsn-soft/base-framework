using System;
using HsnSoft.Base.Auditing;

namespace HsnSoft.Base.Domain.Entities.Auditing;

[Serializable]
public abstract class AuditedAggregateRoot<TKey> : CreationAuditedAggregateRoot<TKey>, IAuditedObject
{
    public DateTime? LastModificationTime { get; set; }

    public Guid? LastModifierId { get; set; }

    protected AuditedAggregateRoot()
    {
    }

    protected AuditedAggregateRoot(TKey id)
        : base(id)
    {
    }
}