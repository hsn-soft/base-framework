# Event bus

`HsnSoft.Base.EventBus` is the broker-independent half: the message envelope, the handler contract, the dispatcher, naming, the
publish retry. A broker project (`HsnSoft.Base.EventBus.RabbitMQ`) only moves the bytes. The design started from the event bus of the previous framework,
but it owes it nothing: names, the envelope and the behavior change when a better structure calls for it ([ADR-0057](adr/0057-the-caller-of-a-message-and-publish-or-send.md)).

## Using it

```csharp
// the product's Shared project decides what is registered, and in which order
services
    .AddRabbitMqEventBus(configuration)                         // sections RabbitMq:Connection and RabbitMq:EventBus
    .AddEventHandlers(typeof(Program).Assembly, fetchCounts)    // or AddEventHandler<OrderPlacedEto, OrderPlacedHandler>(fetchCount: 4)
    .AddEventHeaderProvider<HttpContextHeaders>();              // what the product adds to every message (user, client, correlation)
```

```json
"RabbitMq": {
  "Connection": { "HostName": "rabbit", "Port": 5672, "UserName": "guest", "Password": "guest", "ConnectionRetryCount": 5 },
  "EventBus":   { "ExchangeName": "HHS", "ConsumerClientName": "OrderService", "ErrorClientName": "EventManagerService",
                  "EventNamePrefix": "", "EventNameSuffix": "Eto" }
}
```

```csharp
public sealed record OrderPlacedEto(Guid OrderId) : IIntegrationEventMessage;

public sealed class OrderPlacedHandler : IIntegrationEventHandler<OrderPlacedEto>
{
    public Task HandleAsync(MessageEnvelope<OrderPlacedEto> envelope, CancellationToken cancellationToken) { ... }
}

await eventBus.PublishAsync(new OrderPlacedEto(id));                     // an event: to every service that listens, the publisher does not know who
await eventBus.SendAsync(new ShipOrderEto(id), toService: "shipment-service");  // a command: straight to the queue of one service, nobody else gets it
```

`PublishAsync` is what almost everything is: something happened, whoever cares subscribes. `SendAsync` is for the message that asks one service to do something
(and for putting a failed message back to the consumer that failed); the queue is made if it is not there yet. Kafka has no such place for a message and refuses it.

**Who is calling travels in the envelope, not in the message.** The tenant, the user and the roles of whoever started the chain are put in the envelope when a message is
published (from `ICurrentUser`, or from the message being handled), and the dispatcher runs the handler **as that user** (`ActingUser`): `ICurrentUser` and `ICurrentTenant` answer
in the handler as in a controller, so the query filters and the audit fields (`CreatedBy`, `UpdatedBy`) work without the handler doing anything. A message needs no `TenantId` of its own.

Inbox (a message is handled once) and outbox (a message is published with the change that caused it) are **optional**: the event bus knows neither; a service that needs them
adds them ([ADR-0057](adr/0057-the-caller-of-a-message-and-publish-or-send.md)).

A handler that publishes another event does not have to say which message caused it: the message being handled is the parent
automatically (`EventContext.Current`), so `ParentMessageId`, `HopLevel`, the correlation id and the headers follow the chain.
Pass `parentMessage:` to choose a different parent.

## The envelope and the wire format

One flat JSON object, property names as the C# properties (PascalCase); reading ignores case.

```json
{ "HopLevel": 2, "ReQueuedCount": 0, "MessageId": "…", "MessageTime": "…Z", "CorrelationId": "…", "Producer": "order-service",
  "TenantId": "…", "IsSystemTenant": false, "UserId": "…", "UserName": "alice", "UserRoles": ["customer"],
  "ParentMessageId": "…", "ClientChannel": "mobile", "Message": { "OrderId": "…" } }
```

`TenantId`, `IsSystemTenant`, `UserId`, `UserName` and `UserRoles` are properties of the envelope (the caller of the chain). `ClientChannel`, `ClientVersion`, `ClientLat`, `ClientLong` and anything else
a product puts next to them are **headers** (`envelope.GetHeader("ClientChannel")`, `SetHeader`): the framework carries them from a message to the ones it causes and does not interpret them;
the product names them and fills them through `IEventHeaderProvider`.

## What is on the RabbitMQ broker

| What | Name | Declared as |
|---|---|---|
| Exchange | `{ExchangeName}` | `direct`, not durable |
| Queue of an event | `{ExchangeName}_{ConsumerClientName}_{EventName}` | durable, no arguments; bound with the event name as routing key |
| Error queue | `{ExchangeName}_{ErrorClientName}_{FailedEventName}` (e.g. `HHS_EventManagerService_Failed`) | durable, no arguments |
| Requeue queue | `{ExchangeName}_{ConsumerClientName}_{ReQueuedEventName}` | the consumer's own queue of `ReQueuedEto` |

The event name is the type name without `EventNamePrefix` and `EventNameSuffix` (`OrderPlacedEto` is `OrderPlaced`). Messages are
persistent, `mandatory`.

## When something goes wrong

| Case | What happens |
|---|---|
| The handler throws | A `FailedEto` (reason, the failed message and its type) is published to the error queue of the error client, the original is acknowledged. The error client can put it back with a `ReQueuedEto`. |
| The handler throws `TimeoutException` | The message goes back on the queue after `RequeueDelay` (5 s); nothing is reported. |
| The message cannot be read (invalid JSON, unknown event) | Reported like a failed handler, with the raw content; an event nobody subscribed to is acknowledged and logged. |
| A `FailedEto` itself fails | Logged as critical and dropped (reporting it again would loop). |
| The report cannot be published | The message goes back on the queue after `RequeueDelay`. |
| The broker is not reachable while publishing | Retried `PublishRetryCount` times (default 5) with `2^n` seconds between tries. |
| The connection is cut | The client reconnects and recovers channels and consumers; a consumer whose channel dies alone rebuilds it. |
| A step of the framework fails before the handler (RabbitMQ) | The message is reported like a failed handler (error client's queue) and taken off the queue; the consumer goes on with the next one. |
| The consumer fails on its own (Kafka: the broker cannot be read, an offset cannot be committed) | It is logged (`KAFKA_SUBSCRIBER_CONSUMER_FAILED`), the consumer of that event is built again after `Kafka:RecoveryDelay` (5 s); the other events and the host are not touched, and the message that was not committed is read again. |
| The service stops | Each consumer is cancelled on the broker first (no new messages), then finishes the messages it is handling (up to `EventBusOptions.ShutdownTimeout`, 30 s) before the channel is closed; what is not finished stays unacknowledged and is delivered again. On Kafka the consumer of each event reads no new message once the service stops and lets the messages it is handling finish (up to the same `ShutdownTimeout`; its handler is cancelled after it, and the message, not committed, is handled again later). With `fetchCount` 10 on RabbitMQ all ten are waited for. In Kubernetes the times add up, they do not nest: a stopping pod spends its `preStop` (the time the endpoints need to forget it), then the consumers drain (up to `EventBusOptions.ShutdownTimeout`, in parallel for all events; the host stops the other hosted services in the same phase, bounded by `HostOptions.ShutdownTimeout`), then the bus waits for the messages it is publishing (up to `ShutdownTimeout` again, when the container is disposed). `terminationGracePeriodSeconds` has to cover all of it: with a `preStop` of 10 s and both timeouts at 30 s, 90 s (the default of 30 s kills the pod in the middle; so does `stop_grace_period` of a compose file). |

`EventBusOptions.LogMessageBody` (default `false`) decides whether the whole message body is written to the logs: the Debug log of a
received message and the Critical log of a failed event that is dropped. A message may carry personal data and the console is a log
too, so the body is off unless a service turns it on to debug.

The `ConsumeStart` log carries the message the handling started with as `MessageBody`: the envelope (`messageId`, `correlationId`, `headers`, ...) and the event
together, as JSON, with what is sensitive hidden first (`[SensitiveData]` properties of the event and the usual keys, as in the request logs). It is on
by default (`EventBusOptions.LogConsumedMessage`), is cut at `MaxLoggedMessageLength` (8192), and is left out when no `ILogMasker` is registered, so nothing
unmasked is written. `LogMessageBody` above is a different switch: the raw body, for debugging only.

`MaxHandlerAttempts` (default 1) can make the framework retry a handler before it counts as failed; the services in production leave
retrying to their handlers.

## Product side

The framework leaves these to the product's Shared project, because they know products:

- the handler base class (inbox, idempotency, the `IEventApplicationService` that passes the parent message);
- `IEventHeaderProvider`: where the correlation id, user and client details come from (an `HttpContext` accessor);
- the generic `ReQueuedEtoHandler` that hands a requeued message to the service's own handler (it needs `IEventDispatcher`,
  `IEventBusSubscriptionManager` and `IFailedEventNotifier`, all registered by `AddRabbitMqEventBus`);
- registering that handler: `AddEventHandler<ReQueuedEto, ReQueuedEtoHandler>()`. An event has **one handler per service, the first
  registration wins**, so a service's own handler for an event replaces a shared default.

## Moving from the previous framework

| Previous | Now |
|---|---|
| `eventBus.Subscribe<TEvent, THandler>(fetchCount)` at startup, `UseEventBus(assembly, fetchCounts)` | `AddEventHandler<TEvent, THandler>(fetchCount)` / `AddEventHandlers(assembly, fetchCounts)`; a hosted service starts the consumers and stops them |
| `HandleAsync(MessageEnvelope<T> @event)` | `HandleAsync(MessageEnvelope<T> envelope, CancellationToken)` |
| `PublishAsync(eventMessage, parentMessage, correlationId, isExchangeEvent, isReQueuePublish)` | `PublishAsync(eventMessage, parentMessage, correlationId, cancellationToken)` to every service; `SendAsync(eventMessage, toService, ..., isReQueuePublish, cancellationToken)` to one service (no `isExchangeEvent` flag) |
| `MessageEnvelope.UserId`, `UserRoles`, `Client*` | `TenantId`, `IsSystemTenant`, `UserId`, `UserName`, `UserRoles` are properties of the envelope, filled from `ICurrentUser`; `Client*` are headers (`GetHeader`), filled by an `IEventHeaderProvider` |
| `IEventBusLogger` with persisted log models | `ILogger`: `PRODUCE_EVENT_*` / `CONSUME_EVENT_*` event names, and `CorrelationId`, `MessageId`, `EventName` in the log scope |
| `RabbitMqConnectionSettings`, `RabbitMqEventBusConfig : EventBusConfig` | `RabbitMqConnectionSettings` (same section), `EventBusOptions` (same section and property names) and `RabbitMqEventBusConfig` for the RabbitMQ tuning |
| `IEventBusSubscriptionManager.AddSubscription` / `Clear` | registration is done through the container |
| `IRabbitMqPersistentConnection.GetRabbitMqConnectionCountAsync` | removed (it called the management API on the AMQP port) |
| `IEventDispatcher.DispatchAsync(name, type, envelope)` | the same, plus an optional `CancellationToken` |

Behaviour that changed on purpose: a handler that succeeds is never reported as failed because its acknowledgement failed (the broker
delivers it again instead); `PublishAsync` can be cancelled; the consumers do not block the host's startup thread; the number of
messages being published is counted correctly when several are published at once.

## Not done yet

The Kafka consumer and an Azure Service Bus project are still to be ported the same way ([ADR-0023](adr/0023-event-bus.md)).
