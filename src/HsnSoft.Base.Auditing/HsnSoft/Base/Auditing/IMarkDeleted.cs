using System;

namespace HsnSoft.Base.Auditing;

/// <summary>An entity that takes the mark of its deletion. The persistence calls <see cref="MarkDeleted"/> when it is deleted, not the application.</summary>
public interface IMarkDeleted
{
    void MarkDeleted(DateTimeOffset at, Guid? by);
}
