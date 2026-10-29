using System;

namespace HsnSoft.Base.Auditing;

/// <summary>Shows when a record was last changed (when it was made, if it never was).</summary>
public interface IHasModificationTime
{
    DateTimeOffset UpdatedAt { get; }
}
