using System;
using JetBrains.Annotations;

namespace HsnSoft.Base.Domain.Entities.Events;

public record FailedEto(
    [NotNull] string FailedReason,
    [CanBeNull] string FailedMessageEnvelopeProducer,
    [CanBeNull] DateTime? FailedMessageEnvelopeTime,
    [CanBeNull] dynamic FailedMessageObject,
    [CanBeNull] string FailedMessageTypeName
) : IIntegrationEventMessage
{
    [NotNull]
    public string FailedReason { get; } = FailedReason;

    [CanBeNull]
    public string FailedMessageEnvelopeProducer { get; } = FailedMessageEnvelopeProducer;

    [CanBeNull]
    public DateTime? FailedMessageEnvelopeTime { get; } = FailedMessageEnvelopeTime;

    [CanBeNull]
    public object FailedMessageObject { get; } = FailedMessageObject;

    [CanBeNull]
    public string FailedMessageTypeName { get; } = FailedMessageTypeName;
}