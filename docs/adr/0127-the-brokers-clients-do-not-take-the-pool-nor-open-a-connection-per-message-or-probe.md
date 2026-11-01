# ADR-0127: The brokers' clients do not take the thread pool, nor open a channel per message or a client per probe

- **Status:** Accepted
- **Date:** 2026-11-01 (adds to [ADR-0023](0023-event-bus.md) and [ADR-0119](0119-the-infrastructure-clients-share-their-connections-and-bound-their-waits.md))

## Context

Reading the two brokers' code for the cost a busy service pays found four things:
- **Kafka held a thread of the pool for each event.** A consumer runs `Consume`, which blocks for a quarter of a second, in a loop that was started with `Task.Run`: a pool thread, almost always in `Consume`. A service that listens to a hundred events held a hundred
  threads of the pool and starved everything that needs one (the requests, the handlers).
- The revoke handler (partitions taken from a consumer) waited for the handlings with the **shutdown timeout** (it was not its own setting) and said nothing about it.
- The Kafka health check **built an admin client for every probe**: a connection to the brokers and a metadata request each time, on every pod, as the RabbitMQ check did before ADR-0117.
- The service **created its topics** at every start, always, with one partition: a platform that makes its topics could not say so.
- RabbitMQ **opened a channel (with confirms) and closed it for every message**, and declared the exchange again each time: two round trips and a channel churn on the broker for each message.

## Decision

- **A Kafka consumer reads on a thread of its own** (`TaskCreationOptions.LongRunning`, a blocking loop), so the pool is not touched however many events a service has; the handlers still run on the pool.
- **`Kafka:RevokeTimeout`** (30 s, shorter than `MaxPollInterval`, checked at the start) bounds the wait of a revoke for the handlings; it is logged with how long it waited and whether everything finished.
- **One admin client** (lazily made, disposed with the host, reconnecting by itself) answers every health probe; the probe does not wait for the brokers once it is cancelled.
- **`Kafka:CreateTopics`** (default true) turns the creation of topics off for a platform that makes them.
- **RabbitMQ keeps publisher channels** (`EventBusOptions.PublisherChannelPoolSize`, default 4, 0 = none): a publish takes one for itself and gives it back once confirmed; a channel on which a publish failed or that is closed is closed and not reused; the pool keeps
  at most its size idle; the exchange or queue is declared once per channel.

## Consequences

- A service with many events does not starve for threads because of its consumers, and a message costs its publish, not a channel.
- The brokers see a connection and a channel per pod, not per probe or per message.
- A publish that failed leaves no half-known channel behind.
- Not here: a pool of the channels of the failed event notifier (it publishes rarely) and Kafka's cooperative rebalancing (a stop-the-world rebalance with a bounded wait is kept).

## Related

[ADR-0023](0023-event-bus.md), [ADR-0117](0117-the-health-endpoints-are-live-startup-and-ready-and-answer-with-the-checks.md), [ADR-0119](0119-the-infrastructure-clients-share-their-connections-and-bound-their-waits.md), [docs/EventBus.md](../EventBus.md).
