using System;

namespace HsnSoft.Base.Auditing;

/// <summary>An entity that takes the mark of its creation. The persistence calls <see cref="MarkCreated"/> when it is added, the application never does; a DTO has nothing to mark.</summary>
public interface IMarkCreated
{
    void MarkCreated(DateTimeOffset at, Guid? by);
}
