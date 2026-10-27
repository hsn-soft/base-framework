# ADR-0066: Kafka handles `fetchCount` messages at the same time

- **Status:** Accepted
- **Date:** 2026-10-26 (changes the one-message-at-a-time reading of [ADR-0062](0062-a-kafka-consumer-for-every-service-and-event.md); the two brokers now behave alike)
- **Note:** [ADR-0077](0077-only-the-abc-sample-stays.md): the Xyz sample, which showed this, was removed; the Kafka consumer is covered by its tests.

## Context

`fetchCount` (`AddEventHandler(fetchCount)`, `AddEventHandlers(assembly, fetchCounts)`) says how many messages of an event a service handles at the same time, one when it is not named. RabbitMQ did it (prefetch, a semaphore, the dispatch concurrency of the client); Kafka ignored it: the consumer of an event handled one message, committed it
and read the next. A topic with a thousand messages was worked through at the speed of one handler, and a service could not choose, as it can on RabbitMQ.

## Decision

- **The consumer of an event (`KafkaEventConsumer`) handles up to `fetchCount` messages at the same time**, each given to the dispatcher on its own task and so in a scope of its own, as before. It reads another message only when a place is free; with 1 (the default) it is the old one-by-one reading.
- **An offset is committed only when every offset before it, in its partition, is finished** (`OffsetTracker`), as Kafka remembers one offset per partition: a message that is still running (or failed to leave) holds the commit back and is read again after a restart; the ones that finished after it are then handled again too, so a handler must stand
  a redelivery as on any broker. Only the thread that reads touches the consumer: the handlers say they are done, the reading thread commits.
- **Rebalances:** when partitions are taken away the consumer lets the messages in flight finish (up to `ShutdownTimeout`), commits, and only then gives the partitions up; when they are lost nothing is committed for them.
- **A message that cannot even be moved to the dead-letter topic** is not committed and fails the consumer, which is built again ([ADR-0065](0065-the-consumers-outlive-their-own-failures.md)).
- The order of messages is **not kept** between the ones handled at the same time, even of one key: as on RabbitMQ, a handler that needs an order of its own keeps it itself.

## Consequences

- The two brokers have the same `fetchCount`, the same graceful stop and the same failure handling, and the samples use it the same way (Abc and Xyz: the product service handles ten `OrderCreated` at the same time, the order service five `OrderStockReserved`).
- A consumer that handles long messages must still poll within `max.poll.interval.ms` (5 minutes): it polls while handlers run, but not while every place is taken by a handler that is stuck for longer than that.
- Tests (a real Kafka): the number at the same time is exactly `fetchCount`; one without; what was handled together is committed and not read again; a message cancelled at shutdown is read again; a second instance that joins takes partitions over without a message handled twice or lost. `OffsetTracker` is covered by unit tests.

## Related

[ADR-0062](0062-a-kafka-consumer-for-every-service-and-event.md), [ADR-0064](0064-the-stock-is-changed-in-one-step.md), [ADR-0065](0065-the-consumers-outlive-their-own-failures.md), [ADR-0023](0023-event-bus.md).
