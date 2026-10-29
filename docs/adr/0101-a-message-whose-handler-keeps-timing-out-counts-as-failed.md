# ADR-0101: A RabbitMQ message whose handler keeps timing out counts as failed after a limit

- **Status:** Accepted
- **Date:** 2026-10-28 (bounds the requeue of [ADR-0065](0065-the-consumers-outlive-their-own-failures.md))

## Context

A handler that throws `TimeoutException` makes the consumer put the message back on the queue after `RequeueDelay` (5 s). A message that always times out came back for ever: it held a place of the consumer (`FetchCount`) during every delay, wrote a Warning every 5 s and was never reported.

## Decision

- **`RabbitMqEventBusConfig.MaxTimeoutRequeues` (10, `0` = no limit):** the consumer counts the timeouts of a message by its `MessageId`; the first ten put it back, the next one counts as failed (`RABBITMQ_CONSUMER_TIMEOUT_LIMIT`, Error): it is reported to the error client and taken off the queue like any failed message (`ReportFailureAsync`).
- The count is in the memory of the consumer (at most 10 000 messages; when it is full it is emptied) and is forgotten when the message is handled. A message that comes back to another instance, or after a restart, starts again: a limit per instance is enough to stop the loop, a shared count would need a store.
- A message without a `MessageId` (not published by the framework) is not counted and keeps the old behaviour.
- **Not bounded on purpose:** a message put back because the error client could not be told (the broker is down for the notifier) must not be lost, so it waits for the broker; and `MessageInProgressException` ([ADR-0098](0098-a-message-another-handling-holds-is-put-back-not-acknowledged.md)) is bounded by the lease of the inbox.

## Consequences

- A poison message that times out leaves the queue after about a minute (ten tries of 5 s) and is visible in the error queue; it no longer holds a place for ever.
- A handler that is slow only for a while keeps its message for ten tries; raise the limit for a service whose dependency is down for longer.
- Tests: the limit, the count per message and its reset when handled, a message without an id (unit).

## Related

[ADR-0065](0065-the-consumers-outlive-their-own-failures.md), [ADR-0098](0098-a-message-another-handling-holds-is-put-back-not-acknowledged.md).
