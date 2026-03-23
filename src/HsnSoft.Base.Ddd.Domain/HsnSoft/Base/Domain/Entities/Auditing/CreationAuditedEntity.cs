using System;
using HsnSoft.Base.Auditing.Contracts;

namespace HsnSoft.Base.Domain.Entities.Auditing;

[Serializable]
public abstract class CreationAuditedEntity<TKey> : Entity<TKey>, ICreationAuditedObject
{
    public DateTime CreationTime { get; protected set; }

    public Guid? CreatorId { get; protected set; }

    protected CreationAuditedEntity()
    {
    }

    protected CreationAuditedEntity(TKey id)
        : base(id)
    {
    }
}