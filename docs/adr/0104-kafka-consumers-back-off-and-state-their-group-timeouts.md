# ADR-0104: Kafka consumers back off when they fail and state their group timeouts

- **Status:** Accepted
- **Date:** 2026-10-29 (completes [ADR-0065](0065-the-consumers-outlive-their-own-failures.md) and [ADR-0066](0066-kafka-handles-fetchcount-messages-at-the-same-time.md))

## Context

Four small things of the Kafka consumer were left to chance: a consumer that failed started again after a fixed 5 s, so a broker that was down was asked every five seconds by every consumer of every pod; `session.timeout.ms` and `max.poll.interval.ms` were the library's defaults, though a consumer whose places are
all taken by slow handlers does not read, and one that waits for its handlers while partitions move does not read either (the broker then takes its partitions away and another instance handles the same messages); the creation of the topics could not be interrupted, so a stopping service
(or a publisher that gave up) waited up to twenty seconds for it.

## Decision

- **Back-off:** the wait before a failed consumer starts again doubles with every failure in a row, from `Kafka:RecoveryDelay` (5 s) up to `Kafka:RecoveryMaxDelay` (60 s); a run that lasted longer than the maximum was healthy and the next failure starts from the first wait again. The log says the wait it uses.
- **Group timeouts are settings:** `Kafka:SessionTimeout` (45 s) and `Kafka:MaxPollInterval` (5 min) are put on every consumer. **`MaxPollInterval` must be longer than `EventBusOptions.ShutdownTimeout`**, or the subscriber throws at start (the host stops, as with any failed background service): a consumer
  waits the shutdown timeout for what it handles when partitions move. It must also be longer than the slowest handler; that cannot be checked, so the comment of the setting says it.
- **Creating the topics takes the token** of the caller: a stopping service and a publisher whose token is cancelled stop waiting between the tries.

## Consequences

- A broker that is down is asked less and less often (about once a minute per consumer), not every five seconds.
- A service with handlers slower than five minutes has to raise `MaxPollInterval`; before this change the same limit was there, unwritten.
- Not done: reading while every place is taken (pausing the partitions and going on polling), which would remove the dependence of `MaxPollInterval` on the slowest handler; it is a larger change of the read loop.
- Tests: the doubling, its maximum and its reset (unit); the topic creation that stops with its token (unit); the check at start, and every existing Kafka behavior with the settings on the consumers (a real broker).

## Related

[ADR-0065](0065-the-consumers-outlive-their-own-failures.md), [ADR-0066](0066-kafka-handles-fetchcount-messages-at-the-same-time.md), [ADR-0103](0103-brokers-are-reached-with-tls-and-sasl-when-the-settings-say-so.md).
