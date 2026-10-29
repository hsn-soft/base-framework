# ADR-0016: The log type is told by the namespace of the class that wrote it

- **Status:** Accepted
- **Date:** 2026-10-25

## Context

Graylog needs to tell apart what the framework says, what the application says, what the platform says and the logs that follow a request or a message. The first rule listed the platform libraries (`System`, `Microsoft`,
`Npgsql`, `Yarp`, ...) and called the rest the application's. The list of platforms is open-ended (every new library is one more) while the application's namespace is one thing the product knows. We also decided that a
product's shared library is neither the framework nor a service.

## Decision

`LogType` is told by the **category** (the class that wrote the log, `SourceContext`):

| Category starts with | `LogType` |
|---|---|
| `HsnSoft.Base` | `FrameworkLog` (a framework library talking about itself) |
| `{AppNamespace}.Shared` (`Hhs.Shared.*`) | `SharedLog` (the product's shared library) |
| `{AppNamespace}` (`Hhs.*`: a gateway, an auth server, a microservice) | `AppLog` |
| anything else | `PlatformLog` (ASP.NET Core, Entity Framework, a driver, **any library**) |

- A prefix matches a whole name part: `Hhs` is `Hhs` and everything under it, not `HhsExtras`; `Hhs.Shared` is `SharedLog`, `Hhs.Checkout.Shared` is `AppLog`. `Shared` is fixed; `Hhs` comes from the settings.
- **`PersistentLogger:AppNamespace`** gives the application's namespace. Without it, it is the first part of the name of the assembly that runs (`Hhs.IdentityService.Http.Host` is `Hhs`), which is right where a product's code
  has one root namespace. Set it when it does not.
- A log without a category (the static `Log`) and the console logger's (`ConsoleOnly.*`) are the application's.
- The request and message logs (`HttpRequest`, `HttpResponse`, `HttpRequestResponse`, `ConsumeStart`, `ConsumeEnd`) say their own type: the code that writes them puts it there ([ADR-0018](0018-chain-logs.md)). A developer
  cannot choose a type on a log line.
- **A product's shared library is not the framework**: `FrameworkLog` is only `HsnSoft.Base.*`.

## Consequences

- There is no list of platform libraries to keep up to date: a new library is the platform's the moment it logs.
- A product whose code lives outside `{AppNamespace}` (a different root namespace) sets `AppNamespace`, or its logs are `PlatformLog`s and are held to the platform's higher level (ADR-0017).
- The type decides small things: the fields a log keeps (ADR-0019) and the level the platform's logs need to be kept (ADR-0017). `SharedLog` and `AppLog` are treated the same there.
- A category that is not a class name (`CreateLogger("Startup")`) is not under the application's namespace and is the platform's; use `ILogger<T>`.

## Related

[ADR-0005](0005-where-code-lives.md), [ADR-0017](0017-two-places-two-levels.md), [ADR-0019](0019-log-fields-pipeline.md), `LogShapeEnricher`, `docs/Logging.md` (The log types).
