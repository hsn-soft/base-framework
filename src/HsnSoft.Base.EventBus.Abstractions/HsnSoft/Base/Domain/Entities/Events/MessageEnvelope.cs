using System;

namespace HsnSoft.Base.Domain.Entities.Events;

public sealed record MessageEnvelope<T> where T : IIntegrationEventMessage
{
    public int HopLevel { get; set; }

    public Guid? ParentMessageId { get; set; }

    public Guid MessageId { get; set; }

    public DateTimeOffset MessageTime { get; set; }

    public T Message { get; set; }

    public string? CorrelationId { get; set; }

    public string? UserId { get; set; }
    public string? UserRoleUniqueName { get; set; }

    public string? Channel { get; set; }
    public string? Producer { get; set; }
}

// Marker
public interface IIntegrationEventMessage
{
}