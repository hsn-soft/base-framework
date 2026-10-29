using System;
using HsnSoft.Base.Security;

namespace HsnSoft.Base.Auditing;

/// <summary>The default <see cref="IAuditPropertySetter"/>. Without a current user (a worker, a seed) the user fields are <see langword="null"/>.</summary>
public sealed class AuditPropertySetter(TimeProvider clock, ICurrentUser? currentUser = null) : IAuditPropertySetter
{
    public AuditStamp Stamp()
    {
        (DateTimeOffset now, Guid? by) = Now();
        return new AuditStamp(now, by);
    }

    public void SetCreation(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        (DateTimeOffset now, Guid? by) = Now();
        if (entity is IMarkCreated created)
        {
            created.MarkCreated(now, by);
        }

        if (entity is IMarkModified modified)
        {
            modified.MarkModified(now, by);
        }
    }

    public void SetModification(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity is IMarkModified modified)
        {
            (DateTimeOffset now, Guid? by) = Now();
            modified.MarkModified(now, by);
        }
    }

    public void SetDeletion(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        (DateTimeOffset now, Guid? by) = Now();
        if (entity is IMarkDeleted deleted && entity is ISoftDelete { IsDeleted: false })
        {
            deleted.MarkDeleted(now, by);
        }

        if (entity is IMarkModified modified)
        {
            modified.MarkModified(now, by);
        }
    }

    private (DateTimeOffset Now, Guid? By) Now() => (clock.GetUtcNow(), currentUser?.Id);
}
