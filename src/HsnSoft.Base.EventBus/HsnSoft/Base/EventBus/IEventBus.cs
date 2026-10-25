using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities.Events;

namespace HsnSoft.Base.EventBus;

/// <summary>Publishes events. RabbitMQ, Kafka or any other broker implements it in its own project.</summary>
public interface IEventBus
{
    /// <summary>
    /// Publishes <paramref name="eventMessage"/> in an envelope to <b>every service that listens to the event</b> (the publisher does not know, and does not care, how many). This is what an event is: something that happened.
    /// </summary>
    /// <param name="eventMessage">The message.</param>
    /// <param name="parentMessage">The message that caused this one. When left out, the message being handled right now (if any) is used, so the chain follows by itself.</param>
    /// <param name="correlationId">Ties the message to a request. Defaults to the parent's, then to what the registered header providers say.</param>
    /// <param name="cancellationToken">Cancels the publish.</param>
    Task PublishAsync<TEventMessage>(
        TEventMessage eventMessage,
        ParentMessageEnvelope? parentMessage = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
        where TEventMessage : IIntegrationEventMessage;

    /// <summary>
    /// Sends <paramref name="eventMessage"/> to <b>one service</b>: straight to its queue of the event, so nobody else gets it. This is what a command is: a service is asked to do something (and the messages that a failed handling
    /// puts back to the consumer that failed). The service reads it as it reads any event; the queue is made if it is not there yet.
    /// </summary>
    /// <param name="eventMessage">The message.</param>
    /// <param name="toService">The <c>ConsumerClientName</c> of the service that gets it (<c>shipment-service</c>).</param>
    /// <param name="parentMessage">The message that caused this one (see <see cref="PublishAsync{TEventMessage}"/>).</param>
    /// <param name="correlationId">Ties the message to a request (see <see cref="PublishAsync{TEventMessage}"/>).</param>
    /// <param name="isReQueuePublish">Puts a failed message back to the consumer that failed (see <see cref="ReQueuedEto"/>); counts in <see cref="ParentMessageEnvelope.ReQueuedCount"/>.</param>
    /// <param name="cancellationToken">Cancels the send.</param>
    Task SendAsync<TEventMessage>(
        TEventMessage eventMessage,
        string toService,
        ParentMessageEnvelope? parentMessage = null,
        string? correlationId = null,
        bool isReQueuePublish = false,
        CancellationToken cancellationToken = default)
        where TEventMessage : IIntegrationEventMessage;
}
