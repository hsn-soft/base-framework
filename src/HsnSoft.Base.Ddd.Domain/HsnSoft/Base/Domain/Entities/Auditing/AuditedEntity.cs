using System;
using HsnSoft.Base.Auditing;

namespace HsnSoft.Base.Domain.Entities.Auditing;

[Serializable]
public abstract class AuditedEntity<TKey> : CreationAuditedEntity<TKey>, IAuditedObject
{
    public DateTime? LastModificationTime { get; set; }

    public Guid? LastModifierId { get; set; }

    protected AuditedEntity()
    {
    }

    protected AuditedEntity(TKey id)
        : base(id)
    {
    }
}