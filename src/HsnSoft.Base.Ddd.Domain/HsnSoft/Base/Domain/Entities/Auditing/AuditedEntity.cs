using System;
using HsnSoft.Base.Auditing;
using HsnSoft.Base.Auditing.Contracts;

namespace HsnSoft.Base.Domain.Entities.Auditing;

[Serializable]
public abstract class AuditedEntity<TKey> : CreationAuditedEntity<TKey>, IAuditedObject
{
    public DateTime LastModificationTime { get; protected set; }

    public Guid? LastModifierId { get; protected set; }

    protected AuditedEntity()
    {
    }

    protected AuditedEntity(TKey id)
        : base(id)
    {
    }
}