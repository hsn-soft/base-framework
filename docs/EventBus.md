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

**Who is calling travels in the envelope, not in the message.** The user and the roles of whoever started the chain are put in the envelope when a message is
published (from `ICurrentUser`, or from the message being handled), and the dispatcher runs the handler **as that user** (`ActingUser`): `ICurrentUser` answers
in the handler as in a controller, so the audit fields (`CreatedBy`, `UpdatedBy`) work without the handler doing anything. **The tenant travels as two headers** (`TenantId`, `IsSystemTenant`) that `HsnSoft.Base.MultiTenancy.EventBus` (`AddBaseTenantEventBus()`)
writes and whose `IEventHandlerScope` runs the handler in the tenant of the message, so the tenant filters work in the handler too ([ADR-0074](adr/0074-multitenancy-is-a-package-of-its-own.md)); a service without tenants does not have the package and still passes the headers on to the messages it causes.
A message needs no `TenantId` of its own. `IEventHandlerScope` is the hook for anything else a package must open around a handler (a scoped service, begun before the handler and disposed after, each attempt).

Inbox (a message is handled once) and outbox (a message is published with the change that caused it) are **optional**: the event bus knows neither; a service that needs them
adds them ([ADR-0057](adr/0057-the-caller-of-a-message-and-publish-or-send.md)).

A handler that publishes another event does not have to say which message caused it: the message being handled is the parent
automatically (`EventContext.Current`), so `ParentMessageId`, `HopLevel`, the correlation id and the headers follow the chain.
Pass `parentMessage:` to choose a different parent.

## The envelope and the wire format

One flat JSON object, property names as the C# properties (PascalCase); reading ignores case.

```json
{ "HopLevel": 2, "ReQueuedCount": 0, "MessageId": "…", "MessageTime": "…Z", "CorrelationId": "…", "Producer": "order-service",
  "UserId": "…", "UserName": "alice", "UserRoles": ["customer"], "TenantId": "…",
  "ParentMessageId": "…", "ClientChannel": "mobile", "Message": { "OrderId": "…" } }
```

`UserId`, `UserName` and `UserRoles` are properties of the envelope (the caller of the chain). `TenantId` (and `IsSystemTenant`, when it is `true`), `ClientChannel`, `ClientVersion`, `ClientLat`, `ClientLong` and anything else
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

## The inbox: a message that comes twice is handled once

A broker delivers at least once, so a message can come twice (a crash after the handler, before the acknowledgement; a message put back from the error queue). A service that must not handle it twice opts in to the **inbox**: it is a record, in the service's own database, of the messages
it received, keyed by the id of the message. A service that does not call `AddEventInbox` has no inbox and pays nothing for it ([ADR-0067](adr/0067-the-event-inbox-is-opt-in-and-a-decorator.md)).

```csharp
// the model of the context: modelBuilder.ConfigureEventInbox(prefix + "InboxMessage", schema);
builder.Services
    .AddEventHandlers(typeof(EventHandlersAssemblyMarker).Assembly)
    .AddEventInbox<OrderDbContext>();      // EventBus.EntityFrameworkCore; options: MaxRetryCount (4), ProcessingLease (2 minutes); on MongoDB: .AddEventInbox("InboxMessage")
```

The dispatcher asks the inbox before the handler and tells it how the handling ended. The handler is not changed and knows nothing of the inbox.

| The inbox says | What happens |
|---|---|
| `Started` (a new message, or one taken again) | the handler runs; handled (or the application said no to it): `Completed`; failed after its attempts: `Failed` with the reason, the error goes on to the error queue |
| `AlreadyHandled` | the handler does not run, the message is acknowledged (`CONSUME_EVENT_INBOX_ALREADY_HANDLED`, Information) |
| `InProgress` | another handling holds the message and its lease (`ProcessingLease`) has not run out: **not acknowledged**; RabbitMQ puts it back after `RequeueDelay`, Kafka asks again after `InProgressRetryDelay`, until the other handling ends or its lease runs out ([ADR-0098](adr/0098-a-message-another-handling-holds-is-put-back-not-acknowledged.md)) |
| `RetryLimitReached` | the message failed more than `MaxRetryCount` times: left to the error queue (Warning) |
| the inbox cannot be asked (the database is down) | the delivery fails and is reported like a failed handler: **the message is not dropped** |

A message is taken by inserting its row (the id is the key, so of two deliveries at once one insert wins); a row that failed, or whose handling died (a `Started` row older than the lease), is taken again by one `UPDATE ... WHERE` (the status and the lease are in the
condition), so two deliveries cannot both take it. What the inbox does **not** do: the row and the changes of the handler are two transactions, so a handler that crashes between its own commit and `Completed` is run again when the message comes back (after the lease): a handler
must still stand it. Rows are kept until the service asks for the cleanup (below). On EF Core (SQL Server, PostgreSQL: `EventBus.EntityFrameworkCore`, `AddEventInbox<TContext>()`) and on MongoDB (`EventBus.MongoDB`, `AddEventInbox(collectionName)`).

### Forgetting old messages

The table (or collection) only grows unless the service asks for the cleanup, which removes the **`Completed`** rows older than `RetentionPeriod` ([ADR-0102](adr/0102-the-inbox-cleanup-is-opt-in-and-needs-no-coordination-between-instances.md)). A started or failed row is never removed.

```csharp
builder.Services
    .AddEventInbox<OrderDbContext>()
    .AddEventInboxCleanup<OrderDbContext>(configuration);   // on MongoDB: .AddEventInboxCleanup("InboxMessage", configuration: configuration)
```

```json
"EventInbox": { "Cleanup": { "RetentionPeriod": "7.00:00:00", "Interval": "01:00:00", "BatchSize": 1000 } }
```

- **Relational databases:** a worker, run by every instance of the service, removes the rows a batch at a time; instances need no coordination (two that pick the same rows delete each once). `ConfigureEventInbox` adds an index on `Status, ProcessedAt`, so the next migration of the service creates it.
- **MongoDB:** a TTL index on `ProcessedAt` that only `Completed` documents are in, made at start (changed in place when the retention changes); the server removes the documents, nothing runs in the service. `Interval` is not used.
- `RetentionPeriod` of `00:00:00` turns it off. **Keep it longer than the broker keeps messages** (Kafka: 7 days by default): a copy that comes after its row was removed is handled again.
- A service that does not call `AddEventInboxCleanup` has no worker; one that cleans in the database itself (a job of the database) does not need it.

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
| `MessageEnvelope.UserId`, `UserRoles`, `Client*` | `UserId`, `UserName`, `UserRoles` are properties of the envelope, filled from `ICurrentUser`; `TenantId`, `IsSystemTenant` and `Client*` are headers (`GetHeader`), filled by an `IEventHeaderProvider` (the tenant by `MultiTenancy.EventBus`) |
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
