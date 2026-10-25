using System;

namespace HsnSoft.Base;

/// <summary>
/// An entity that is not removed when it is deleted: it is marked as deleted, and the lists no longer show it (the query filter of the persistence;
/// <see cref="Data.IDataFilter"/> lets a piece of code see the deleted ones too). Who deleted it and when is kept.
/// </summary>
public interface ISoftDelete
{
    bool IsDeleted { get; }

    DateTimeOffset? DeletedAt { get; }

    /// <summary>The id of the user who deleted it; <see langword="null"/> when nobody signed in did.</summary>
    Guid? DeletedBy { get; }

    /// <summary>Marks the entity as deleted. Called by the persistence when the entity is deleted, not by the application.</summary>
    void MarkDeleted(DateTimeOffset at, Guid? by);
}
