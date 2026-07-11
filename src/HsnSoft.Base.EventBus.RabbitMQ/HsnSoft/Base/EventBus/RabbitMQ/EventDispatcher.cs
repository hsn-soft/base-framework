using System;
using System.Diagnostics;
using System.Threading.Tasks;
using HsnSoft.Base.Data;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus.Logging;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Subscribe;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.EventBus.RabbitMQ;

/// <summary>
/// Shared per-handler resolve+invoke logic, reused by both fresh message consumption
/// (<see cref="RabbitMqConsumer"/>) and requeued-message redispatch (the framework-level
/// ReQueuedEtoHandler in Hhs.Shared.Hosting), so any exception — whether a pre-handler dispatch
/// failure or one that escapes the handler's own execution — is logged then rethrown uniformly,
/// letting the caller report it via FailedEto so the event is never silently lost.
/// </summary>
public sealed class EventDispatcher(
    IServiceScopeFactory serviceScopeFactory,
    IEventBusSubscriptionManager subscriptionsManager,
    IEventBusLogger logger
) : IEventDispatcher
{
    public async Task DispatchAsync(string trimmedEventName, Type eventType, object messageEnvelope)
    {
        var subscriptions = subscriptionsManager.GetHandlersForEvent(trimmedEventName);

        // AbcEvent => AbcEventLogHandler, AbcEventMailHandler etc. Multiple subscriptions can exist for one event
        foreach (var subscription in subscriptions)
        {
            using var scope = serviceScopeFactory.CreateScope(); // because handler type scoped service
            object handler = scope.ServiceProvider.GetService(subscription.HandlerType);
            if (handler == null)
            {
                // Framework-level dispatch failure — happens BEFORE any business logic runs — must propagate.
                throw new InvalidOperationException($"No handler resolved for {subscription.HandlerType.FullName} (event: {trimmedEventName}).");
            }

            var watch = Stopwatch.StartNew();
            var handleStartTime = DateTimeOffset.UtcNow;

            try
            {
                var dataFilter = scope.ServiceProvider.GetService<IDataFilter>();
                using (dataFilter.Disable<IMultiTenant>()) // disable tenant filter
                {
                    using (dataFilter.Disable<IScopeSubscription>()) // disable scope key filter
                    {
                        var eventHandlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                        await Task.Yield();

                        var method = eventHandlerType.GetMethod(nameof(IIntegrationEventHandler<IIntegrationEventMessage>.HandleAsync));
                        await ((Task)method!.Invoke(handler, [messageEnvelope]))!;
                    }
                }

                watch.Stop();
                logger.EventBusInfoLog(new ConsumeMessageLogModel(
                    LogId: Guid.CreateVersion7().ToString(),
                    CorrelationId: ((dynamic)messageEnvelope)?.CorrelationId,
                    Facility: nameof(EventBusLogFacility.CONSUME_EVENT_SUCCESS),
                    Producer: ((dynamic)messageEnvelope)?.Producer,
                    ConsumeDateTimeUtc: handleStartTime,
                    MessageLog: new MessageLogDetail(
                        EventType: trimmedEventName,
                        HopLevel: ((dynamic)messageEnvelope)?.HopLevel,
                        ParentMessageId: ((dynamic)messageEnvelope)?.ParentMessageId,
                        MessageId: ((dynamic)messageEnvelope)?.MessageId,
                        MessageTime: ((dynamic)messageEnvelope)?.MessageTime,
                        Message: ((dynamic)messageEnvelope)?.Message,
                        UserId: ((dynamic)messageEnvelope)?.UserId,
                        UserRoles: ((dynamic)messageEnvelope)?.UserRoles,
                        ClientLat: ((dynamic)messageEnvelope)?.ClientLat,
                        ClientLong: ((dynamic)messageEnvelope)?.ClientLong,
                        ClientChannel: ((dynamic)messageEnvelope)?.ClientChannel,
                        ClientVersion: ((dynamic)messageEnvelope)?.ClientVersion
                    ),
                    ConsumeDetails: "Message handling successfully completed",
                    ConsumeHandleWorkingTimeMs: watch.ElapsedMilliseconds
                ));
            }
            catch (Exception ex)
            {
                watch.Stop();
                logger.EventBusErrorLog(new ConsumeMessageLogModel(
                    LogId: Guid.CreateVersion7().ToString(),
                    CorrelationId: ((dynamic)messageEnvelope)?.CorrelationId,
                    Facility: nameof(EventBusLogFacility.CONSUME_EVENT_ERROR),
                    Producer: ((dynamic)messageEnvelope)?.Producer,
                    ConsumeDateTimeUtc: handleStartTime,
                    MessageLog: new MessageLogDetail(
                        EventType: trimmedEventName,
                        HopLevel: ((dynamic)messageEnvelope)?.HopLevel,
                        ParentMessageId: ((dynamic)messageEnvelope)?.ParentMessageId,
                        MessageId: ((dynamic)messageEnvelope)?.MessageId,
                        MessageTime: ((dynamic)messageEnvelope)?.MessageTime,
                        Message: ((dynamic)messageEnvelope)?.Message,
                        UserId: ((dynamic)messageEnvelope)?.UserId,
                        UserRoles: ((dynamic)messageEnvelope)?.UserRoles,
                        ClientLat: ((dynamic)messageEnvelope)?.ClientLat,
                        ClientLong: ((dynamic)messageEnvelope)?.ClientLong,
                        ClientChannel: ((dynamic)messageEnvelope)?.ClientChannel,
                        ClientVersion: ((dynamic)messageEnvelope)?.ClientVersion
                    ),
                    ConsumeDetails: $"Handle Error: {ex.Message}",
                    ConsumeHandleWorkingTimeMs: watch.ElapsedMilliseconds
                ));

                // Any exception that escapes the microservice's own internal handling (e.g. reaches
                // ApplicationEventHandlerBase's catch) is an unhandled failure, not an intentional
                // retry/swallow decision — those never throw in the first place. Propagate so the
                // caller (RabbitMqConsumer) can report it via FailedEto; the event must not be lost.
                throw;
            }
        }
    }
}
