# ADR-0100: A connection the RabbitMQ client is recovering is never replaced from outside

- **Status:** Accepted
- **Date:** 2026-10-28 (completes the connection handling of [ADR-0023](0023-event-bus.md))

## Context

The connection is created with automatic recovery: when it is lost the client reconnects it (every 10 s, for ever) and brings back its channels and consumers. While it does so the connection is not open. `TryConnectAsync` took "not open" to mean "dead": a publisher that called it in that window closed and disposed the
connection the client was recovering and opened a new one, so the channels and consumers being recovered were orphaned and the consumers waited for the probe of the recovery loop to rebuild them.

## Decision

- **A connection that is not open and was not closed by the application (`CloseReason.Initiator` is not `Application`) is being recovered: `TryConnectAsync` leaves it alone and answers `false`** (log `RABBITMQ_CONNECTION_RECOVERING`, Debug). A connection the application closed, or that has no reason (never opened), is replaced as before.
- The callers already handle `false`: a publish throws `RabbitMqNotConnectedException`, which is transient ([ADR-0099](0099-rabbitmq-publishes-with-confirms-and-a-timeout.md)), so it is repeated with the publish backoff until the client has recovered; the consumers' recovery loop does nothing while the connection is down.

## Consequences

- One connection per process for the life of the process; a lost connection comes back with its channels and consumers by the client's own recovery, without a second connection.
- A broker that stays down keeps the connection in recovery: publishes fail with the transient exception after their retries, and the consumers resume when the broker is back; there is no point at which the framework gives up and opens another connection.
- Tests: a lost connection is not replaced and a connection the application closed is (unit); a publish during a recovery on a real broker, which ends with one connection (the race window is short, so this test guards the flow, the unit tests guard the rule).

## Related

[ADR-0023](0023-event-bus.md), [ADR-0099](0099-rabbitmq-publishes-with-confirms-and-a-timeout.md).
