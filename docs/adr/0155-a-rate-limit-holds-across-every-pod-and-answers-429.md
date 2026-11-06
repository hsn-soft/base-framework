# ADR-0155: A rate limit holds across every pod and answers 429 (`RateLimiting`)

- **Status:** Accepted
- **Date:** 2026-11-06

## Context

A host that is open to the internet (the auth server of the hhs system, a public gateway) has to limit how often one caller may ask: a password or a client secret is guessed at a login and a token endpoint. The old auth server had a limiter written and commented out, the gateway had none, and the framework had only the **concurrency** limit (ADR-0143: how many run at once). The limiter of ASP.NET Core counts in one process, so a service of six pods lets each pod count for itself, six times the limit; and it answers its own 429 body, not the `BaseResponse` of the other errors. The auth server was written with the limiter of ASP.NET Core meanwhile (hhs auth server ADR-0018) and says so: its limits are of one instance.

## Decision

- **`IRateLimiter`** (`HsnSoft.Base.RateLimiting`) counts one request of a key under a name in a window and answers whether it is within the limit, how many are left and when the window ends. The window is a **fixed window that starts with the first request of the key**; **a refused request is counted and does not lengthen the window**. `InMemoryRateLimiter` counts in one process (and sweeps the ended windows as it is used).
- **`HsnSoft.Base.RateLimiting.StackExchangeRedis`** is the limiter for a cluster: one counter for each key, **one script** per request (increment, time to live on the first, read what is left), the window by **the clock of Redis**. If Redis cannot be reached the call throws.
- **`HsnSoft.Base.RateLimiting.AspNetCore`** has `[RateLimit(name, permits)]` (`PerSeconds`, `By`) for a controller action, `RequireRateLimit` for a minimal api endpoint, and `UseBaseRateLimiting` for every request of one address (`RateLimiting:Global`, off until permits are set, `/health` never counted). A request over the limit is answered at once with **429, `Retry-After` and a `BaseResponse` with the error code `rate_limit_exceeded`**. The caller is the address (after `UseBaseForwardedHeaders`) or, with `RateLimitBy.User`, the signed-in user.
- **The permits and the window can be changed by the settings** (`RateLimiting:Limits:{name}`), with no deployment; the code gives the default. **A host fails when the store is gone, unless `RateLimiting:FailOpen`** says the request matters more than its limit: a limit that protects a login is never silently off.
- **It is a family of three packages, like `Concurrency`** (the contract and the in-memory limiter; the Redis provider; the web layer), and a provider never reaches the web layer.

## Consequences

"Ten sign-ins a minute for one address, in all the pods" is an attribute. The cost: the window is fixed (a caller can use two windows' permits around the edge between them), the limit depends on Redis being there (or on `FailOpen`), and an address behind a proxy that is not listed in `Hosting:ForwardedHeaders` is the proxy's: the host must list its proxies. The hhs auth server and the gateways take this package when it is released, and the Hsx sample follows them.

## Related

[ADR-0003](0003-abstractions-separate-from-providers.md), [ADR-0139](0139-the-host-writes-security-headers-and-names-the-origins-that-may-call-it.md), [ADR-0143](0143-a-limit-on-how-many-run-at-once-holds-across-every-pod.md), [RateLimiting](../RateLimiting.md).
