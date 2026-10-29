using System;

namespace HsnSoft.Base.Auditing;

/// <summary>When a change was made and by whom (<see langword="null"/> when nobody signed in did).</summary>
public readonly record struct AuditStamp(DateTimeOffset At, Guid? By);

/// <summary>Fills the audit fields of an entity: the time from the clock and the user from <see cref="Security.ICurrentUser"/>. What does not implement the audit contracts is left as it is.</summary>
public interface IAuditPropertySetter
{
    /// <summary>For an entity that is being added: <see cref="IMarkCreated"/> and, as it is also the first change, <see cref="IMarkModified"/>.</summary>
    void SetCreation(object entity);

    /// <summary>For an entity that was changed: <see cref="IMarkModified"/>.</summary>
    void SetModification(object entity);

    /// <summary>The time and the user that a change made now has: for a change that is made without loading the entities (a set based update), which no entity can be marked by.</summary>
    AuditStamp Stamp();

    /// <summary>For an entity that is being deleted: <see cref="IMarkDeleted"/> (once, an <see cref="ISoftDelete"/> that is not deleted yet) and <see cref="IMarkModified"/>.</summary>
    void SetDeletion(object entity);
}
