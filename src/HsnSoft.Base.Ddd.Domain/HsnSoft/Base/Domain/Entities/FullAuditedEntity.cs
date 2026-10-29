using System;
using HsnSoft.Base.Auditing;

namespace HsnSoft.Base.Domain.Entities;

/// <summary>An audited entity that is not removed when it is deleted: the persistence marks it (<see cref="MarkDeleted"/>) and the lists do not show it.</summary>
public abstract class FullAuditedEntity<TKey> : AuditedEntity<TKey>, IFullAuditedObject, IMarkDeleted
{
    protected FullAuditedEntity()
    {
    }

    protected FullAuditedEntity(TKey id)
        : base(id)
    {
    }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public Guid? DeletedBy { get; private set; }

    public void MarkDeleted(DateTimeOffset at, Guid? by) => (IsDeleted, DeletedAt, DeletedBy) = (true, at, by);
}
