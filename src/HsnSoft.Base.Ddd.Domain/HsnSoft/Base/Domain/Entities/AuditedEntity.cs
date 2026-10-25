using System;
using HsnSoft.Base.Auditing;

namespace HsnSoft.Base.Domain.Entities;

/// <summary>An entity with the creation and the last change audited: the persistence calls <see cref="MarkModified"/> when it is added and every time it is changed.</summary>
public abstract class AuditedEntity<TKey> : CreationAuditedEntity<TKey>, IModificationAudited
{
    protected AuditedEntity()
    {
    }

    protected AuditedEntity(TKey id)
        : base(id)
    {
    }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    public void MarkModified(DateTimeOffset at, Guid? by) => (UpdatedAt, UpdatedBy) = (at, by);
}
