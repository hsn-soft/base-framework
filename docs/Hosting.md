# The host

`HsnSoft.Base.AspNetCore` gives a service one place for what its host does, all in the settings of the service ([ADR-0120](adr/0120-the-host-takes-its-settings-from-one-section-and-starts-safely.md)).

```csharp
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddBaseHosting();                                   // first: the .env file and the section Hosting
builder.Services.RequireBaseSettings("Scheduler:ApiKey");   // the service does not start without it

return await builder.RunBaseAsync(app =>
{
    app.UseBaseForwardedHeaders();                          // first middleware
    // ... the rest of the pipeline
});
```

## The shared hosting of a product

A product that has several hosts keeps their composition in shared hosting projects, in layers (ADR-0151): `{Product}.Shared.Hosting` has what every host has (the logging) and no ASP.NET; `.Http` has what every web host has (the web api shell, Swagger, the bearer token) and the gateways, microservices and gRPC services build on it; an agent has `.Agents` on the core; and the message broker, Redis, a database and the texts of several languages are referenced only by the host that uses them, so a host without a broker does not restore one.

## Settings

```json
{
  "Urls": "http://localhost:5420",
  "Hosting": {
    "ShutdownTimeoutSeconds": 30,
    "ThreadPool": { "MinWorkerThreads": 32, "MinCompletionPortThreads": 32 },
    "Kestrel": {
      "MaxRequestBodySize": 30000000,
      "MaxRequestBufferSize": 1048576,
      "MaxRequestHeadersTotalSize": 32768,
      "MaxRequestHeaderCount": 100,
      "RequestHeadersTimeout": "00:00:30",
      "KeepAliveTimeout": "00:02:10",
      "MaxConcurrentConnections": null
    },
    "ForwardedHeaders": { "Enabled": true, "KnownProxies": [], "KnownNetworks": ["10.244.0.0/16"], "ForwardLimit": 1 }
  }
}
```

- **The port** is `Urls` (or `ASPNETCORE_URLS`, which wins): a specific port in development and Compose, `http://+:8080` in a pod. `Kestrel:Limits` of the settings is **not** read by ASP.NET Core; use `Hosting:Kestrel`.
- A Kestrel value that is not set keeps Kestrel's default (the values above are the defaults); a body limit of `0` is no limit.
- `ShutdownTimeoutSeconds` has to be shorter than the `terminationGracePeriodSeconds` of the pod.
- **Forwarded headers** are believed only from the listed proxies or networks. With both lists empty only a proxy on the same machine is believed.

## Security headers: `UseBaseSecurityHeaders()`

`app.UseBaseSecurityHeaders()` (after `UseBaseForwardedHeaders()`, so the scheme is the one the browser used) writes the headers that make a browser safer with every answer, from `Hosting:SecurityHeaders` ([ADR-0139](adr/0139-the-host-writes-security-headers-and-names-the-origins-that-may-call-it.md)); a header the answer already has is left alone, and an empty text leaves a header out:

```json
"Hosting": { "SecurityHeaders": {
  "Enabled": true, "FrameOptions": "DENY", "ReferrerPolicy": "no-referrer",
  "HstsMaxAgeSeconds": 31536000, "HstsIncludeSubDomains": false,
  "ContentSecurityPolicy": "", "PermissionsPolicy": ""
} }
```

`X-Content-Type-Options: nosniff` is always written. **`Strict-Transport-Security` is only written on an https request and outside Development** (0 for none). A host with pages sets `ContentSecurityPolicy` to what its pages load; a web api needs none.

## Which browsers may call: `Hosting:Cors` and `UseBaseCors()`

By default **no other origin may call** the host. Name the origins and `AddBaseHosting` registers the policy; `app.UseBaseCors()` (after the routing, before the authentication) applies it (nothing is done when no origin is listed):

```json
"Hosting": { "Cors": {
  "AllowedOrigins": [ "https://backoffice.example.com" ],
  "AllowedMethods": [ "GET", "POST", "PUT", "PATCH", "DELETE" ],
  "AllowedHeaders": [ "Authorization", "Content-Type", "X-Correlation-Id", "Accept-Language" ],
  "ExposedHeaders": [ "X-Correlation-Id", "Content-Disposition" ],
  "AllowCredentials": false, "PreflightMaxAgeSeconds": 600
} }
```

An origin is a scheme, a host and a port with no path; `*` is any origin and **cannot go with `AllowCredentials`**. A setting that cannot be right stops the start with an error that names it.

## Who this instance is: `AppInfo` and `GET /`

`AddBaseHosting` makes an `AppInfo` (service, instance, environment, version, start) and registers it; `MapBaseInfo()` answers it at `GET /` for anyone, as JSON:

```json
{ "name": "Abc.OrderService.Http.Host", "id": "abc-order-7d9f-x2k", "environment": "uat", "version": "2026.10.41", "startedAt": "2026-10-31T20:00:00+00:00" }
```

`name` is `Service:Name` (else the name of the application), `id` is `APP_ID`, else `POD_NAME`, else `HOSTNAME` (a pod sets `POD_NAME` from its own name), `version` is `BUILD_NUMBER` (else the version of the assembly).

## The thread pool

`Hosting:ThreadPool` raises the minimum threads the pool has ready (`MinWorkerThreads`, `MinCompletionPortThreads`); a value lower than the one in force is ignored and the maximum is never touched.

## The `.env` file

`AddBaseHosting` reads `.env` from the current folder, then from the folder of the application. It is below the environment variables and above the settings files (`appsettings*.json`), so a key the settings file has as an empty placeholder is filled by the `.env`; a variable of the platform always wins. A line is `KEY=value` (`export` and quotes
are accepted, `#` starts a comment); `Scheduler__ApiKey` means `Scheduler:ApiKey`. Never commit one with real values: it is for a developer's machine and a mounted secret.

## A start that fails says why

`RunBaseAsync` builds the host, checks the settings named with `RequireBaseSettings` (one error with every missing key, never a value), runs the host, and logs a failure of any of these as **critical** A host that fails while it starts (a setting checked at start with `ValidateOnStart`, a hosted service that throws) has disposed its services, loggers included, so that reason is written to the standard error and the call still returns 1.
once, flushes the logs and returns the exit code 1, so the platform restarts the pod and the reason is in the logs.
