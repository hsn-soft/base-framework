using System.Reflection;
using System.Text.Json;
using Hhs.Shared.Contracts.EventInbox;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.EventBus.RabbitMQ;

namespace Hhs.Shared.Hosting.EventHandlers;

/// <summary>
/// Generic, framework-level handler for ReQueuedEto — automatically registered for every microservice
/// via UseEventBus (see Hhs.Shared.Hosting.Extensions.ApplicationBuilderExtensions), so no microservice
/// needs to write its own redelivery/replay code. On receiving a requeued message, it resolves the
/// original event type and dispatches it to whatever handler THIS microservice already has registered
/// for that type, via the same IEventDispatcher used for fresh message consumption.
///
/// Derives from the shared ApplicationEventHandlerBase like every other consumed event, so requeue
/// redispatches also get inbox-tracking/idempotency for free, consistent with the rest of the pattern.
/// ExecuteAsync never throws: every failure path here is reported explicitly via IFailedEventNotifier
/// rather than by propagating an exception, so this handler always looks like a "successful" dispatch
/// to whatever consumed the outer ReQueuedEto message — InboxStore.CompleteAsync always fires after.
/// </summary>
public sealed class ReQueuedEtoHandler(
    IEventInboxMessageManager inboxStore,
    IEventBusSubscriptionManager subscriptionsManager,
    IEventDispatcher dispatcher,
    IFailedEventNotifier failedEventNotifier
) : ApplicationEventHandlerBase<ReQueuedEto>(inboxStore)
{
    protected override async Task ExecuteAsync(MessageEnvelope<ReQueuedEto> @event, CancellationToken cancellationToken)
    {
        string originalTypeName = @event.Message.ReQueuedMessageTypeName; // raw CLR type name, e.g. "CustomerContentCreatedEto" — NOT trimmed

        try
        {
            string trimmedName = subscriptionsManager.EventNameGetter(originalTypeName);

            if (!subscriptionsManager.HasSubscriptionsForEvent(trimmedName))
            {
                await failedEventNotifier.NotifyDirectAsync(
                    errorMessage: $"No handler registered in this service for requeued event type '{originalTypeName}'.",
                    failedMessageTypeName: originalTypeName,
                    failedMessageObject: @event.Message.ReQueuedMessageObject,
                    parentContext: @event);
                return;
            }

            var eventType = subscriptionsManager.GetEventInfoByName(trimmedName)!.EventType;

            string rawJson = @event.Message.ReQueuedMessageObject is JsonElement jsonElement
                ? jsonElement.GetRawText()
                : JsonSerializer.Serialize(@event.Message.ReQueuedMessageObject);
            object originalMessage = JsonSerializer.Deserialize(rawJson, eventType);

            var envelopeType = typeof(MessageEnvelope<>).MakeGenericType(eventType);
            object envelope = BuildOriginalEnvelope(envelopeType, originalMessage, @event);

            await dispatcher.DispatchAsync(trimmedName, eventType, envelope);
        }
        catch (Exception ex)
        {
            // Only unexpected framework-level failures in the redispatch plumbing itself land here
            // (e.g. the original message could not be deserialized) — the inner handler's own business
            // logic failures already propagate out of IEventDispatcher and are reported separately.
            await failedEventNotifier.NotifyDirectAsync(
                errorMessage: ex.Message,
                failedMessageTypeName: originalTypeName,
                failedMessageObject: @event.Message.ReQueuedMessageObject,
                parentContext: @event);
        }
    }

    private static object BuildOriginalEnvelope(Type envelopeType, object originalMessage, MessageEnvelope<ReQueuedEto> outerEnvelope)
    {
        object envelope = Activator.CreateInstance(envelopeType)!;

        void Set(string propertyName, object value) => envelopeType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)!.SetValue(envelope, value);

        Set(nameof(MessageEnvelope<IIntegrationEventMessage>.Message), originalMessage);
        Set(nameof(ParentMessageEnvelope.MessageId), Guid.CreateVersion7());
        Set(nameof(ParentMessageEnvelope.MessageTime), DateTime.UtcNow);
        Set(nameof(MessageEnvelope<IIntegrationEventMessage>.ParentMessageId), outerEnvelope.MessageId);
        Set(nameof(ParentMessageEnvelope.CorrelationId), outerEnvelope.CorrelationId);
        Set(nameof(ParentMessageEnvelope.UserId), outerEnvelope.UserId);
        Set(nameof(ParentMessageEnvelope.UserRoles), outerEnvelope.UserRoles);
        Set(nameof(ParentMessageEnvelope.ClientLat), outerEnvelope.ClientLat);
        Set(nameof(ParentMessageEnvelope.ClientLong), outerEnvelope.ClientLong);
        Set(nameof(ParentMessageEnvelope.ClientChannel), outerEnvelope.ClientChannel);
        Set(nameof(ParentMessageEnvelope.ClientVersion), outerEnvelope.ClientVersion);
        Set(nameof(ParentMessageEnvelope.Producer), outerEnvelope.Producer);
        Set(nameof(ParentMessageEnvelope.HopLevel), (ushort)(outerEnvelope.HopLevel + 1));
        Set(nameof(ParentMessageEnvelope.ReQueuedCount), (ushort)(outerEnvelope.ReQueuedCount + 1));

        return envelope;
    }
}
