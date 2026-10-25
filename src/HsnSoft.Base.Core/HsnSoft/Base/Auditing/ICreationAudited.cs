using System;

namespace HsnSoft.Base.Auditing;

/// <summary>An entity that knows when it was made and by whom. The persistence calls <see cref="MarkCreated"/> when the entity is added.</summary>
public interface ICreationAudited
{
    DateTimeOffset CreatedAt { get; }

    /// <summary>The id of the user who made it; <see langword="null"/> when nobody signed in did.</summary>
    Guid? CreatedBy { get; }

    void MarkCreated(DateTimeOffset at, Guid? by);
}
