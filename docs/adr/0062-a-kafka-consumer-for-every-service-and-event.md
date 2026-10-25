# ADR-0062: A Kafka consumer, in a group of its own, for every service and event

- **Status:** Accepted
- **Date:** 2026-10-25 (changes how [ADR-0023](0023-event-bus.md) reads on Kafka)

## Context

The first Kafka subscriber was one consumer in one group per service (`{ExchangeName}_{ConsumerClientName}`) subscribed to the topics of all the events the service handles. It read one message at a time, so a slow or stuck handler of one event held back every other event of the
service, and the offsets of the events were one commit of one group. The RabbitMQ bus does not behave like that: every service makes a queue of its own for every event (`ABC_OrderService_OrderCreated`), and two services that handle the same event, or two events of one service, are independent.

## Decision

- **Every handled event gets a consumer of its own, running on its own thread, in a consumer group of its own**, named like the RabbitMQ queue of the same service and event: `{ExchangeName}_{ConsumerClientName}_{EventName}` (`XYZ_OrderService_OrderCreated`; `KafkaTopology.GroupFor`).
  The topic stays `{TopicPrefix}{EventName}` and is shared: each group reads it with its own offset and works through the messages independently of every other group (the services that handle the event, and the other events of the same service).
- A consumer still reads **one message at a time** and commits after it is dealt with (a crash re-delivers it); the dispatch, the retries and the dead-letter topic (one for the bus, `x-original-topic` says where a message came from) are unchanged.
- The names are as good as a first proposal: they follow RabbitMQ so the two buses read alike; a different pattern is a change of `GroupFor` only.

## Consequences

- A stuck handler holds back only its own event. A service now has one connection and one thread per handled event instead of one in all (Kafka's groups, not partitions, are what separates them; partitions of a topic still spread one group over instances).
- More groups on the broker (services x events) and, on the first start after this change, groups that begin at the earliest offset: a service that ran before reads its topics again from the start, and the old per-service groups stay on the broker unused until somebody deletes them. A handler must stand a redelivery; the stock reservation of the Xyz product service does not (it takes the stock again for every `OrderCreated`), so replaying old messages emptied the stock of the sample: reset the sample's topics, groups and databases together (or make that handler idempotent by order id, which is still to do).
- Tests: two services read the same event each with its own offset, and a blocked event does not hold back another one of the same service (integration, a real Kafka).

## Related

[ADR-0023](0023-event-bus.md), [ADR-0057](0057-the-caller-of-a-message-and-publish-or-send.md), [ADR-0061](0061-the-xyz-sample-beside-abc.md).
