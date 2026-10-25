using System;

namespace HsnSoft.Base.Auditing;

/// <summary>An entity that knows when it was last changed and by whom. The persistence calls <see cref="MarkModified"/> when it is added and every time it is changed.</summary>
public interface IModificationAudited
{
    DateTimeOffset UpdatedAt { get; }

    /// <summary>The id of the user who changed it last; <see langword="null"/> when nobody signed in did.</summary>
    Guid? UpdatedBy { get; }

    void MarkModified(DateTimeOffset at, Guid? by);
}
