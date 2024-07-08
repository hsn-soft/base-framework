using JetBrains.Annotations;

namespace HsnSoft.Base.Domain.Entities.Events;

public record ReQueuedEto(
    [NotNull] string ReQueuedMessageEnvelopeConsumer,
    [NotNull] object ReQueuedMessageObject,
    [NotNull] string ReQueuedMessageTypeName
) : IIntegrationEventMessage
{
    [NotNull]
    public string ReQueuedMessageEnvelopeConsumer { get; } = ReQueuedMessageEnvelopeConsumer;

    [NotNull]
    public object ReQueuedMessageObject { get; } = ReQueuedMessageObject;

    [NotNull]
    public string ReQueuedMessageTypeName { get; } = ReQueuedMessageTypeName;
}