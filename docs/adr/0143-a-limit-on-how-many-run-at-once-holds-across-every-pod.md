# ADR-0143: A limit on how many run at once holds across every pod (`Concurrency`)

- **Status:** Accepted
- **Date:** 2026-11-03 (corrects the "no distributed lock" item of [ADR-0133](0133-what-is-not-brought-back-from-the-old-framework-and-the-risks-we-accept.md), which answered a different question)

## Context

A service runs in several pods, and an endpoint or a job must not run more than N times at the same moment in all of them together (a heavy export, a call to a provider that allows few at once, a job that must have one runner). The limiter of ASP.NET Core counts in one process, so six pods allow six times the limit. The old framework's `RedisLockService` was a get-then-set (not atomic) mutex, and ADR-0133 recorded that a lock is not brought back; that was the wrong question: what is needed is a **counting** semaphore for the cluster, of which a lock is the case N = 1.

## Decision

- **`IConcurrencyLimiter`** (`HsnSoft.Base.Concurrency`) gives a named place among at most `max` or `null` when they are taken (it does not wait): a **lease** that ends by itself after its time, so a pod that dies does not hold a place for ever, and that can be renewed by work that takes longer. `InMemoryConcurrencyLimiter` counts in one process.
- **`HsnSoft.Base.Concurrency.StackExchangeRedis`** is the limiter for a cluster: a sorted set for each name, the end of each lease by **the clock of Redis**, and taking a place is **one script** (remove the ended, add only if fewer than the maximum), so the limit holds with any number of pods asking at once. If Redis cannot be reached the call fails; it never lets the work through unlimited.
- **`HsnSoft.Base.Concurrency.AspNetCore`** has `[ConcurrencyLimit(name, max)]` for a controller action and `RequireConcurrencyLimit` for a minimal api endpoint: a request that finds the places taken is answered at once with 429, `Retry-After` and a `BaseResponse`, and the place is held until the answer is written and given back whatever way the request ends.
- **The maximum and the lease can be changed by the settings** (`Concurrency:Limits:{name}`), with no deployment; the code gives the default.

## Consequences

"At most two at once, in all the pods" is an attribute. The cost: a request that finds the places taken is refused, not queued (the caller retries after `Retry-After`); the lease has to be the longest the work is expected to take (too short lets a second run begin while the first still works); the limit depends on Redis being there. Strict mutual exclusion that must survive a lease that ended while the work was running (a fencing token) is not given: the work that needs it keeps its own check in its database.

## Related

[ADR-0003](0003-abstractions-separate-from-providers.md), [ADR-0133](0133-what-is-not-brought-back-from-the-old-framework-and-the-risks-we-accept.md), [Concurrency](../Concurrency.md).
