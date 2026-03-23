using System;

namespace HsnSoft.Base.Domain.Entities.Events;

public sealed record MessageEnvelope<T> : ParentMessageEnvelope where T : IIntegrationEventMessage
{
    public Guid? ParentMessageId { get; set; }

    public T Message { get; set; }
}