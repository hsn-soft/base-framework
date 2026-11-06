# How many at once, in every pod: `HsnSoft.Base.Concurrency`

A service that runs in six pods cannot say "this endpoint runs at most twice at the same time" with a limiter of the process: each pod would allow two, twelve in all. `IConcurrencyLimiter` is a **named semaphore for the whole service**. Why it is this way: [ADR-0143](adr/0143-a-limit-on-how-many-run-at-once-holds-across-every-pod.md).

| Package | What |
|---|---|
| `HsnSoft.Base.Concurrency` | `IConcurrencyLimiter` and `IConcurrencyLease`, the settings (`Concurrency`), and the in-memory limiter (one process: development, tests, a service of one instance) |
| `HsnSoft.Base.Concurrency.StackExchangeRedis` | The limiter for a cluster, on the Redis of the service |
| `HsnSoft.Base.Concurrency.AspNetCore` | `[ConcurrencyLimit]` for a controller action and `RequireConcurrencyLimit` for a minimal api endpoint |

```csharp
builder.Services
    .AddBaseRedis(builder.Configuration)                    // the connection the cache and the limiter share
    .AddRedisConcurrencyLimiter(builder.Configuration);     // or AddInMemoryConcurrencyLimiter for one process
```

## On an endpoint

```csharp
[HttpPost("send-invoices")]
[ConcurrencyLimit("send-invoices", 2, LeaseSeconds = 120)]          // at most 2 at once in all the pods
public Task<SendResult> SendAsync(...) { ... }

app.MapPost("/export", Export).RequireConcurrencyLimit("export", 1);   // a minimal api: the same
```

A request that finds the places taken is answered **at once** with `429` and `Retry-After` (a `BaseResponse` with the error code `concurrency_limit_reached`); it does not wait. The place is held until the answer has been written and is given back whatever way the request ends. Two endpoints with one name share the places.

## In code

```csharp
await using IConcurrencyLease? lease = await limiter.TryAcquireAsync("nightly-job", 1, TimeSpan.FromMinutes(10), cancellationToken);
if (lease is null) { return; }          // another pod is running it
// ... the work; for work that takes longer than expected: await lease.RenewAsync(TimeSpan.FromMinutes(10));
```

A limit of **1** is a lock: a job that must run in one pod at a time.

## The lease

A holder has a lease that **ends by itself**: a pod that dies while it holds a place does not hold it for ever, the place is free again when the lease ends. The lease is therefore the longest the work is expected to take (`LeaseSeconds`, else `Concurrency:DefaultLeaseSeconds`, 60); work that runs longer renews it (`RenewAsync`, which says `false` when the lease had ended and the place was given to another). A lease that is too short lets a second run start while the first still works; one that is too long keeps a dead pod's place for that long.

## Settings (`Concurrency`)

```json
"Concurrency": {
  "KeyPrefix": "orders",
  "DefaultLeaseSeconds": 60,
  "RetryAfterSeconds": 1,
  "Limits": { "send-invoices": { "Max": 4, "LeaseSeconds": 180 } }
}
```

`Limits` changes the maximum and the lease of a name **without a deployment** (what the code says is the default). `KeyPrefix` keeps the keys of a service from meeting those of another on a shared Redis.

## How the Redis limiter works

A name is a sorted set: the members are the holders, the scores are when their leases end **by the clock of Redis** (the pods need no same time). Taking a place is one script that removes the ended leases and adds a holder only if fewer than the maximum are left, so two pods that ask in the same moment cannot both take the last place. If Redis cannot be reached the call throws; it does not let the work through unlimited.
