# ADR-0119: The infrastructure clients share their connections, bound their waits and retry only what is safe to retry

- **Status:** Accepted
- **Date:** 2026-10-31 (the findings of the review of the framework for a service that runs as many pods under load; adds to [ADR-0117](0117-the-health-endpoints-are-live-startup-and-ready-and-answer-with-the-checks.md))

## Context

A review of the infrastructure packages for what happens in production (many pods, a dependency that is slow or goes away, a long run) found places where a service holds more than it needs or waits for ever. Each is a small decision; they are kept together here, with the reason of each, and the list grows as the review goes on.

## Decision

- **Redis: the cache and the service share one connection.** `AddBaseRedis` registered the cache (`IDistributedCache`), which made a multiplexer of its own from the connection string, and an `IConnectionMultiplexer` for the service: two multiplexers, which is four connections to Redis in every pod, and two states of
  the connection (the readiness check could say healthy while the cache's own connection was down). The cache is now given the connection of the service (`RedisCacheOptions.ConnectionMultiplexerFactory`): two connections per pod, one state, and the readiness check looks at the connection the cache uses.
  The cache closes the connection when it is disposed, which the container does at the end with the rest of the singletons (tried: wrapping the connection so that it cannot be closed does not work, the cache reaches it through its database); nothing uses Redis after that.

## Consequences

- The connections to Redis of a system are halved (the test counts them on a real Redis: two for a host).
- A service that makes its own use of Redis after the cache is disposed would find the connection closed: at the end of the process only.

## Related

[ADR-0117](0117-the-health-endpoints-are-live-startup-and-ready-and-answer-with-the-checks.md).
