# Event bus

`HsnSoft.Base.EventBus` is the broker-independent half: the message envelope, the handler contract, the dispatcher, naming, the
publish retry. A broker project (`HsnSoft.Base.EventBus.RabbitMQ`) only moves the bytes. The RabbitMQ project is a port of the code that
runs in production: **the exchange and queue names, the parameters they are declared with and the JSON on the wire are the same**, so a
service built on this framework and a service still on the previous one can talk to each other.

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

await eventBus.PublishAsync(new OrderPlacedEto(id), correlationId: "...");
```

A handler that publishes another event does not have to say which message caused it: the message being handled is the parent
automatically (`EventContext.Current`), so `ParentMessageId`, `HopLevel`, the correlation id and the headers follow the chain.
Pass `parentMessage:` to choose a different parent.

## The envelope and the wire format

One flat JSON object, property names as the C# properties (PascalCase); reading ignores case.

```json
{ "HopLevel": 2, "ReQueuedCount": 0, "MessageId": "…", "MessageTime": "…Z", "CorrelationId": "…", "Producer": "OrderService",
  "ParentMessageId": "…", "UserId": "…", "ClientChannel": "web", "Message": { "OrderId": "…" } }
```

`UserId`, `UserRoles`, `ClientLat`, `ClientLong`, `ClientChannel`, `ClientVersion` and anything else a product puts next to the fixed
properties are **headers** (`envelope.GetHeader("UserId")`, `SetHeader`). The framework carries them from a message to the ones it
causes and does not interpret them; the product names them and fills them through `IEventHeaderProvider`.

## What is on the RabbitMQ broker

| What | Name | Declared as |
|---|---|---|
| Exchange | `{ExchangeName}` | `direct`, not durable (as in production) |
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
| The service stops | Each consumer is cancelled on the broker first (no new messages), then finishes the messages it is handling (up to `EventBusOptions.ShutdownTimeout`, 30 s) before the channel is closed; what is not finished stays unacknowledged and is delivered again. In Kubernetes the pod's `terminationGracePeriodSeconds` and the host's `HostOptions.ShutdownTimeout` (30 s by default) must be at least as long as the slowest handler. |

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
| `PublishAsync(eventMessage, parentMessage, correlationId, isExchangeEvent, isReQueuePublish)` | the same parameters (same names), plus a trailing `cancellationToken` |
| `MessageEnvelope.UserId`, `UserRoles`, `Client*` | headers: `GetHeader("UserId")`; filled by an `IEventHeaderProvider` instead of `ICurrentUser` / `ITraceAccesor` |
| `IEventBusLogger` with persisted log models | `ILogger`: `PRODUCE_EVENT_*` / `CONSUME_EVENT_*` event names, and `CorrelationId`, `MessageId`, `EventName` in the log scope |
| `RabbitMqConnectionSettings`, `RabbitMqEventBusConfig : EventBusConfig` | `RabbitMqConnectionSettings` (same section), `EventBusOptions` (same section and property names) and `RabbitMqEventBusConfig` for the RabbitMQ tuning |
| `IEventBusSubscriptionManager.AddSubscription` / `Clear` | registration is done through the container |
| `IRabbitMqPersistentConnection.GetRabbitMqConnectionCountAsync` | removed (it called the management API on the AMQP port) |
| `IEventDispatcher.DispatchAsync(name, type, envelope)` | the same, plus an optional `CancellationToken` |

Behaviour that changed on purpose: a handler that succeeds is never reported as failed because its acknowledgement failed (the broker
delivers it again instead); `PublishAsync` can be cancelled; the consumers do not block the host's startup thread; the number of
messages being published is counted correctly when several are published at once.

## Not done yet

The Kafka project builds on the same core but still has its first, simple consumer. It and an Azure Service Bus project will be ported
the same way: the same envelope, naming, failure handling and shutdown, with the broker's own topology.
