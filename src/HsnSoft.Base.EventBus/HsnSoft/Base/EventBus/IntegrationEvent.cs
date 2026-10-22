using System;

namespace HsnSoft.Base.EventBus;

/// <summary>Base of every message that travels between services. Derive a record per business fact.</summary>
public abstract record IntegrationEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();

    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
