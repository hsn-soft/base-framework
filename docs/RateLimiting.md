# How many requests a caller may make, in every pod: `HsnSoft.Base.RateLimiting`

A host that is open to the internet (an auth server, a public gateway) is asked a password a thousand times by one address unless something says no. The limiter of ASP.NET Core counts in one process, so a service in six pods lets each pod count for itself: six times the limit. `IRateLimiter` is **a counter per caller for the whole service**. Why it is this way: [ADR-0155](adr/0155-a-rate-limit-holds-across-every-pod-and-answers-429.md).

| Package | What |
|---|---|
| `HsnSoft.Base.RateLimiting` | `IRateLimiter`, the settings (`RateLimiting`), and the in-memory limiter (one process: development, tests, a service of one instance) |
| `HsnSoft.Base.RateLimiting.StackExchangeRedis` | The limiter for a cluster, on the Redis of the service |
| `HsnSoft.Base.RateLimiting.AspNetCore` | `[RateLimit]` for a controller action, `RequireRateLimit` for a minimal api endpoint and `UseBaseRateLimiting` for every request of one address |

```csharp
builder.Services
    .AddBaseRedis(builder.Configuration)                // the connection the cache and the limiter share
    .AddRedisRateLimiter(builder.Configuration);        // or AddInMemoryRateLimiter for one process

app.UseBaseForwardedHeaders();                          // first: the address of the caller is the one the proxy saw
app.UseBaseRateLimiting();                              // every request of one address (RateLimiting:Global), before the authentication
```

## On an endpoint

```csharp
[HttpPost("login")]
[RateLimit("sign-in", 10)]                              // 10 requests of one address a minute, in all the pods
public Task<TokenDto> LoginAsync(...) { ... }

[RateLimit("export", 3, PerSeconds = 3600, By = RateLimitBy.User)]   // 3 an hour for one signed-in user
app.MapPost("/token", Token).RequireRateLimit("sign-in", 10);          // a minimal api: the same
```

A request over the limit is answered **at once** with `429`, `Retry-After` (the seconds until the window ends) and a `BaseResponse` with the error code `rate_limit_exceeded`; it does not wait. Two endpoints with one name share the count of a caller (a login and a token endpoint are one limit, because a password or a secret is guessed at both).

**Who is counted** (`By`): the **address** by default (`ip:...`), the signed-in **user** with `RateLimitBy.User` (a caller who is not signed in is counted by its address). The address is the one `UseBaseForwardedHeaders` gives, so the proxies of `Hosting:ForwardedHeaders` have to be listed: otherwise every caller is the proxy and one caller uses up the limit of all.

## The whole host

`UseBaseRateLimiting()` counts every request of one address under the name `global`; it does nothing until `RateLimiting:Global:Permits` is set. The paths in `ExcludedPaths` (`/health` by default) are never counted: a probe of the platform must not be refused.

## The window

A window **starts with the first request of a key** and ends after its time; the count starts again then (a fixed window, not a sliding one: a caller can make the permits at the end of one window and again at the start of the next). **A refused request is counted too and does not lengthen the window**, so a caller that keeps knocking stays refused until the window ends and is let in at that moment.

## Settings (`RateLimiting`)

```json
"RateLimiting": {
  "KeyPrefix": "auth",
  "FailOpen": false,
  "Global": { "Permits": 300, "WindowSeconds": 60, "ExcludedPaths": [ "/health" ] },
  "Limits": { "sign-in": { "Permits": 5, "WindowSeconds": 60 } }
}
```

`Limits` changes the permits and the window of a name **without a deployment** (what the code says is the default). `KeyPrefix` keeps the keys of a service from meeting those of another on a shared Redis. A limit that cannot be right (below one permit or one second) stops the call with an error that names it.

## When the store is gone

The limiters **throw** when Redis cannot be reached: a limit that protects a login is never silently off. The default of a request is therefore to **fail**. `RateLimiting:FailOpen: true` lets the request through and logs the failure, for an endpoint that matters more than its limit.

## How the Redis limiter works

The count of a key under a name is one Redis counter, `{KeyPrefix}:{name}:{key}`. A request is **one script**: increment, give the counter its time to live when it is the first (the window, by **the clock of Redis**, so the pods need no same time), and read the time that is left. Two pods that count in the same moment cannot both see the last permit.
