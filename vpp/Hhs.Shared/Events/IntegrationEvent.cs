namespace Hhs.Shared.Events;

public abstract record IntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public Guid CorrelationId { get; init; } = Guid.NewGuid();

    public Guid? CustomerContentId { get; init; }
    public Guid? AnalysisContentId { get; init; }

    public string ContentProcessType { get; init; } = default!;
    public string EventName { get; init; } = default!;
    public string Facility { get; init; } = default!;
    public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;

    public bool IsManual { get; init; }
}