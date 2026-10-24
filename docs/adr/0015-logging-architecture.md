# ADR-0015: Everything logs through `ILogger`; a provider decides what a log is and where it goes

- **Status:** Accepted
- **Date:** 2026-10-24 (supersedes the structured-logger design of the first rewrite)

## Context

The previous framework gave developers its own logger interfaces (`IAppLogger`, `IFrameworkLogger`, `LogHelper`, `FrameworkLogModel`) and persisted only what was written through them. That forced
every library to depend on those interfaces, made third-party and platform logs invisible in Graylog, and meant a developer's choice of method decided where the log went. Once the log type and the fields
were decided centrally (ADR-0016, ADR-0019), the interfaces had nothing left to decide.

## Decision

- **All code writes with `Microsoft.Extensions.Logging`**: `ILogger<T>` and `[LoggerMessage]` in the framework, `ILogger<T>` (or the `Logger` of the base controller) in the services. There is **no framework logger interface
  for developers**: `IFrameworkLogger`, `LogHelper`, `FrameworkLogModel` and `IPersistentLog` were removed. What a developer chooses on a log line is the level, the message and its arguments, the category (the class),
  and optionally a `Facility` (`logger.BeginFacility("ORDER_IMPORTED")`, a scope, in `HsnSoft.Base.Core`).
- Two packages (both optional): **`HsnSoft.Base.Logging`** is independent of any logging library (the settings every provider reads, `IAppConsoleLogger`, masking) and **`HsnSoft.Base.Logging.Serilog`** is the provider
  (`AddBaseSerilog`). Another provider (NLog) would read the same options and apply the same rules (see [ADR-0003](0003-abstractions-separate-from-providers.md)).
- `IAppConsoleLogger` stays for what is only worth seeing in the console: its category is `ConsoleOnly.<name>` and the provider never keeps it.
- The provider decides, for every log: its **type** ([ADR-0016](0016-log-types-by-namespace.md)), its **places and levels** ([ADR-0017](0017-two-places-two-levels.md)) and its **fields** ([ADR-0019](0019-log-fields-pipeline.md)).
- Common fields on every log: `Solution` (first part of the assembly name), `Assembly`, `Version` (the build: the environment variable `BUILD_NUMBER`, which the CI of the services sets to the image tag, else
  `PersistentLogger:Version`, else the assembly version, so a line number is read in the code that ran), `LoggerName`, `LogType`, `MessageType` (`Structured` when an object was logged with `{@Object}`, `Args` otherwise),
  `SourceContext`, and `Facility` when known (the name of a `[LoggerMessage]` event written in capitals, or `BeginFacility`).
- Serilog swallows the errors of its sinks on purpose (an unreachable Graylog must not break the application); `PersistentLogger:IsSelfLogActive` writes them to the error output. The Graylog sink is rebuilt on a schedule
  (`SinkRefreshIntervalMinutes`) because a third-party sink can stay broken after a failed start.

## Consequences

- Any library's logs, ours and third-party, flow through the same pipeline; nobody has to adopt an interface.
- A product that logged through `IFrameworkLogger` must move to `ILogger` (listed in the migration table of `docs/Logging.md`).
- The type and the fields of a log depend on **where it is written from** (the namespace), not on a method; see ADR-0016.
- A log below both thresholds is still created and dropped by the sinks: Microsoft.Extensions.Logging's own filter is opened wide (ADR-0017). The cost is small.

## Related

[ADR-0016](0016-log-types-by-namespace.md), [ADR-0017](0017-two-places-two-levels.md), [ADR-0019](0019-log-fields-pipeline.md), `docs/Logging.md`.
