# ADR-0099: RabbitMQ publishes with confirms and a timeout; what is worth repeating is one list

- **Status:** Accepted
- **Date:** 2026-10-28 (changes the publishing of [ADR-0023](0023-event-bus.md))

## Context

A publish was fire-and-forget: the channel had no confirms, so a message the broker dropped, or one sent just before the connection broke, was logged as *Published* and lost. `mandatory: true` was set but a returned message was never looked at. Nothing bounded the wait of a publisher
(a broker that stops answering held the caller for the continuation timeout, two minutes), and only an unreachable broker or a socket error was repeated: a channel that closed during recovery, or "no connection" while the client reconnects, failed the publish at once. The failed-event notifier, which
reports a failed message and then lets the original be acknowledged, had the same weakness, so a failure could be lost together with its report.

## Decision

- **Publishing uses `IRabbitMqPersistentConnection.CreatePublisherChannelAsync`**: a channel with publisher confirmations and tracking, so `BasicPublishAsync` returns when the broker has confirmed the message and throws when it nacks (`PublishException`). The consumers' channels are unchanged.
- **A confirm has `EventBusOptions.PublishTimeout` (30 s)**; running out of it is a `TimeoutException` (not the cancellation of the caller, which still is a cancellation), and it is repeated like any transient failure. The message may have reached the queue, so a repeated publish can deliver twice (at least once, as the inbox of [ADR-0067](0067-the-event-inbox-is-opt-in-and-a-decorator.md) expects).
- **One list of what is worth repeating**, `RabbitMqPublisher.IsTransient`, used by the event bus and the failed-event notifier: an unreachable broker, a socket or I/O error, a closed channel or connection, a nack or return (`PublishException`), `RabbitMqNotConnectedException` (no open connection: new, a subclass of `InvalidOperationException`), a timeout.
- **An event nobody listens to is not an error**: published to the exchange with no queue bound, the broker returns it; the publisher logs a Warning (`RABBITMQ_BUS_UNROUTABLE`) and goes on, because a publisher may start before any subscriber and an event has no promise of a reader. A message *sent* to a service goes to its declared queue and a return there is repeated as any other failure.

## Consequences

- A message the broker did not accept is no longer logged as published; a publish costs one round trip more (the confirm). A channel is still opened for every publish: a pool of publisher channels is a separate change.
- A publisher that was never given a broker answer fails after `PublishRetryCount` tries of `PublishTimeout` each; callers should expect `TimeoutException`.
- Tests: confirm channel for events and sends, return not an error, nack repeated, timeout repeated and thrown, caller cancellation not a timeout (unit); against a real broker, an event nobody listens to and then one that is delivered.

## Related

[ADR-0023](0023-event-bus.md), [ADR-0067](0067-the-event-inbox-is-opt-in-and-a-decorator.md), [ADR-0098](0098-a-message-another-handling-holds-is-put-back-not-acknowledged.md).
