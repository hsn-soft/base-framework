using System;

namespace HsnSoft.Base.Domain.Entities.Events;

/// <summary>A message with its envelope: what a handler receives.</summary>
public sealed record MessageEnvelope<T> : ParentMessageEnvelope, IHasParentMessageId
    where T : IIntegrationEventMessage
{
    /// <summary>The message that caused this one, if a handler produced it.</summary>
    public Guid? ParentMessageId { get; set; }

    public T Message { get; set; } = default!;
}

/// <summary>Lets the dispatcher read the parent of a message without knowing its type.</summary>
internal interface IHasParentMessageId
{
    Guid? ParentMessageId { get; }
}
