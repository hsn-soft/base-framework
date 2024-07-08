using JetBrains.Annotations;

namespace HsnSoft.Base.Domain.Entities.Events;

public record ReQueuedEto(
    [NotNull] string ReQueuedMessageEnvelopeProducer,
    [NotNull] object ReQueuedMessageObject,
    [NotNull] string ReQueuedMessageTypeName
) : IIntegrationEventMessage
{
    [NotNull]
    public string ReQueuedMessageEnvelopeProducer { get; } = ReQueuedMessageEnvelopeProducer;

    [NotNull]
    public object ReQueuedMessageObject { get; } = ReQueuedMessageObject;

    [NotNull]
    public string ReQueuedMessageTypeName { get; } = ReQueuedMessageTypeName;
}