# ADR-0017: The console and what is kept have separate levels

- **Status:** Accepted
- **Date:** 2026-10-25 (supersedes a single `FrameworkLogger:LogLevel` that set the console)

## Context

A production pod's console showed far too much, and `Logging:LogLevel` did nothing: the logging builder's minimum level was opened to Trace so Serilog could decide, and the console level came from the framework's own
section. People expect the standard .NET `Logging:LogLevel` to limit what the pod prints, and Graylog (or a file) needs its own, independent level.

## Decision

- **The console** (the pod's log) is told by the standard **`Logging:LogLevel`**: a `Default` and a level per category (`"Microsoft.AspNetCore": "Warning"`, `None` to silence). A rule counts for the namespace and everything
  below it; the longest rule that fits wins. Default Information. Only `Logging:LogLevel` is read (not provider sections like `Logging:Console:LogLevel`).
- **What is kept** (Graylog, or a file) is told by the **`PersistentLogger`** section (renamed from `FrameworkLogger`): `LogLevel` (default Information), `LevelOverrides` per namespace, `PlatformMinimumLevel`
  (default Warning), `IsGrayLogActive` (`true`: Graylog; `false`: the file `FilePath`; never both), `IsSelfLogActive` (the logging library's own errors to the error output; it changes nothing about which logs are written).
- **The persistent level is never below Information**: Debug and Trace (Verbose) are never kept, whatever a rule says. They can be in the console.
- **Platform logs** (ADR-0016) are kept only from `PlatformMinimumLevel` (Warning): their Information, Debug and Verbose stay in the console. A namespace named in `LevelOverrides` is not held to it (to see
  a library's Information in Graylog, `"MongoDB": "Information"`). `PersistentLevelRules` and `LogLevelRules` (in `HsnSoft.Base.Logging`) are the rules, so another provider applies the same.
- **`ConsoleOnly.*` (`IAppConsoleLogger`) is never kept.**
- The two places never look at each other: a log can be shown and not kept, kept and not shown, both, or neither. Microsoft.Extensions.Logging's own filter is opened wide by `AddBaseSerilog`: it would otherwise decide for both.
- A sink a product adds itself (the `configure` of `AddBaseSerilog`) is not held to these levels.

## Consequences

- **Breaking for existing services:** the `FrameworkLogger` section is no longer read. `FrameworkLogger:LogLevel` (the console) became `Logging:LogLevel`; `PersistentMinimumLevel` became `PersistentLogger:LogLevel`;
  the environment variable is `PersistentLogger__IsGrayLogActive`. The old "libraries one step quieter" defaults were removed; the console rules do that job and the platform's persistent level is `PlatformMinimumLevel`.
- A pod can print only warnings while Graylog keeps Information, and the other way round.
- `PersistentLogger:LogLevel: Warning` also stops keeping the request and successful response logs (Information), by design.
- Levels are read when the service starts.
- What each setting does, with settings for the usual cases, is in `docs/LogLevels.md`.

## Related

[ADR-0015](0015-logging-architecture.md), [ADR-0016](0016-log-types-by-namespace.md), `docs/LogLevels.md`, `docs/Logging.md` (Using it), `LogLevelRules`, `PersistentLevelRules`, `PersistentLoggerOptions`.
