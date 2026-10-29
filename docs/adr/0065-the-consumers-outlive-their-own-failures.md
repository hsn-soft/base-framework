# ADR-0065: The consumers outlive their own failures

- **Status:** Accepted
- **Date:** 2026-10-26 (completes the failure handling of [ADR-0023](0023-event-bus.md); applies to [ADR-0062](0062-a-kafka-consumer-for-every-service-and-event.md))

## Context

A failing handler never stopped a service: RabbitMQ reports the message to the error client's queue, Kafka copies it to the dead-letter topic. A failure of the framework's own code was not covered the same way. On Kafka an exception of `Consume` or `Commit` left the consumer loop,
the `BackgroundService` failed and the host stopped (the default of .NET), so one broker hiccup took the whole service and every event of it down. On RabbitMQ the steps before the handler (reading the message, its log scope) were outside the `try`: a failure there left the message unacknowledged.

## Decision

- **Kafka:** the consumer of an event runs under `KafkaRecovery.RunAsync`: when it fails on its own (any exception that is not the stop of the service) the failure is logged (`KAFKA_SUBSCRIBER_CONSUMER_FAILED`), the consumer waits `Kafka:RecoveryDelay` (default 5 s) and is built again,
  until the service stops. The message that was being dealt with was not committed and is read again. The other events and the host are not affected. Starting up still fails when the broker cannot be reached for the topics (retried ten times), as a host that cannot start should say so.
- **RabbitMQ:** the whole of the work of a delivery is under one `try`: a failure of a step before the handler is reported like a failed handler (`ReportFailureAsync`: the error client's queue, then the message is taken off the queue) and the consumer goes on.

## Consequences

- No failure of the event bus itself, on either broker, stops the host; each is logged as an Error with the group or the queue.
- A message that makes the framework itself fail before its handler is moved to the error queue, not retried: it could loop forever. The reason is in the failed message.
- On Kafka a failure that repeats (the broker is down) repeats the Error log every `RecoveryDelay`: that is the signal to alert on.
- Tests: the recovery loop (unit), and a RabbitMQ consumer whose log scope fails once before the handler (integration, a real broker): the message reaches the error queue and the next one is handled.

## Related

[ADR-0023](0023-event-bus.md), [ADR-0062](0062-a-kafka-consumer-for-every-service-and-event.md).
