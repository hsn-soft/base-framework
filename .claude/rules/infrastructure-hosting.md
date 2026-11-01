---
paths:
  - "src/HsnSoft.Base.Hosting/**"
  - "src/HsnSoft.Base.Caching.StackExchangeRedis/**"
  - "docs/Hosting.md"
  - "docs/Health.md"
---

# Infrastructure clients and hosting

- Reuse long-lived infrastructure clients/connections; do not open a client or connection per request, message, or health probe.
- Bound waits and retry only operations that are safe to repeat. Respect each provider's opt-in settings; do not invent universal timeout defaults.
- Centralize host limits and proxy settings in the existing hosting options. Trust forwarded headers only from configured proxies/networks, validate required settings at startup, and preserve safe startup failure behavior.
- Preserve the distinction between liveness, startup, and readiness checks. A dependency outage must not accidentally make a liveness probe fail.
- Preserve process identity (`AppInfo`), the configured thread-pool minimum, and the cross-event handler concurrency limit.
- Read [Hosting.md](../../docs/Hosting.md) and [Health.md](../../docs/Health.md) before changing host options, client lifetime, health checks, or startup behavior.

Relevant decisions: ADR-0117, ADR-0119–0121, ADR-0127.
