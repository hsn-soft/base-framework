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

## The log types

Every log in Graylog has a `Solution` (the first part of the assembly name: `Hhs`, `Xyz`), an `Assembly` (`Hhs.AuthServer.Http.Host`: which
service, auth server or agent wrote it) and a `LogType`:

| `LogType` | What it is | Written by |
|---|---|---|
| `FrameworkLog` | a framework library (`HsnSoft.Base.*`) talking about itself; the `Facility` says what: `RABBITMQ_CONSUMER_SUBSCRIBED`, `PRODUCE_EVENT_ERROR`, ... | the library, nobody has to do anything |
| `HttpRequest`, `HttpResponse` | an HTTP request when it arrives, and when it is answered | `UseBaseRequestLogging()` ([`HsnSoft.Base.AspNetCore.WebApi`]) |
| `ConsumeStart`, `ConsumeEnd` | a message when its handler starts, and when it ends | the event bus |
| `AppLog` | what the developer of the application logs with `ILogger` | the application |
| `PlatformLog` | ASP.NET Core, Entity Framework, a database driver, ... | the platform |

The provider decides `LogType` from the category of a log (`HsnSoft.Base.*` is `FrameworkLog`, `System`, `Microsoft`, `Npgsql`,
`MongoDB`, ... are `PlatformLog`, the rest is `AppLog`); the four chain types say it themselves. `MessageType` is `Structured` when an
object was logged with `{@Object}` and `Args` when only plain values were.

### The chain logs

A request that publishes an event whose handler publishes another event is one chain. The four chain logs are written in pairs so a
log reader can follow it, and so a pod that dies in the middle (out of memory) leaves its start without an end:

| Field | Meaning |
|---|---|
| `CorrelationId` | the same along the whole chain, however many hops. An HTTP request takes it from `X-Correlation-Id` or starts a chain; the response carries it back. |
| `ScopeId` | the same in the two logs of one step (`HttpRequest` + `HttpResponse`, `ConsumeStart` + `ConsumeEnd`), different for every step. Every other log written during the step carries it too. |
| `From`, `To` | `httpclient -> token-login` for a request (`controller-action` in lower case; a minimal API endpoint is its name, or `method-route`). A message: `token-login -> ordercreated` (where it was published from, the event) and, one hop later, `ordercreated -> stockreserved`. A message published outside any request or message (an agent, a scheduler) comes from `agent`. |
| `ElapsedMs` | how long the step took (`HttpResponse`, `ConsumeEnd`) |
| `Facility` | `HTTP_REQUEST`, `HTTP_RESPONSE`, `CONSUME_EVENT_START`, `CONSUME_EVENT_SUCCESS` / `CONSUME_EVENT_ERROR` (the end), `CONSUME_EVENT_RETRY` |

`ConsumeStart`/`ConsumeEnd` also carry `HopLevel`, `MessageId`, `ParentMessageId`, `EventName` and `Producer`; the response log has `Status`
(the level follows it: 4xx Warning, 5xx and exceptions Error) and the user. The message id is the id of the broker-independent
envelope, the same for RabbitMQ and Kafka. Where a message comes from travels in its `Flow` header; a message from an older producer
(without it) comes from its `Producer`.

Request and response bodies are logged only with `RequestLogging:LogBodies` and an `ILogMasker` (`AddBaseLogging()`): masked, JSON only,
cut to `MaxBodyLength`. Headers are written as one JSON text; `Authorization` and cookies are summarized, never written.

### A log has a fixed set of fields

Graylog turns every property of a log into a field, and an object logged with `{@Object}` into a field per property. An object whose
shape is the developer's would create fields without end, so the persistent sink writes an object (and a dictionary, and a list) as
the **one field of its name holding its JSON**, and cuts every text to `FrameworkLogger:MaxPersistentFieldLength` (8192). The console
still shows the readable object. A body with two hundred properties is one field.

## Facility: what a log is about

`Facility` is a top-level field, so `Facility:CONSUME_EVENT_ERROR` finds every handler that failed, and the facilities of a chain in
order tell the story. A log gets it from the name of its event (`[LoggerMessage(EventName = "CONSUME_EVENT_SUCCESS")]`; only names
written in capitals count, the platform's own `ExecutedEndpoint` is not a facility) or from the `Facility` of a structured log written
with `IFrameworkLogger` (`LogHelper.Generate(message, reference, facility: "ORDER_IMPORTED", correlationId)`). A framework library
gives every log an event name: use `RABBITMQ_CONSUMER_SUBSCRIBED`-style names for new ones.

## What the framework packages log

Categories are the class names (`HsnSoft.Base.EventBus.RabbitMQ.RabbitMqConsumer`), so a category prefix controls a whole package
(`"LevelOverrides": { "HsnSoft.Base.EventBus": "Warning" }`). The event bus logs with event names (`PRODUCE_EVENT_ERROR`,
`CONSUME_EVENT_SUCCESS`, ...) and puts `CorrelationId`, `MessageId` and `EventName` in the log scope, which Serilog turns into
properties (Graylog fields).

The RabbitMQ client library does not use `ILogger`, it only writes to an `EventSource` (`rabbitmq-client`). The RabbitMQ package
listens to it while the host runs and passes it on under the category `HsnSoft.Base.EventBus.RabbitMQ.Client`: its information as
Debug (console only), its warnings and errors as they are, so a connection the client gives up on shows up in Graylog.

The message body is not logged unless `EventBusOptions.LogMessageBody` is `true` (see [EventBus.md](EventBus.md)): a message may carry
personal data, and the console is a log too.

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
