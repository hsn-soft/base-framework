# ADR-0117: The health endpoints are live, startup and ready, and answer with the checks

- **Status:** Accepted
- **Date:** 2026-10-31 (adds to [ADR-0071](0071-the-aspnetcore-layer-packages-build-on-aspnetcore.md); the connection rules are the ones of [ADR-0103](0103-brokers-are-reached-with-tls-and-sasl-when-the-settings-say-so.md))

## Context

The health endpoints answered `Healthy` or `Unhealthy` and nothing else: at three in the night nobody could tell which dependency had fallen without the logs. The old framework had three
endpoints (`StartupCheck`, `LivenessCheck`, `ReadinessCheck`), this one two. A check that hangs held a probe for as long as the platform waited (a registration has **no timeout at all** unless its
owner gives one). The RabbitMQ check opened a connection of its own on every probe, in every pod: the broker showed thousands of connections. The Redis check ignored the token of the probe, and its
description (and the message of any exception a check throws) had the address of the server.

## Decision

- **Three endpoints:** `/health/live` (the process answers, no dependency is asked), `/health/startup` and `/health/ready` (the checks tagged `ready`, the same question: the startup probe of the platform
  asks it only until the service has started and gives a slow start its time without loosening the readiness probe). The paths replace `StartupCheck`, `LivenessCheck` and `ReadinessCheck`; a product that
  moves from the old framework changes the probes of its manifests.
- **The answer is JSON:** the status, how long it took and, for every check, its name, status, description and duration. The message of an exception is **not** in it (it has addresses); `Health:IncludeErrors`
  puts it in for a host that is reachable only from where it is safe. The description of a check never has an address.
- **Every check ends:** `Health:CheckTimeout` (5 seconds) is put on every check that has no timeout of its own, the token of the check is cancelled when it passes, and a check lets the cancellation through.
- **A check is switched off by its name, in the settings:** `Health:Disabled` lists the names (`redis`, `rabbitmq`, `kafka`, `mongo`, `minio`, `azure-blob`, `aws-s3`, a database by its provider: `postgres`,
  `sqlserver`, `mysql`; a second context also has its own name after the provider). A service that has a package but does not want it to decide readiness, or an environment without a dependency, needs no change
  of code. A check of the service itself is added with `AddBaseReadyCheck<TCheck>(name)`.
- **A dependency that is down makes the service not ready, Redis included:** the cache, the database and the broker are hard dependencies; a pod that cannot reach one gets no traffic. A gateway looks at itself only: a
  service that is down answers its routes with a 502.
- **The checks look at what the service has instead of opening more:** the RabbitMQ check uses the one persistent connection (it connects it when a service that only publishes has not yet, never replaces one that
  is being recovered), so any number of probes show one connection on the broker; the Redis check answers at once when the connection is down and ends with the token.

## Consequences

- A probe tells which dependency fell, at no more cost to the dependency than the service itself.
- A dependency that hangs costs a probe five seconds, not the time of the platform's timeout, and the cancelled check frees its call.
- The old paths are gone: the probes of a service that moves to this framework change (`/health/startup`, `/health/live`, `/health/ready`).
- Not here: a cache of the answer (the probes of one pod come from one place, the cost is a ping), and the check of a gateway for the services behind it.

## Related

[ADR-0071](0071-the-aspnetcore-layer-packages-build-on-aspnetcore.md), [ADR-0103](0103-brokers-are-reached-with-tls-and-sasl-when-the-settings-say-so.md), [docs/Health.md](../Health.md).
