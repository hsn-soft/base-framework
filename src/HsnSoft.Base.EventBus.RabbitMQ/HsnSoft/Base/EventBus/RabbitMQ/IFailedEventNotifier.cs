using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities.Events;
using JetBrains.Annotations;

namespace HsnSoft.Base.EventBus.RabbitMQ;

public interface IFailedEventNotifier
{
    /// <summary>
    /// Publishes a FailedEto by resolving the failed event's type/payload from its own raw envelope JSON
    /// via this microservice's subscription manager. Used when the consumer only has the raw message content
    /// on hand (e.g. a message this consumer is itself subscribed to failed before a handler could run).
    /// </summary>
    Task NotifyAsync([NotNull] string errorMessage, [NotNull] string failedEventName, [NotNull] string failedMessageContent);

    /// <summary>
    /// Publishes a FailedEto using an already-known type name + message object, skipping any subscription-manager
    /// lookup. Used when the caller already has the payload in hand and a lookup would/could fail
    /// (e.g. redispatching a ReQueuedEto whose original handler type may no longer be resolvable).
    /// </summary>
    Task NotifyDirectAsync([NotNull] string errorMessage, [CanBeNull] string failedMessageTypeName, [CanBeNull] object failedMessageObject, [CanBeNull] ParentMessageEnvelope parentContext);
}
