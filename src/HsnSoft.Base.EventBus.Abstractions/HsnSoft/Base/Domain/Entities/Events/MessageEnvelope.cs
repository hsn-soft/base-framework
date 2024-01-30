using System;
using JetBrains.Annotations;

namespace HsnSoft.Base.Domain.Entities.Events;

public sealed record MessageEnvelope<T> where T : IIntegrationEventMessage
{
    public int HopLevel { get; set; }

    public Guid? ParentMessageId { get; set; }

    public Guid MessageId { get; set; }

    public DateTimeOffset MessageTime { get; set; }

    public T Message { get; set; }

    [CanBeNull]
    public string CorrelationId { get; set; }

    [CanBeNull]
    public string UserId { get; set; }
    [CanBeNull]
    public string UserRoleUniqueName { get; set; }

    [CanBeNull]
    public string Channel { get; set; }
    [CanBeNull]
    public string Producer { get; set; }
}

// Marker
public interface IIntegrationEventMessage
{
}