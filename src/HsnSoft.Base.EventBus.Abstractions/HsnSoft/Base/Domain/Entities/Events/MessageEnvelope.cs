using System;

namespace HsnSoft.Base.Domain.Entities.Events;

public sealed record MessageEnvelope<T> where T : IIntegrationEventMessage
{
    public Guid? RelatedMessageId { get; set; }

    public Guid MessageId { get; set; }

    public DateTimeOffset MessageTime { get; set; }

    public T Message { get; set; }

    public string? Producer { get; set; }

    public string? CorrelationId { get; set; }
}

// Marker
public interface IIntegrationEventMessage
{
}