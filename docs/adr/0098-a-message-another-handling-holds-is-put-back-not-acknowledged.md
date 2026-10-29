# ADR-0098: A message another handling holds is put back, never acknowledged

- **Status:** Accepted
- **Date:** 2026-10-28 (changes the `InProgress` answer of the inbox of [ADR-0067](0067-the-event-inbox-is-opt-in-and-a-decorator.md))

## Context

When a handler is cut short (the pod is killed, the shutdown timeout passes) its inbox row stays `Started`, and the broker brings the message back within seconds, but the lease (`ProcessingLease`, 2 minutes) has not run out, so the inbox answers `InProgress`. The dispatcher then returned
normally, so RabbitMQ acknowledged and Kafka committed the delivery: **the message was never handled**. A rolling deploy in a busy system would lose the messages that were in flight.

## Decision

- **`InProgress` throws `MessageInProgressException`** (in `EventBus`, namespace `Inbox`) from the dispatcher, before any handler runs and without touching the row. A delivery that is not acknowledged is the only safe answer: either the other handling ends (the next delivery is `AlreadyHandled`), or its lease runs out (the next
  delivery takes the message).
- **RabbitMQ** puts the message back after its `RequeueDelay`, as it does for a timeout (no report to the error client, no acknowledge). **Kafka** keeps the message in its place, does not complete the offset, and asks the dispatcher again after `EventBusOptions.InProgressRetryDelay` (5 s) until it can
  be handled or the service stops; the message is not dead-lettered.
- `RetryLimitReached` is still left alone (acknowledged): the error queue was told when the message failed for the last time; the log now says so instead of saying it is left to the error queue.

## Consequences

- No message is lost to a crashed handling; the cost is that a message waits, at most one lease, behind a handling that really crashed, and holds its slot (RabbitMQ prefetch, Kafka `FetchCount`) meanwhile.
- A handling that is really still running on another instance makes the delivery wait until it ends; the lease must be longer than the slowest handler (ADR-0067).
- Tests: the dispatcher throws and touches nothing; the RabbitMQ consumer nacks with requeue and reports nothing; a Kafka service asks again and handles the message (integration, a real broker).

## Related

[ADR-0067](0067-the-event-inbox-is-opt-in-and-a-decorator.md), [ADR-0065](0065-the-consumers-outlive-their-own-failures.md).
