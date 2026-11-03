# Calling another service: `HsnSoft.Base.Http.Client`

A service, a web host or a worker that calls another service over HTTP registers the client with the pieces it needs; each piece is one call. Why it is this way: [ADR-0138](adr/0138-a-call-to-another-service-has-a-correlation-id-safe-retries-and-a-token-that-goes-only-where-it-is-for.md). The package references `Core` and `Microsoft.Extensions.Http.Resilience`, nothing else.

```csharp
builder.Services
    .AddHttpClient<GatewayClient>()
    .AddBaseHttpClientDefaults(builder.Configuration, "Gateway")             // the address, the correlation id, timeouts, retries, a circuit breaker
    .AddBearerTokenRelay(
        services => services.GetRequiredService<IHttpContextAccessor>().HttpContext?.User.FindFirst("access_token")?.Value,
        new Uri(builder.Configuration["Gateway:BaseUrl"]!).Host);             // the token of the user, only to this host
```

## What `AddBaseHttpClientDefaults` does

It reads `BaseHttpClientOptions` from the section you name (`Gateway`) and puts on the client:

| Setting | Default | Meaning |
|---|---|---|
| `BaseUrl` | none | The address the client calls (http or https); required unless the host sets `BaseAddress` itself |
| `AttemptTimeoutSeconds` | 10 | One attempt |
| `TotalTimeoutSeconds` | 30 | The call with its retries (at least the attempt timeout) |
| `MaxRetries` | 3 | A timeout, a connection failure, a 5xx, a 408 and a 429 are tried again, with growing waits and jitter; 0 for none |
| `RetryDelayMilliseconds` | 2000 | The wait before the first retry |
| `RetryUnsafeMethods` | `false` | **A write (POST, PUT, PATCH, DELETE) is not tried again**: it may have been done before the failure, and would be done twice. Turn it on for a client whose calls are idempotent (reads sent as POST, a call with an idempotency key); `AddBaseHttpClientDefaults(..., configure)` can do it in code |
| `CircuitBreakerEnabled` | `true` | After a run of failures calls fail at once for a while |
| `CorrelationIdHeader` | `X-Correlation-Id` | The header of the correlation id |

The correlation id is the one of the chain the code is part of (`LogFlow.Current`: the request being served by a host that uses the request logging, the message being handled), so a page, the gateway and the services behind it are one chain in the logs. A call that already has the header keeps it.

## The token goes only where it is for

`AddBearerTokenRelay(token, allowedHosts)` sends `Authorization: Bearer` with the token that `token` gives (a web host reads the cookie of the user) **only to the hosts you list** (at least one): a call to another host is sent without it, and a call that already has an `Authorization` header keeps its own. Automatic redirects are off for the client, so a redirect cannot take the token to another host.

## An address that came from outside: the guard

A client that fetches what a user pointed to (a file, a page) must not be made to call the inside of its own network (the cloud metadata address, a database, another service). `AddSsrfGuard(options => ...)` checks the scheme, the port and the host before the call, and the address a name resolves to where the connection is made, so only the public internet is reached; redirects are off (a redirect is an address nobody checked). It is for a client of its own, never with the token relay.

| `SsrfGuardOptions` | Default | Meaning |
|---|---|---|
| `AllowedPorts` | 80, 443 | Empty for any |
| `AllowedHosts` | empty | A name, or `*.example.com` for the names under it; empty for any public host |
| `RequireHttps` | `false` | Only https |
| `AllowPrivateNetworks` | `false` | Private, loopback, link-local, shared (100.64/10), benchmarking, documentation, multicast and reserved addresses, IPv4 and IPv6, and IPv4 inside IPv6, are refused; on for a development machine only |

A refused call is an `SsrfBlockedException` (an `HttpRequestException`).

## Reading the answer

The services answer with a `BaseResponse`; `GetPayloadAsync<T>`, `PostPayloadAsync<T>`, `PutPayloadAsync<T>` and `SendPayloadAsync<T>` on the `HttpClient` return its payload (camelCase JSON, names not case sensitive, enums as texts; `default` for 204 or no payload), and `DeleteResourceAsync` checks a delete. A failure is a `BaseHttpException` with the status and the messages the service gave, and the error code in `Exception.Data["errorCode"]`; a body that is not a `BaseResponse` (a proxy's page) gives the status and its reason. A caller that must tell a refused token from a failed call reads `HttpStatusCode` (401).
