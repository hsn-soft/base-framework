# ADR-0023: The event bus is a port of the production code: same wire, same topology, same failure handling

- **Status:** Accepted
- **Date:** 2026-10-23
- **Note:** the Kafka client retries the creation of topics while the broker does not answer, instead of stopping the host; [ADR-0062](0062-a-kafka-consumer-for-every-service-and-event.md): one consumer and group per service and event.

## Context

Services built on the new framework must talk to services still on the old one, so the exchange and queue names, the parameters they are declared with and the JSON on the wire cannot change. The failure handling and the shutdown of the old
code had bugs worth fixing without changing behaviour that others rely on.

## Decision

- **`EventBus` is the broker-independent core** (envelope, handler contract, dispatcher, naming, publish retry); a **broker package only moves bytes** (ADR-0003). RabbitMQ is a port: the exchange (`direct`, not durable), the queue
  `{ExchangeName}_{ConsumerClientName}_{EventName}` (durable, no arguments), the error queue `{ExchangeName}_{ErrorClientName}_{FailedEventName}`, the requeue queue, persistent `mandatory` messages and the flat PascalCase JSON envelope are as in production.
  The envelope's `UserId`, `UserRoles`, `ClientLat`, `ClientLong`, `ClientChannel`, `ClientVersion` are **headers**: the framework carries them from a message to the ones it causes and does not interpret them; the product fills them (`IEventHeaderProvider`).
- **A handler that publishes does not name its parent**: the message being handled is the parent (`EventContext.Current`), so `ParentMessageId`, `HopLevel`, the correlation id and the headers follow the chain. **One handler per event per service; the first registration wins.**
- **Failure handling:** a handler that throws publishes a `FailedEto` to the error client's queue and acknowledges the original; `TimeoutException` puts the message back after `RequeueDelay`; an unreadable message is reported with its raw
  content; a `FailedEto` that itself fails is logged Critical and dropped (reporting again would loop); a report that cannot be published puts the message back; publishing retries `PublishRetryCount` times with `2^n` seconds. A handler that succeeded is
  never reported failed because its acknowledgement failed (the broker redelivers). `MaxHandlerAttempts` (default 1) can retry a handler inside the framework.
- **Shutdown:** each consumer is cancelled on the broker first, then finishes what it is handling (up to `ShutdownTimeout`, 30 s); unfinished messages stay unacknowledged and are delivered again. The pod's `terminationGracePeriodSeconds` and `HostOptions.ShutdownTimeout` must
  cover the slowest handler.
- **Left to the product's shared project:** the handler base class (inbox, idempotency), `IEventHeaderProvider`, the generic `ReQueuedEtoHandler`.
- The logs of a message are ADR-0018; its failure logs are ADR-0020.

## Consequences

- Wire compatibility is tested by integration tests against a real RabbitMQ (ADR-0010) and must not be broken by a refactor.
- **Not done yet:** the Kafka project builds on the same core but still has its first, simple consumer; Kafka and an Azure Service Bus project will be ported the same way (same envelope, naming, failure handling and shutdown, the broker's own topology), and the
  Kafka client's logs bridged to `ILogger` as the RabbitMQ client's are.

## Related

[ADR-0003](0003-abstractions-separate-from-providers.md), [ADR-0018](0018-chain-logs.md), [ADR-0020](0020-failure-logged-once.md), `docs/EventBus.md`.
