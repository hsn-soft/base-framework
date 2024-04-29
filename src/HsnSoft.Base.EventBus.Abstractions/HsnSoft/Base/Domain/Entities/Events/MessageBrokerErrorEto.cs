using System;
using JetBrains.Annotations;

namespace HsnSoft.Base.Domain.Entities.Events;

public record MessageBrokerErrorEto(
    DateTime ErrorTime,
    [NotNull] string ErrorMessage,
    [NotNull] string FailedEventName,
    [CanBeNull] string FailedMessageTypeName,
    [CanBeNull] dynamic FailedMessageObject,
    [CanBeNull]  DateTimeOffset? FailedMessageEnvelopeTime) : IIntegrationEventMessage
{
    public DateTime ErrorTime { get; } = ErrorTime;

    [NotNull]
    public string ErrorMessage { get; } = ErrorMessage;

    [NotNull]
    public string FailedEventName { get; } = FailedEventName;

    [CanBeNull]
    public string FailedMessageTypeName { get; } = FailedMessageTypeName;

    [CanBeNull]
    public dynamic FailedMessageObject { get; } = FailedMessageObject;

    [CanBeNull]
    public DateTimeOffset? FailedMessageEnvelopeTime { get; } = FailedMessageEnvelopeTime;
}