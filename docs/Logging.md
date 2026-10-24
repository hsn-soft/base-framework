# Logging

The framework packages (EventBus, RabbitMQ, Kafka, PostgreSQL, MongoDB, ...) do not know which logging library a product uses. They
write through `Microsoft.Extensions.Logging` (`ILogger<T>`, `[LoggerMessage]`), and the product decides where the logs go: Serilog and
Graylog in one service, NLog in another. Two packages help with that; both are optional.

| Package | What it is |
|---|---|
| `HsnSoft.Base.Logging` | Independent of any logging library: `IAppConsoleLogger` (console only), the settings every provider reads (`FrameworkLogger` section), masking of sensitive data. |
| `HsnSoft.Base.Logging.Serilog` | Serilog as the provider: the console, the persistent sink (Graylog, or a file), the routing rule below, the self-healing Graylog sink. |

## The rule: Debug and Verbose stay in the console

| Log | Console | Persistent sink (Graylog, or a file) |
|---|---|---|
| Debug, Trace (Serilog: Verbose) | yes | **never** |
| Information and above | yes | yes |
| Platform logs (`PlatformLog`: ASP.NET Core, Entity Framework, YARP, the drivers) below Warning | yes | **no** (`FrameworkLogger:PlatformMinimumLevel`, default `Warning`) |
| Anything written through `IAppConsoleLogger` | yes | **never** (it is the console logger) |
| Anything below `FrameworkLogger:LogLevel` | no | no |

The persistent level can be raised (`FrameworkLogger:PersistentMinimumLevel`, e.g. `Warning`) but never lowered below Information.
The rule lives in the logging provider, not in the framework packages: they only write logs and do not know what is persistent.
A provider recognizes console-only logs by their **category** (`LogCategories.ConsoleOnlyPrefix`), the one thing every logging
library has, so another provider can apply the same rule.

## Using it (Serilog)

```csharp
builder.Services.AddBaseLogging();                                   // IAppConsoleLogger, ILogMasker
builder.Logging.AddBaseSerilog(builder.Configuration, "OrderService"); // everything that logs through ILogger goes through Serilog
```

```json
"FrameworkLogger": {
  "LogLevel": "Debug",
  "PersistentMinimumLevel": "Information",
  "PlatformMinimumLevel": "Warning",
  "AppNamespace": "Hhs",
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
service, auth server or agent wrote it), a `Version` (the build that wrote it: the environment variable `BUILD_NUMBER`, which the CI of the
services sets to the image tag `{yyMMddHHmm}-sha-{git short sha}`; else `FrameworkLogger:Version`, else the version of the entry assembly: with it the
`Line` of a failure is read in the code that ran) and a `LogType`:

| `LogType` | What it is | Written by |
|---|---|---|
| `FrameworkLog` | a framework library (`HsnSoft.Base.*`) talking about itself; the `Facility` says what: `RABBITMQ_CONSUMER_SUBSCRIBED`, `PRODUCE_EVENT_ERROR`, ... | the library, nobody has to do anything |
| `HttpRequest`, `HttpResponse` | an HTTP request when it arrives, and when it is answered | `UseBaseRequestLogging()` ([`HsnSoft.Base.AspNetCore.WebApi`]) |
| `ConsumeStart`, `ConsumeEnd` | a message when its handler starts, and when it ends | the event bus |
| `SharedLog` | the shared library of the product (`{AppNamespace}.Shared.*`: its rate limit, its helpers) | the shared library |
| `AppLog` | what the developer of the application logs with `ILogger` | the application |
| `PlatformLog` | ASP.NET Core, Entity Framework, a database driver, ... | the platform |

The provider decides `LogType` from the category of a log, that is from the class that wrote it: what is under **`HsnSoft.Base`** is `FrameworkLog`, what is under
**the namespace of the application** (`FrameworkLogger:AppNamespace`, `Hhs`) is `AppLog`, except what is under **`{AppNamespace}.Shared`** (`Hhs.Shared.*`), which is `SharedLog`, and **everything else is `PlatformLog`**: no list of libraries
to keep up to date, a new library is the platform's the moment it logs. Without `AppNamespace` it is the first part of the name of the assembly that runs
(`Hhs.IdentityService.Http.Host` is `Hhs`); set it when the code of the product does not start with that. A log written without a category (the static `Log`) and the
console logger's are the application's. The chain types say theirs themselves. `MessageType` is `Structured` when an
object was logged with `{@Object}` and `Args` when only plain values were.

What is a framework library is only `HsnSoft.Base.*`. The shared library of a product (`Xyz.Shared`) is not the framework's: its logs are `SharedLog`s, told from the logs of the services (`AppLog`) so what a shared policy says can be found on its own. Both are the product's code and have the same fields: who asked, the facility and the arguments, without the endpoint and the span of ASP.NET Core.

### The chain logs

Fields ASP.NET Core already adds to every log of a request are used as they are and not written twice: `RequestId` (the key of one request),
`RequestPath`, `ConnectionId`, and the W3C trace (`TraceId`, `SpanId`, `ParentId`, which a gateway passes on in `traceparent`).

A request that publishes an event whose handler publishes another event is one chain. The four chain logs are written in pairs so a
log reader can follow it, and so a pod that dies in the middle (out of memory) leaves its start without an end:

| Field | Meaning |
|---|---|
| `CorrelationId` | the same along the whole chain, however many hops. A client (a mobile app) that sends `X-Correlation-Id` has its own id kept, so it finds its whole chain in the logs; a request without one takes the `TraceId` of the request (32 hex digits), not a new id: the same value is the `TraceId` of the logs of the gateway and of the services, and of the consume logs (a message carries only the correlation id, so a consume log gets `TraceId` when it is one). The response carries it back. |
| `ScopeId` | also 32 hex digits; the same in the start and end log of one message (`ConsumeStart` + `ConsumeEnd`), different for every message; every other log written while the message is handled carries it too. An HTTP request has no `ScopeId`: ASP.NET Core puts its `RequestId` on every log of the request, and `HttpRequest` + `HttpResponse` (and the exception log) share it. |
| `From`, `To` | `httpclient -> token-login` for a request (`controller-action` in lower case; for anything that is not a controller action, such as a minimal API endpoint or a proxied route, the request path in lower case). A message: `token-login -> ordercreated` (where it was published from, the event) and, one hop later, `ordercreated -> stockreserved`. A message published outside any request or message (an agent, a scheduler) comes from `agent`. |
| `ElapsedMs` | how long the step took (`HttpResponse`, `ConsumeEnd`) |
| `Facility` | `HTTP_REQUEST`, `HTTP_RESPONSE`, `CONSUME_EVENT_START`, `CONSUME_EVENT_SUCCESS` / `CONSUME_EVENT_ERROR` (the end), `CONSUME_EVENT_RETRY` |

`ConsumeStart`/`ConsumeEnd` also carry `HopLevel`, `MessageId`, `ParentMessageId`, `EventName` and `Producer`; the response log has `Status`
(the level follows it, wherever it is logged: 2xx and 3xx Information, 4xx Warning, 5xx Error) and the user. The message id is the id of the broker-independent
envelope, the same for RabbitMQ and Kafka. Where a message comes from travels in its `Flow` header; a message from an older producer
(without it) comes from its `Producer`.

Request and response bodies are logged only with `RequestLogging:LogBodies` and an `ILogMasker` (`AddBaseLogging()`): masked, JSON only,
cut to `MaxBodyLength`. Headers are written as one JSON text; `Authorization` and cookies are summarized, never written.

The request log also says who asked: `ClientIp`, `ForwardedFor`, `UserAgent`, `AcceptLanguage`, `Origin` (the host of the `Origin`, else the `Referer`,
else the `From` header), `Referer`, `Host`, `ContentType`, `RequestBytes` (the size the request says it has: 0 without a body, absent when the body has a size nobody knows beforehand, such as a chunked upload or a gRPC stream). What only a product knows (the channel and version of the client, the
device, the roles, a location) is added with an `IRequestLogEnricher`, called for both logs.

### Who writes the request and response logs

In a system with a gateway, the **services** write them (`HttpRequest` when a request arrives, `HttpResponse` when it is answered: they know the
action, the user, the exception, and the classes of the bodies, so their `[SensitiveData]` masks them exactly). Every answer of a service that
logged carries the header `X-Logged-By`. The **gateway** (`"RequestLogging": { "Role": "Gateway" }`) stays quiet for those and removes the
header; for every request it answered **itself**, which no service saw or logged (a rejected token, a rate limit, an unknown route, a service that
cannot be reached or died), it writes one `HttpRequestResponse` with the whole request and its answer, its level the status of the answer.
When a service does not log (`Enabled: false`, it is down) the gateway logs: a log twice is better than none.

If the answer of the gateway is a 5xx (the service was unreachable, or died on this request: out of memory, a restart) the log also has the
**body of the request**, masked, so what was asked is known. For that a gateway keeps the JSON bodies it forwards (up to `MaxBodyReadBytes`)
in a buffer when `LogBodies` is on, and only writes them when the answer is a 5xx; a rejected request (4xx) does not write its body. The gateway
does not have the classes of the services: it masks by the usual key names and by the contract assemblies given to `AddBaseLogging(...)`.

The chain does not depend on any of this: the correlation id is set and passed on in every role. A service that dies in the middle of a request
leaves its `HttpRequest` without an `HttpResponse`, and the gateway's `HttpRequestResponse` with 502 or 504.

### The settings of the request log (section `RequestLogging`)

| Setting | Default | |
|---|---|---|
| `Role` | `Service` | `Service` or `Gateway` (see above) |
| `Enabled` | `true` | off: a service logs nothing (the gateway logs what it did not) |
| `LogHealthChecks`, `HealthCheckPaths` | `false`; `/health`, `/StartupCheck`, `/LivenessCheck`, `/ReadinessCheck` | health probes are not logged unless on |
| `ExcludedPaths` | `/openapi`, `/swagger` | prefixes that are never logged |
| `LogBodies` | `false` | JSON bodies, masked; needs an `ILogMasker` |
| `MaxRequestBodyLength`, `MaxResponseBodyLength` | 8192 each | longer bodies are cut (the cut is marked `...(+N)`) |
| `MaxBodyReadBytes` | 256 KB | a bigger body is not read for the log |
| `MaxHeaderValueLength` | 128 | every header value, and `UserAgent`, `Origin`, `Referer`, `ForwardedFor`, the client fields |
| `MaxHeadersLength` | 4096 | all the headers together (one field) |
| `MaxUrlLength` | 512 | query string and path |
| `ClientChannelHeader`, `ClientVersionHeader`, `ClientLatHeader`, `ClientLongHeader` | `X-Channel`, `X-Client-Version`, `X-Client-Request-Lat`, `X-Client-Request-Long` | the headers a client tells about itself; they become `ClientChannel`, `ClientVersion`, `ClientLat`, `ClientLong` |
| `CorrelationIdHeader`, `LoggedByHeader` | `X-Correlation-Id`, `X-Logged-By` | |

`UserId` and `UserRoles` (the role claims, distinct, comma separated) are in the request log too, when the request is authenticated: authentication runs after the logging middleware in the pipeline, so the middleware asks for it first (the answer is remembered for the request, asking again costs nothing; if it fails the log is simply written without a user).  What a message's headers say about who
caused it (`UserId`, `UserRoles`, `ClientChannel`, `ClientVersion`, `ClientLat`, `ClientLong`, as the production wire always named them) is on the
consume logs too. The device type of the client needs a user-agent parser, so a product adds it with an `IRequestLogEnricher`.

### Files, gRPC and MVC

- **Files never log their body.** A download or an upload is logged with what it is, not what is in it: the response log says
  `..., download report.pdf (1048576 bytes)` or `..., upload a.txt (5 bytes); upload scan.png (20480 bytes)` and carries `Files` (a JSON
  list of `direction`, `name`, `fileName`, `size`, `contentType`), `FileCount` and `FileBytes`. A download is a response with a
  `Content-Disposition` or a binary content type; an upload is a form with files (only when the application has read the form: logging
  never buffers an upload by itself), or a raw upload (`application/octet-stream`, `application/pdf`, ...) named by its
  `Content-Disposition`. `ResponseBytes` is what was really written (a download is measured as it is sent). Response
  bodies are streamed as before, never held back: only a small, uncompressed JSON answer is kept for the log (with `LogBodies`).
- **MVC** applications (views, Razor Pages) work the same: the controller action names the step (`home-index`), an HTML page is text and
  its body is never logged.
- **gRPC** calls are named `service-method` (`/orders.Orders/GetOrder` is `orders-getorder`). A gRPC call answers HTTP 200 whatever
  happened, so the response log judges it by `grpc-status` (header or trailer; Warning for the client's mistakes such as NotFound and
  Unauthenticated, Error for the server's) and says it in the message (`, grpc-status 5 (order not found)`) and in `GrpcStatus`.
  The messages of a stream are counted, not logged.

### What a failure's logs say

Every log of a failure (the `HttpResponse` of a 400 or 500, the exception handler's log, `CONSUME_EVENT_REJECTED`, `CONSUME_EVENT_ERROR`,
`CONSUME_EVENT_RETRY`) has, besides `ExceptionType`:

- `ExceptionIdentifier`: where it was thrown, like `Solution:Xyz|Assembly:Xyz.ServiceOrder.Host|File:OrdersController.cs|Line:92`. It is the
  first place with a source file in the code of the application that runs (the solution of its entry assembly), looking at the innermost exception (the
  cause) first; a throw deep inside a library is reported where the application called it, and when no code of the application has sources, the first
  code that has. It needs the symbols (`.pdb`) next to the assemblies, which `dotnet publish` copies; without them the method is told
  (`...|Method:Type.Method`), and an exception that was never thrown has only its type. It has no length limit.
- `Reason`: the message of the exception and of every exception inside it, down to the one that has none inside, joined with `" >> "`
  (`outer >> middle >> inner`), up to 8192 characters.

For a 500 these two are what to look at; for a 400 (an error the caller made) they are there when they can be told, and not what matters.

### A log has a fixed set of fields

Graylog turns every property of a log into a field, and an object logged with `{@Object}` into a field per property. An object whose
shape is the developer's would create fields without end, so the persistent sink writes an object (and a dictionary, and a list) as
the **one field of its name holding its JSON**, and cuts every text to `FrameworkLogger:MaxPersistentFieldLength` (16384, above the 8192 of a body or a reason, so those are not cut twice). The console
still shows the readable object. A body with two hundred properties is one field.

## Where each decision is made

The code that writes a log (`logger.LogInformation(...)` in a controller, an application service, a repository) decides only the **level**, the **message and its
arguments**, and the **category** (the class: `ILogger<UsersController>`). Everything else is decided once, in the provider, for every log:

| Decision | Where | Decided by |
|---|---|---|
| Which sinks get it (console, file, Graylog) | `BaseSerilog.Create` and `SerilogRouting` (`HsnSoft.Base.Logging.Serilog`) | the **level** and the **category** of the log, with the `FrameworkLogger` section: the console gets everything from `LogLevel`, the persistent sink (Graylog when `IsGrayLogActive`, else the file) from `PersistentMinimumLevel` (never below Information), `PlatformLog`s from `PlatformMinimumLevel`; the category `ConsoleOnly.*` (`IAppConsoleLogger`) never reaches it |
| Its `LogType` | `LogShapeEnricher`, or the writer for the chain types | the **category**: `HsnSoft.Base.*` is `FrameworkLog`, what is under `FrameworkLogger:AppNamespace` is `AppLog` (`{AppNamespace}.Shared.*`: `SharedLog`), everything else is `PlatformLog`. `HttpRequest`, `HttpResponse`, `HttpRequestResponse`, `ConsumeStart` and `ConsumeEnd` are said by the code that writes them (the request logging middleware and the event bus): a developer cannot choose a type on the line of a log |
| Which fields it has | the pipeline of `BaseSerilog.Create`, in this order | below |

The pipeline, for every log: **scopes** (`BeginScope`, what the middleware and the event bus put there: `CorrelationId`, `TraceId`, the `Facility` of
`BeginFacility`) and the arguments of the message become properties; the enrichers add `Solution`, `Assembly`, `Version`, `LoggerName`, `Facility` (from the name of an event
written in capitals), `LogType` and `MessageType`; **`NoiseEnricher` removes** what a type must not have (`ConnectionId` from all, the fields of who asked from `FrameworkLog` and
`PlatformLog`, the endpoint, span and request fields of ASP.NET Core from `AppLog`); a product's own enrichers (the `configure` of `AddBaseSerilog`) run after them; and
for the persistent sink `FieldGuardSink` turns an object into one JSON field, cuts long texts and drops what is only for the message (`EventId`, `MessageDetails`).

### To add or remove a field

- **For one log:** put it in the arguments (`"Order {OrderNo}"`), or in a scope for a group of logs (`BeginScope`, `BeginFacility`).
- **For every log, a new field:** an enricher added in `AddBaseSerilog(configuration, name, configure: serilog => serilog.Enrich.With(new MyEnricher()))`.
- **For every log, take a field away:** the same, with `logEvent.RemovePropertyIfPresent("Name")` in the enricher (it runs after `NoiseEnricher`, so it can remove what the
  framework leaves); what the framework itself must never put on a type is in `NoiseEnricher`.
- **What a type really has now:** [GraylogFields.md](GraylogFields.md), measured from a run. Look at it after adding a package or a field: a field that nobody wrote on
  purpose (it came from a library) shows in the table, in the column of the type that carries it.

## Facility: what a log is about

`Facility` is a top-level field, so `Facility:CONSUME_EVENT_ERROR` finds every handler that failed, and the facilities of a chain in
order tell the story. A log gets it from the name of its event (`[LoggerMessage(EventName = "CONSUME_EVENT_SUCCESS")]`; only names
written in capitals count, the platform's own `ExecutedEndpoint` is not a facility). A framework library
gives every log an event name: use `RABBITMQ_CONSUMER_SUBSCRIBED`-style names for new ones.

The application names the logs of an operation with `logger.BeginFacility("ORDER_IMPORTED")` (a scope: every log written inside it has that `Facility`).
The fields of all the log types are in [GraylogFields.md](GraylogFields.md).

## What the framework packages log

Categories are the class names (`HsnSoft.Base.EventBus.RabbitMQ.RabbitMqConsumer`), so a category prefix controls a whole package
(`"LevelOverrides": { "HsnSoft.Base.EventBus": "Warning" }`). The event bus logs with event names (`PRODUCE_EVENT_ERROR`,
`CONSUME_EVENT_SUCCESS`, ...) and puts `CorrelationId`, `MessageId` and `EventName` in the log scope, which Serilog turns into
properties (Graylog fields).

The RabbitMQ client library does not use `ILogger`, it only writes to an `EventSource` (`rabbitmq-client`). The RabbitMQ package
listens to it while the host runs and passes it on under the category `HsnSoft.Base.EventBus.RabbitMQ.Client`: its information as
Debug (console only), its warnings and errors as they are, so a connection the client gives up on shows up in Graylog.

An `AppLog` (and a `SharedLog`) has what its developer wrote (the arguments as fields, `{@Object}` as one JSON field), the `Facility` (`BeginFacility`), the `CorrelationId` and `TraceId`
of its chain and the category it was written under (`SourceContext`). The endpoint (`ActionId`, `ActionName`), the span (`SpanId`, `ParentId`) and the request
(`RequestId`, `RequestPath`) that ASP.NET Core and the tracing put on every log are removed from it: they are on the `HttpRequest` and `HttpResponse` of the same request.

The `ConsumeStart` log carries the message (envelope and event, masked) as `MessageBody`; see [EventBus.md](EventBus.md). The raw body is not logged unless `EventBusOptions.LogMessageBody` is `true` (see [EventBus.md](EventBus.md)): a message may carry
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
| `HsnSoft.Base.Serilog` (`SerilogConfigurationHelper`, `AppLogger`, `FrameworkLogger`) | `HsnSoft.Base.Logging.Serilog`: `AddBaseSerilog(...)`; the console logger is `AppConsoleLogger` of `HsnSoft.Base.Logging`, on `ILogger`; the application writes with `ILogger` |
| `IAppConsoleLogger` in `HsnSoft.Base.Core` | the same name and namespace (`HsnSoft.Base.Logging...`), in `HsnSoft.Base.Logging` |
| `IFrameworkLogger`, `LogHelper`, `FrameworkLogModel` (structured logs a developer wrote on purpose) | gone: the application writes with `ILogger` (`{@Object}` for an object, `BeginFacility` for the `Facility`), and the log type and its fields are decided by the provider |
| `new AppLogger(configuration, "ThreadPool")` | `new AppConsoleLogger(loggerFactory, "ThreadPool")` |
| Persistent logs: only what was written with `IFrameworkLogger` (`IsPersistentLogger`) | by level: everything of Information and above except what `IAppConsoleLogger` wrote; ASP.NET Core, EF Core and the HsnSoft packages now reach Graylog from Information (libraries from Warning) |
| `IRefreshableLogger`, `PersistentLoggerSinkRefreshWorker` in Shared | built in: a hosted service refreshes the Graylog sink |
| `HsnSoft.Base.Serilog.Mask.SensitiveDataAttribute` | `HsnSoft.Base.Logging.Masking.SensitiveDataAttribute` (a property now, usable the same way) |
| `FrameworkLogger:*` settings | the same keys; new: `PersistentMinimumLevel`, `LevelOverrides` |
| `IEventBusLogger` and its persisted log models | `ILogger` (see above) |
| `HsnSoft.Base.Json.*` | not ported: no product uses `IJsonSerializer`; they use Newtonsoft or `System.Text.Json` directly |

## One Error for a failure

A failure is logged once: the log that ends the pair carries the stack. A request that ends in a 500 is the `HttpResponse` Error (`Reason`,
`ExceptionIdentifier`, `ExceptionType` and the stack); a message whose handler failed after its attempts is the `ConsumeEnd` `CONSUME_EVENT_ERROR` (with `Attempts`, how many times the handler ran). Each failed
attempt is a `CONSUME_EVENT_RETRY` Warning, and what follows a failed message (`RABBITMQ_CONSUMER_MOVED_TO_ERROR_QUEUE`, a Warning that names the error queue; the report itself is only a Debug) is the
consequence, not a second Error. A known error (4xx, a rejected message) is a Warning without a stack.
