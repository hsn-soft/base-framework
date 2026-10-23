# Logging

The framework packages (EventBus, RabbitMQ, Kafka, PostgreSQL, MongoDB, ...) do not know which logging library a product uses. They
write through `Microsoft.Extensions.Logging` (`ILogger<T>`, `[LoggerMessage]`), and the product decides where the logs go: Serilog and
Graylog in one service, NLog in another. Two packages help with that; both are optional.

| Package | What it is |
|---|---|
| `HsnSoft.Base.Logging` | Independent of any logging library: `IAppConsoleLogger`, `IFrameworkLogger`, `LogHelper`, the settings every provider reads (`FrameworkLogger` section), masking of sensitive data. |
| `HsnSoft.Base.Logging.Serilog` | Serilog as the provider: the console, the persistent sink (Graylog, or a file), the routing rule below, the self-healing Graylog sink. |

## The rule: Debug and Verbose stay in the console

| Log | Console | Persistent sink (Graylog, or a file) |
|---|---|---|
| Debug, Trace (Serilog: Verbose) | yes | **never** |
| Information and above | yes | yes |
| Anything written through `IAppConsoleLogger` | yes | **never** (it is the console logger) |
| Anything below `FrameworkLogger:LogLevel` | no | no |

The persistent level can be raised (`FrameworkLogger:PersistentMinimumLevel`, e.g. `Warning`) but never lowered below Information.
The rule lives in the logging provider, not in the framework packages: they only write logs and do not know what is persistent.
A provider recognizes console-only logs by their **category** (`LogCategories.ConsoleOnlyPrefix`), the one thing every logging
library has, so another provider can apply the same rule.

## Using it (Serilog)

```csharp
builder.Services.AddBaseLogging();                                   // IAppConsoleLogger, IFrameworkLogger, ILogMasker
builder.Logging.AddBaseSerilog(builder.Configuration, "OrderService"); // everything that logs through ILogger goes through Serilog
```

```json
"FrameworkLogger": {
  "LogLevel": "Debug",
  "PersistentMinimumLevel": "Information",
  "IsGrayLogActive": true,
  "GrayLog": { "Address": "http://graylog", "Port": 12201, "SinkRefreshIntervalMinutes": 5 },
  "FilePath": "Logs/logs.txt",
  "IsSelfLogActive": false,
  "LevelOverrides": { "Microsoft.AspNetCore": "Error" }
}
```

- Without Graylog the persistent sink is the file (`FilePath`, rolling daily, 30 days).
- Libraries are quieter than the application, as before: `System`, `Microsoft*`, `Npgsql`, `MongoDB` and the HsnSoft EF Core and
  MongoDB packages log one level up from the configured level (Warning once that is Information); `LevelOverrides` changes or adds.
- `AddBaseSerilog` also removes the other logging providers, lifts the default filter of `Microsoft.Extensions.Logging` (it would hide
  Debug logs before Serilog sees them) and, unless you pass `replaceGlobalLogger: false`, assigns Serilog's static `Log.Logger`.
- Objects logged with `{@Object}` have their `[SensitiveData]` properties masked.
- The Graylog sink is rebuilt every `SinkRefreshIntervalMinutes` (a third-party sink can stay broken after a failed start); the
  replaced sink lives on for ten seconds so what it was sending is not lost.
- `IsSelfLogActive` prints the errors of the logging library itself (e.g. Graylog not reachable) to the error output, never to a sink:
  a failing sink must not log into itself.
- A product that logs Newtonsoft objects adds the destructuring itself, the framework does not depend on Newtonsoft:
  `builder.Logging.AddBaseSerilog(configuration, "Host", serilog => serilog.Destructure.JsonNetTypes())`.

`LogLevel` values may be Microsoft's (`Trace`, `Critical`) or Serilog's (`Verbose`, `Fatal`), in any case.

## What the framework packages log

Categories are the class names (`HsnSoft.Base.EventBus.RabbitMQ.RabbitMqConsumer`), so a category prefix controls a whole package
(`"LevelOverrides": { "HsnSoft.Base.EventBus": "Warning" }`). The event bus logs with event names (`PRODUCE_EVENT_ERROR`,
`CONSUME_EVENT_SUCCESS`, ...) and puts `CorrelationId`, `MessageId` and `EventName` in the log scope, which Serilog turns into
properties (Graylog fields).

The RabbitMQ consumer logs the whole message body at Debug, as before. Debug is never persistent, so it stays in the console; keep
it that way if messages carry personal data.

## Another provider (NLog, ...)

A provider has to do what `HsnSoft.Base.Logging.Serilog` does, with its own tools: read `FrameworkLoggerOptions.FromConfiguration`,
write everything to the console, keep Debug/Trace and the `ConsoleOnly.` categories out of the persistent target, and mask
`[SensitiveData]` properties. In NLog that is a rule order, sketched here (not run in this repository):

```xml
<rules>
  <logger name="*" minlevel="Trace" writeTo="console" />
  <logger name="ConsoleOnly.*" final="true" />
  <logger name="*" minlevel="Info" writeTo="graylog" />
</rules>
```

## Moving from the previous framework

| Previous | Now |
|---|---|
| `HsnSoft.Base.Serilog` (`SerilogConfigurationHelper`, `AppLogger`, `FrameworkLogger`) | `HsnSoft.Base.Logging.Serilog`: `AddBaseSerilog(...)`; the loggers are `AppConsoleLogger` and `FrameworkLogger` of `HsnSoft.Base.Logging`, on `ILogger` |
| `IAppConsoleLogger`, `IFrameworkLogger`, `LogHelper`, `FrameworkLogModel` in `HsnSoft.Base.Core` | the same names and namespaces (`HsnSoft.Base.Logging...`), in `HsnSoft.Base.Logging` |
| `new AppLogger(configuration, "ThreadPool")` | `new AppConsoleLogger(loggerFactory, "ThreadPool")` |
| Persistent logs: only what was written with `IFrameworkLogger` (`IsPersistentLogger`) | by level: everything of Information and above except what `IAppConsoleLogger` wrote; ASP.NET Core, EF Core and the HsnSoft packages now reach Graylog from Information (libraries from Warning) |
| `IRefreshableLogger`, `PersistentLoggerSinkRefreshWorker` in Shared | built in: a hosted service refreshes the Graylog sink |
| `HsnSoft.Base.Serilog.Mask.SensitiveDataAttribute` | `HsnSoft.Base.Logging.Masking.SensitiveDataAttribute` (a property now, usable the same way) |
| `FrameworkLogger:*` settings | the same keys; new: `PersistentMinimumLevel`, `LevelOverrides` |
| `IEventBusLogger` and its persisted log models | `ILogger` (see above) |
| `HsnSoft.Base.Json.*` | not ported: no product uses `IJsonSerializer`; they use Newtonsoft or `System.Text.Json` directly |
