# ADR-0138: A call to another service has the correlation id, safe retries and a token that goes only where it is for (`Http.Client`)

- **Status:** Accepted
- **Date:** 2026-11-03

## Context

The old `HsnSoft.Base.Http.Client` was dropped with the old projects and nothing replaced it ([the analysis](../old-framework/4-web-tools.md)). The need is real: the three web hosts of the samples (Abc commercial, Abc public, Hsx commercial) each hand-wrote the same handler (the correlation id and the bearer token) and the same code that reads a `BaseResponse`, with no timeout setting and no retry. The old package forwarded the caller's `Authorization` header to any host, which hands a user's token to whoever controls the address; the hhs system fetched user-supplied addresses with no guard against calls to the inside of its network (server-side request forgery); and the retry that the usual defaults give repeats a write that may already have been done.

## Decision

- **`HsnSoft.Base.Http.Client` is one package** on `Core` and `Microsoft.Extensions.Http.Resilience`, in three opt-in pieces on the `IHttpClientBuilder` and one set of helpers on `HttpClient`.
- **`AddBaseHttpClientDefaults`**: the address, the correlation id of the chain (`LogFlow.Current`) in `X-Correlation-Id`, a timeout for each attempt and for the whole call, retries with jitter for what is worth trying again and a circuit breaker (the standard resilience of .NET), all from a section of the settings with values that are safe. **A write is not retried** unless the client says its calls are idempotent (`RetryUnsafeMethods`).
- **`AddBearerTokenRelay(token, allowedHosts)`**: the token goes only to the listed hosts (at least one is required), never over an `Authorization` header the call already has, and automatic redirects are off.
- **`AddSsrfGuard`**: for a client that calls addresses from outside, the scheme, port and host are checked before the call and the address the name resolves to where the connection is made; private, loopback, link-local and reserved addresses are refused by default and redirects are off. A client that fetches user-supplied addresses has the guard; a client that carries a user's token never does.
- **The answer is read once**: the payload of a `BaseResponse`, or a `BaseHttpException` with the status, the messages and the error code.

## Consequences

The three copies in the samples are the package: a host writes its client as the calls it makes. The cost is the dependency of `Microsoft.Extensions.Http.Resilience` (and Polly through it) for a host that calls other services, which every such host has already chosen; a host that only serves does not reference the package. A client whose POSTs are reads must say so to have them retried. The guard is a defence at connection time, not a replacement for allowing only the hosts a product knows (`AllowedHosts`).

## Related

[ADR-0003](0003-abstractions-separate-from-providers.md), [ADR-0022](0022-web-api-answers.md), [ADR-0070](0070-a-framework-package-brings-what-its-api-exposes.md), [Calling another service](../HttpClient.md), [the old framework analysis](../old-framework/README.md).
