using System;

namespace HsnSoft.Base.Auditing;

/// <summary>Shows when a record was made. An entity and its DTO both have it.</summary>
public interface IHasCreationTime
{
    DateTimeOffset CreatedAt { get; }
}
