using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities.Events;

namespace HsnSoft.Base.EventBus.RabbitMQ;

/// <summary>Tells whoever manages failed events (the error client) about a message that could not be handled: publishes a <see cref="FailedEto"/> to its queue.</summary>
public interface IFailedEventNotifier
{
    /// <summary>
    /// For a message the consumer only has as raw JSON (it failed before a handler could run): the type and the message are recovered
    /// from the JSON through the service's own subscriptions; when that fails, the failure reason says so.
    /// </summary>
    Task NotifyAsync(string errorMessage, string failedEventName, string failedMessageContent, CancellationToken cancellationToken = default);

    /// <summary>For a caller that already has the message: no lookup that could fail again (e.g. redispatching a requeued message).</summary>
    Task NotifyDirectAsync(string errorMessage, string? failedMessageTypeName, object? failedMessageObject, ParentMessageEnvelope? parentContext, CancellationToken cancellationToken = default);
}
