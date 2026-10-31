# ADR-0120: The host takes its limits and proxies from one section, reads a `.env` file, and starts safely

- **Status:** Accepted
- **Date:** 2026-10-31 (adds to [ADR-0071](0071-the-aspnetcore-layer-packages-build-on-aspnetcore.md))

## Context

Comparing the Program.cs of a production service of the old system with ours showed what a host needs and had no home. Kestrel's limits were not in the settings of any service, and **the section
`Kestrel:Limits` is not bound by ASP.NET Core** (measured: a body limit of 12345 in the settings left the default of 30 MB in force), so a limit written in the settings did nothing without a word. The
forwarded headers of a proxy were read by a gateway only; a service behind a proxy saw the address of the proxy. A service with a missing setting (an API key) failed at the first call that used it, not at
the start. A failure of the start (a database that cannot be prepared, a setting that is missing) ended the process with a stack trace on the console and nothing in the logs, because the logs are flushed by a
host that never ran. Keys that every service needs (the key of a scheduler) were set by hand in each settings file or secret.

## Decision

- **One section, `Hosting`** (`BaseHostingOptions`), in `HsnSoft.Base.AspNetCore`: `ShutdownTimeoutSeconds` (30), `Kestrel` (`MaxRequestBodySize`, `MaxRequestBufferSize`, `MaxRequestHeadersTotalSize`,
  `MaxRequestHeaderCount`, `RequestHeadersTimeout`, `KeepAliveTimeout`, `MaxConcurrentConnections`) and `ForwardedHeaders`. A Kestrel value that is not set keeps Kestrel's default; a body limit of `0` means no
  limit. The services write the defaults they run with in their settings, so what is in force is seen.
- **The port is the setting `Urls`**, which ASP.NET Core already reads (and `ASPNETCORE_URLS`, which wins): a development machine and a Compose file say `http://localhost:5420`, a pod says `http://+:8080`.
  No second key says the same.
- **Forwarded headers** (`X-Forwarded-For`, `-Proto`, `-Host`) are believed from `Hosting:ForwardedHeaders:KnownProxies` and `KnownNetworks` (CIDR) only, with `ForwardLimit` (1) and an `Enabled` switch.
  With neither list set only a proxy on the same machine is believed, so a client cannot say it is someone else. `UseBaseForwardedHeaders` is the first middleware.
- **`AddBaseHosting`** (on the builder, called first) reads a **`.env` file** (`EnvFile`: the current folder, then the folder of the application) *below* the environment variables and above the settings files, and
  applies the section. The file is optional; `A__B` is `A:B`, as in a variable. No library is added: the format is a few lines.
- **`RequireBaseSettings(keys)`** declares the settings a service cannot start without; **`RunBaseAsync`** builds the host, checks them (one error that names every missing key, never a value), runs it, and
  logs any failure of these steps as **critical** once, flushes the logs, and returns the exit code 1 (the platform sees a failed start). A failure before the host exists goes to the standard error.

## Consequences

- A limit, a proxy, the time to stop and the port are all in the settings of the service and are applied.
- A service with a missing setting does not start, and says which; its pod restarts instead of being ready and failing at the first call.
- A failed start leaves a critical log in Graylog and the file, with the reason.
- Not here: the thread pool and the limits of the consumers (their own decision), the application information and the root endpoint, and `AddAbcAuthentication`/`AddAbcAuthorization` (next steps).

## Related

[ADR-0071](0071-the-aspnetcore-layer-packages-build-on-aspnetcore.md), [ADR-0117](0117-the-health-endpoints-are-live-startup-and-ready-and-answer-with-the-checks.md), [docs/Hosting.md](../Hosting.md).
