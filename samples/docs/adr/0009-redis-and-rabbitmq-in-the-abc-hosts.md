# ADR-0034: Redis and RabbitMQ in the Abc hosts

- **Status:** Accepted
- **Date:** 2026-10-25
- **Note:** [ADR-0072](0072-a-host-chooses-its-localization-and-its-cache.md): Redis is not registered by `AddAbcRedis` of the hosting any more; each host that wants it calls the framework's `AddBaseRedis` and references the Redis package itself (the microservices, the auth server and the commercial gateway still do).

## Context

The Abc hosts have their databases; the system also needs a cache and an event bus, as the Xyz sample has. Which host gets which connection, and where the registration lives, has to be the same for all of them.

## Decision

- **Redis** (`HsnSoft.Base.Caching.StackExchangeRedis`: `IDistributedCache`, the raw connection and a readiness check) is registered by `AddAbcRedis` in `Abc.Shared.Hosting`. **Every microservice and the auth server get it through `AddAbcMicroservice`, the commercial gateway calls it
  in its `Program`; the public gateway does not**, as the host decides. The settings are `ConnectionStrings:Redis` (`localhost:6379`) and `Redis:InstanceName` = `abc:`, one prefix for the keys of the whole system.
- **RabbitMQ** (`HsnSoft.Base.EventBus.RabbitMQ`: the publisher, the consumer of the handlers, a readiness check) is registered by `AddAbcEventBus` in `Abc.Shared.Hosting.Microservices`, which also adds `AbcEventHeaderProvider`: the events a request causes carry who started it
  (`UserId`, `UserRoles`, `ClientChannel`, `ClientVersion`) and its correlation id, so the consume logs of a chain say so. **The four services (identity, order, product, shipment) call it; the auth server does not.** The settings are `RabbitMq:Connection` (`localhost:5672`, `guest`)
  and `RabbitMq:EventBus` (exchange `abc.events`, `MaxHandlerAttempts` 3, `ConsumerClientName` = the service, e.g. `product-service`).
- **Both are checked by `/health/ready`** (opening a connection is the check): a host that cannot reach Redis or RabbitMQ answers 503 there and stays alive (`/health/live` is 200).
- The services add their handlers with `AddEventHandler` when they have events; none has yet.

## Consequences

- Redis and RabbitMQ have to be running for the hosts to be ready; a host starts without them (the connections are made lazily, Redis is allowed to come later), but it is not ready.
- Nothing uses the cache or publishes an event yet: this is the connection. The first uses are for the next decisions (rate limit and refresh tokens in Redis, the order-to-stock flow on the bus).
- The product code (`AbcEventHeaderProvider`) is in the Abc shared project, not in the framework: the event bus only carries headers.

## Related

[ADR-0028](0028-shared-hosting-projects.md), [ADR-0030](0030-microservice-project-pattern.md), [ADR-0033](0033-the-abc-services-crud-pattern.md), `samples/README.md`.
