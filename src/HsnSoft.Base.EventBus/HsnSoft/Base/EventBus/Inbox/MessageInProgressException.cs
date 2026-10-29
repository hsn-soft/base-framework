using System;

namespace HsnSoft.Base.EventBus.Inbox;

/// <summary>
/// The inbox of the service says another handling holds this message and its lease has not run out. The delivery must not be acknowledged: the handling that holds it may be one that crashed (the broker brought the message back within seconds,
/// the lease is minutes), and acknowledging would lose the message. The broker project puts it back (RabbitMQ) or tries again after a pause (Kafka), until the other handling has ended or its lease has run out.
/// </summary>
public sealed class MessageInProgressException : Exception
{
    public MessageInProgressException()
        : base("Another handling holds the message and its lease has not run out.")
    {
    }
}
