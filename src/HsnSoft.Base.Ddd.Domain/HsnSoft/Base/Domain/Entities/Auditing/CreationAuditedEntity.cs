using System;
using HsnSoft.Base.Auditing;

namespace HsnSoft.Base.Domain.Entities.Auditing;

[Serializable]
public abstract class CreationAuditedEntity<TKey> : Entity<TKey>, ICreationAuditedObject
{
    protected CreationAuditedEntity()
    {
    }

    protected CreationAuditedEntity(TKey id)
        : base(id)
    {
    }

    public DateTime CreationTime { get; protected set; }

    public Guid? CreatorId { get; protected set; }
}