using System;

namespace HsnSoft.Base.Auditing;

/// <summary>An entity that takes the mark of its last change. The persistence calls <see cref="MarkModified"/> when it is added and every time it is changed.</summary>
public interface IMarkModified
{
    void MarkModified(DateTimeOffset at, Guid? by);
}
