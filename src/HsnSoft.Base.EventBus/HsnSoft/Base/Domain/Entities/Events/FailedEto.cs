using System;

namespace HsnSoft.Base.Domain.Entities.Events;

/// <summary>
/// Published to the service's error queue when a message could not be handled, so whoever manages failed events can see it and put it
/// back (see <see cref="ReQueuedEto"/>).
/// </summary>
/// <param name="FailedReason">The error message.</param>
/// <param name="FailedMessageEnvelopeTime">When the failed message was produced.</param>
/// <param name="FailedMessageObject">The failed message itself, when it could be read.</param>
/// <param name="FailedMessageTypeName">The type name of the failed message, when known.</param>
public sealed record FailedEto(
    string FailedReason,
    DateTime? FailedMessageEnvelopeTime,
    object? FailedMessageObject,
    string? FailedMessageTypeName) : IIntegrationEventMessage;
