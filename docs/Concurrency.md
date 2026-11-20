# How many at once, in every pod: `HsnSoft.Base.Concurrency`

A service that runs in six pods cannot say "this endpoint runs at most twice at the same time" with a limiter of the process: each pod would allow two, twelve in all. `IConcurrencyLimiter` is a **named semaphore for the whole service**. Why it is this way: [ADR-0143](adr/0143-a-limit-on-how-many-run-at-once-holds-across-every-pod.md).

| Package | What |
|---|---|
| `HsnSoft.Base.Concurrency` | `IConcurrencyLimiter` and `IConcurrencyLease`, the settings (`Concurrency`), and the in-memory limiter (one process: development, tests, a service of one instance) |
| `HsnSoft.Base.Concurrency.StackExchangeRedis` | The limiter for a cluster, on the Redis of the service |
| `HsnSoft.Base.Concurrency.AspNetCore` | `[ConcurrencyLimit]` for a controller action and `RequireConcurrencyLimit` for a minimal api endpoint, with `HandOff` for a job |
| `HsnSoft.Base.Concurrency.EventBus` | The place travels with an event: the handler keeps it alive and gives it back ([a job](#a-job-the-endpoint-answers-at-once-and-the-handler-does-the-work)) |

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

## A job: the endpoint answers at once and the handler does the work

A job that takes a quarter of an hour must not be a request the caller (the scheduler) waits for, and it must not overlap itself. The endpoint takes the place, publishes **one** event and answers; the handler of the event does the work; **the place is free when the work ends**. Why: [ADR-0159](adr/0159-a-place-of-a-concurrency-limit-is-handed-over-with-an-event-so-a-job-answers-at-once-and-is-over-when-its-handler-is.md).

```csharp
builder.Services.AddRedisConcurrencyLimiter(builder.Configuration).AddConcurrencyHandoff();   // both halves, in the service that publishes and in the one that handles (often the same)

[HttpPost("jobs/nightly-export")]
[ConcurrencyLimit("nightly-export", 1, LeaseSeconds = 1200, HandOff = true)]       // the lease is the longest the HANDLER is expected to take
public async Task<JobAcceptedDto> StartAsync(CancellationToken ct)
{
    await eventBus.PublishAsync(new NightlyExportRequestedEto(), cancellationToken: ct);   // the first event takes the place
    return new JobAcceptedDto();
}

public sealed class NightlyExportHandler : IIntegrationEventHandler<NightlyExportRequestedEto>
{
    public Task HandleAsync(MessageEnvelope<NightlyExportRequestedEto> envelope, CancellationToken ct) { /* the work: a quarter of an hour */ }
}
```

| What happens | The place |
|---|---|
| The endpoint is called while a run is going | `429` (`concurrency_limit_reached`), as without hand-off |
| The endpoint publishes and answers | **held**: the event carries it (three `Local-Concurrency*` headers) |
| The handler works | **kept alive**: the lease is renewed three times in each lease time |
| The handler ends: handled, rejected, or failed on every attempt | **given back** |
| The endpoint publishes nothing, or fails | given back by the endpoint (no handler is coming) |
| The pod of the handler dies | the lease ends by itself: free again |
| The host stops while the handler works | not given back: the message comes back, or the lease ends |
| The message is delivered again later | its holder owns nothing by then: it renews nothing and frees nothing |

Only the **first** event of the endpoint carries the place, and the headers are `Local-` (not passed on to the events the handler publishes: `ParentMessageEnvelope.LocalHeaderPrefix`). A handler of the event bus can also watch any handling with an `IEventHandlingObserver` (begin and end of the whole handling of one message, with its outcome). A delivery stays unacknowledged while the handler works: a job longer than the `consumer_timeout` of the broker (RabbitMQ: 30 minutes by default) is delivered again.

## In code, between processes

`limiter.Attach(name, holder)` is the lease of a place that another process took (`lease.Holder` is what travels): renew it, give it back; a place that has ended is not renewed, and giving it back frees nothing of another's.

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
