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

## Who this instance is: `AppInfo` and `GET /`

`AddBaseHosting` makes an `AppInfo` (service, instance, environment, version, start) and registers it; `MapBaseInfo()` answers it at `GET /` for anyone, as JSON:

```json
{ "name": "Abc.OrderService.Http.Host", "id": "abc-order-7d9f-x2k", "environment": "uat", "version": "2026.10.41", "startedAt": "2026-10-31T20:00:00+00:00" }
```

`name` is `Service:Name` (else the name of the application), `id` is `APP_ID`, else `POD_NAME`, else `HOSTNAME` (a pod sets `POD_NAME` from its own name), `version` is `BUILD_NUMBER` (else the version of the assembly).

## The thread pool

`Hosting:ThreadPool` raises the minimum threads the pool has ready (`MinWorkerThreads`, `MinCompletionPortThreads`); a value lower than the one in force is ignored and the maximum is never touched.

## The `.env` file

`AddBaseHosting` reads `.env` from the current folder, then from the folder of the application. It is below the environment variables and above the settings files. A line is `KEY=value` (`export` and quotes
are accepted, `#` starts a comment); `Scheduler__ApiKey` means `Scheduler:ApiKey`. Never commit one with real values: it is for a developer's machine and a mounted secret.

## A start that fails says why

`RunBaseAsync` builds the host, checks the settings named with `RequireBaseSettings` (one error with every missing key, never a value), runs the host, and logs a failure of any of these as **critical**
once, flushes the logs and returns the exit code 1, so the platform restarts the pod and the reason is in the logs.
