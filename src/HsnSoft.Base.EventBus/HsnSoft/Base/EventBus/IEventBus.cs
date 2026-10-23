using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities.Events;

namespace HsnSoft.Base.EventBus;

/// <summary>Publishes events. RabbitMQ, Kafka or any other broker implements it in its own project.</summary>
public interface IEventBus
{
    /// <summary>Publishes <paramref name="eventMessage"/> in an envelope.</summary>
    /// <param name="eventMessage">The message.</param>
    /// <param name="parentMessage">The message that caused this one. When left out, the message being handled right now (if any) is used, so the chain follows by itself.</param>
    /// <param name="correlationId">Ties the message to a request. Defaults to the parent's, then to what the registered header providers say.</param>
    /// <param name="isExchangeEvent"><see langword="true"/>: to every service that listens to the event. <see langword="false"/>: straight to this service's own queue of the event.</param>
    /// <param name="isReQueuePublish">Puts a failed message back to one consumer (see <see cref="ReQueuedEto"/>); counts in <see cref="ParentMessageEnvelope.ReQueuedCount"/>.</param>
    /// <param name="cancellationToken">Cancels the publish.</param>
    Task PublishAsync<TEventMessage>(
        TEventMessage eventMessage,
        ParentMessageEnvelope? parentMessage = null,
        string? correlationId = null,
        bool isExchangeEvent = true,
        bool isReQueuePublish = false,
        CancellationToken cancellationToken = default)
        where TEventMessage : IIntegrationEventMessage;
}
