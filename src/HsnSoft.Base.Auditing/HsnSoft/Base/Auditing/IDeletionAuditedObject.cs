using System;

namespace HsnSoft.Base.Auditing;

/// <summary>Shows when a record was marked as deleted and by whom: the id of the user, <see langword="null"/> when nobody signed in did.</summary>
public interface IDeletionAuditedObject : IHasDeletionTime
{
    Guid? DeletedBy { get; }
}
