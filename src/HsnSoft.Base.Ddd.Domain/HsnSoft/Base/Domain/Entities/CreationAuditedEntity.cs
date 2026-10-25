using System;
using HsnSoft.Base.Auditing;

namespace HsnSoft.Base.Domain.Entities;

/// <summary>An entity that knows when it was made and by whom: the persistence calls <see cref="MarkCreated"/> when it is added.</summary>
public abstract class CreationAuditedEntity<TKey> : Entity<TKey>, ICreationAudited
{
    protected CreationAuditedEntity()
    {
    }

    protected CreationAuditedEntity(TKey id)
        : base(id)
    {
    }

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public void MarkCreated(DateTimeOffset at, Guid? by) => (CreatedAt, CreatedBy) = (at, by);
}
