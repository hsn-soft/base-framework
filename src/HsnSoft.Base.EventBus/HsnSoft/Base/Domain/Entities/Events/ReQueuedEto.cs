namespace HsnSoft.Base.Domain.Entities.Events;

/// <summary>
/// Puts a failed message back to one consumer: published straight to that consumer's queue, which the consumer reads like any event.
/// </summary>
/// <param name="ReQueuedMessageEnvelopeConsumer">The consumer to give the message to: <c>{ExchangeName}_{ConsumerClientName}</c>.</param>
/// <param name="ReQueuedMessageObject">The message to hand to its handler.</param>
/// <param name="ReQueuedMessageTypeName">The type name of the message.</param>
public sealed record ReQueuedEto(
    string ReQueuedMessageEnvelopeConsumer,
    object ReQueuedMessageObject,
    string ReQueuedMessageTypeName) : IIntegrationEventMessage;
