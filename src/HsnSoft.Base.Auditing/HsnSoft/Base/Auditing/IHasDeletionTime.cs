using System;

namespace HsnSoft.Base.Auditing;

/// <summary>Shows when a record that is not removed when it is deleted (<see cref="ISoftDelete"/>) was marked as deleted.</summary>
public interface IHasDeletionTime : ISoftDelete
{
    DateTimeOffset? DeletedAt { get; }
}
