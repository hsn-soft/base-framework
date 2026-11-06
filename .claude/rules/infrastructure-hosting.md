---
paths:
  - "src/HsnSoft.Base.Hosting/**"
  - "src/HsnSoft.Base.Caching.StackExchangeRedis/**"
  - "src/HsnSoft.Base.RateLimiting*/**"
  - "test/HsnSoft.Base.RateLimiting*/**"
  - "docs/RateLimiting.md"
  - "docs/Hosting.md"
  - "docs/Health.md"
---

# Infrastructure clients and hosting

- Reuse long-lived infrastructure clients/connections; do not open a client or connection per request, message, or health probe.
- Bound waits and retry only operations that are safe to repeat. Respect each provider's opt-in settings; do not invent universal timeout defaults.
- Centralize host limits and proxy settings in the existing hosting options. Trust forwarded headers only from configured proxies/networks, validate required settings at startup, and preserve safe startup failure behavior.
- Preserve the distinction between liveness, startup, and readiness checks. A dependency outage must not accidentally make a liveness probe fail.
- Preserve process identity (`AppInfo`), the configured thread-pool minimum, and the cross-event handler concurrency limit.
- A product's shared hosting is in layers (ADR-0151): the core (logging only), the kind of host (`.Http` for web hosts, `.Gateways`, `.Microservices`, `.Agents`), and what a host chooses (broker, cache, database, texts) referenced only by the host that uses it.
- A host that is open to the internet limits how often one caller may ask (`RateLimiting`, ADR-0155): `[RateLimit]` on the endpoints where a password or a secret is guessed and `UseBaseRateLimiting` after `UseBaseForwardedHeaders`, with the Redis limiter in a cluster (the in-memory one counts per pod). The limiters throw when the store is gone and a host fails unless `RateLimiting:FailOpen` is set; a refused request is counted and does not lengthen the window; the address is the one the listed proxies give. Tests move the time with a `TimeProvider`, never by waiting.
- Read [Hosting.md](../../docs/Hosting.md) and [Health.md](../../docs/Health.md) before changing host options, client lifetime, health checks, or startup behavior.

Relevant decisions: ADR-0117, ADR-0119–0121, ADR-0127, ADR-0143, ADR-0151, ADR-0155.
