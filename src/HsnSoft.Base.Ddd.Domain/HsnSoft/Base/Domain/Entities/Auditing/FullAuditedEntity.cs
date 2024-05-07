using System;
using HsnSoft.Base.Auditing;

namespace HsnSoft.Base.Domain.Entities.Auditing;

[Serializable]
public abstract class FullAuditedEntity<TKey> : AuditedEntity<TKey>, IFullAuditedObject
{
    public bool IsDeleted { get; set; }

    public Guid? DeleterId { get; set; }

    public DateTime? DeletionTime { get; set; }

    protected FullAuditedEntity()
    {
    }

    protected FullAuditedEntity(TKey id)
        : base(id)
    {
    }
}