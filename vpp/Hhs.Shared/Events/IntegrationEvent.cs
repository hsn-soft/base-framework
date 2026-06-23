namespace Hhs.Shared.Events;

public abstract record IntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public string? CorrelationId { get; init; }

    public string EventName { get; init; } = default!;
    public string Facility { get; init; } = default!;
}
