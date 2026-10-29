using System;

namespace HsnSoft.Base.Auditing;

/// <summary>Shows when a record was last changed and by whom: the id of the user, <see langword="null"/> when nobody signed in did.</summary>
public interface IModificationAuditedObject : IHasModificationTime
{
    Guid? UpdatedBy { get; }
}
