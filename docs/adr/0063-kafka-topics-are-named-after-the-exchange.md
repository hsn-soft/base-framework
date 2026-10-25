# ADR-0063: Kafka topics are named after the exchange; the topic prefix setting is gone

- **Status:** Accepted
- **Date:** 2026-10-25 (changes the topic naming of [ADR-0023](0023-event-bus.md) and [ADR-0062](0062-a-kafka-consumer-for-every-service-and-event.md))

## Context

A Kafka topic was named `{Kafka:TopicPrefix}{EventName}` (`xyz.OrderCreated`) while the consumer group was named from the exchange (`XYZ_OrderService_OrderCreated`). Two settings said the same thing, the product (exchange) and the prefix, in two spellings, and the names of the
topic, the group and the RabbitMQ queue did not read alike.

## Decision

- **The topic of an event is `{ExchangeName}_{EventNameWithoutEto}`** (`XYZ_OrderCreated`), the dead-letter topic is `{ExchangeName}_dead-letter`, and the consumer group of a service for the event is `{ExchangeName}_{ConsumerClientName}_{EventNameWithoutEto}`
  (`XYZ_OrderService_OrderCreated`, [ADR-0062](0062-a-kafka-consumer-for-every-service-and-event.md)). The exchange name is the one namespace of a product on a cluster; with no exchange name the topic is just the event name.
- **`Kafka:TopicPrefix` is removed** (`KafkaOptions` has the bootstrap servers, the partitions and the replication factor). `KafkaTopology.TopicFor` and `DeadLetterTopic` take the `EventBusOptions`.

## Consequences

- One setting names a product on the broker, as it does on RabbitMQ (`ABC_OrderService_OrderCreated` queue, `XYZ_OrderService_OrderCreated` group).
- Topics of an earlier run (`xyz.*`) are not read any more; the samples are started from clean topics, groups and databases.
- The integration tests isolate themselves with an exchange name of their own instead of a prefix.

## Related

[ADR-0023](0023-event-bus.md), [ADR-0062](0062-a-kafka-consumer-for-every-service-and-event.md), [ADR-0061](0061-the-xyz-sample-beside-abc.md).
