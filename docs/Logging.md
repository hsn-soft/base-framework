# Logging

How to use the logging of the framework and what its logs look like. **Why it is this way** is in the ADRs: [0015](adr/0015-logging-architecture.md) (everything uses `ILogger`),
[0016](adr/0016-log-types-by-namespace.md) (the log types), [0017](adr/0017-two-places-two-levels.md) (the console and what is kept), [0018](adr/0018-chain-logs.md) (the chain logs),
[0019](adr/0019-log-fields-pipeline.md) (the fields), [0020](adr/0020-failure-logged-once.md) (failures), [0021](adr/0021-sensitive-data-by-attributes.md) (sensitive data).

The framework packages (EventBus, RabbitMQ, Kafka, PostgreSQL, MongoDB, ...) do not know which logging library a product uses. They write through `Microsoft.Extensions.Logging`
(`ILogger<T>`, `[LoggerMessage]`), and the product decides where the logs go: Serilog and Graylog in one service, NLog in another. These packages help with that; all are optional.

| Package | What it is |
|---|---|
| `HsnSoft.Base.Logging` | Independent of any logging library: `IAppConsoleLogger` (console only), the settings every provider reads (`Logging:LogLevel` for the console, the `PersistentLogger` section for what is kept), masking of sensitive data. |
| `HsnSoft.Base.Logging.Serilog` | Serilog as the provider: the console, the persistent sink (Graylog, or a file), the routing rules, the self-healing Graylog sink. |
| `HsnSoft.Base.Logging.NLog` | NLog as the provider, with the same settings, console and file lines and fields: `builder.Logging.AddBaseNLog(builder.Configuration, "OrderService")` instead of `AddBaseSerilog`. |

## Using it (Serilog)

```csharp
builder.Services.AddBaseLogging();                                   // IAppConsoleLogger, ILogMasker
builder.Logging.AddBaseSerilog(builder.Configuration, "OrderService"); // everything that logs through ILogger goes through Serilog
```

```json
"Logging": {
  "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" }
},
"PersistentLogger": {
  "LogLevel": "Information",
  "PlatformMinimumLevel": "Warning",
  "AppNamespace": "Hhs",
  "IsGrayLogActive": true,
  "GrayLog": { "Address": "http://graylog", "Port": 12201, "SinkRefreshIntervalMinutes": 5 },
  "FilePath": "Logs/logs.txt",
  "IsSelfLogActive": false,
  "LevelOverrides": { "Microsoft.AspNetCore": "Error" }
}
```

- **The console** is told by `Logging:LogLevel` (a default and a level per namespace), **what is kept** (Graylog, or the file `FilePath` when `IsGrayLogActive` is `false`) by `PersistentLogger:LogLevel` and
  `LevelOverrides`, never below Information. What each setting changes, where a log of each level can be seen and settings for the usual cases are in **[LogLevels.md](LogLevels.md)**.
- `AddBaseSerilog` removes the other logging providers, opens the filter of `Microsoft.Extensions.Logging` (Serilog decides, per place) and, unless you pass `replaceGlobalLogger: false`, assigns Serilog's static `Log.Logger`.
- The file is rolled daily and kept 30 days. The Graylog sink is rebuilt every `SinkRefreshIntervalMinutes` (a third-party sink can stay broken after a failed start); the replaced sink lives on for ten seconds so what it
  was sending is not lost.
- `IsSelfLogActive` prints the errors of the logging library itself (e.g. Graylog not reachable) to the error output, never to a sink: a failing sink must not log into itself.
- A product that logs Newtonsoft objects adds the destructuring itself, the framework does not depend on Newtonsoft:
  `builder.Logging.AddBaseSerilog(configuration, "Host", serilog => serilog.Destructure.JsonNetTypes())`.
- Level values may be Microsoft's (`Trace`, `Critical`) or Serilog's (`Verbose`, `Fatal`), in any case.

## The log types

Every log in Graylog has a `Solution` (the first part of the assembly name: `Hhs`, `Abc`), an `Assembly` (`Hhs.AuthServer.Http.Host`: which service, auth server or agent wrote it), a `Version` (the build that wrote it: the
environment variable `BUILD_NUMBER`, which the CI of the services sets to the image tag `{yyMMddHHmm}-sha-{git short sha}`; else `PersistentLogger:Version`, else the version of the entry assembly: with it the `Line` of a failure
is read in the code that ran) and a `LogType`:

| `LogType` | What it is | Written by |
|---|---|---|
| `FrameworkLog` | a framework library (`HsnSoft.Base.*`) talking about itself; the `Facility` says what: `RABBITMQ_CONSUMER_SUBSCRIBED`, `PRODUCE_EVENT_ERROR`, ... | the library, nobody has to do anything |
| `HttpRequest`, `HttpResponse`, `HttpRequestResponse` | an HTTP request when it arrives, and when it is answered; one log for what a gateway answered itself | `UseBaseRequestLogging()` (`HsnSoft.Base.AspNetCore.WebApi`) |
| `ConsumeStart`, `ConsumeEnd` | a message when its handler starts, and when it ends | the event bus |
| `SharedLog` | the shared library of the product (`{AppNamespace}.Shared.*`: its rate limit, its helpers) | the shared library |
| `AppLog` | what the developer of the application logs with `ILogger` | the application |
| `PlatformLog` | ASP.NET Core, Entity Framework, a database driver, any other library | the platform |

The type is told from the **namespace of the class that wrote the log**: `HsnSoft.Base.*` is the framework's, `{AppNamespace}.Shared.*` the shared library's, `{AppNamespace}.*` the application's (`PersistentLogger:AppNamespace`; without it the
first part of the assembly name), everything else the platform's. The chain types say theirs themselves; a developer cannot choose a type on a log line. See [ADR-0016](adr/0016-log-types-by-namespace.md).
`MessageType` is `Structured` when an object was logged with `{@Object}` and `Args` when only plain values were.

## The chain logs

A request that publishes an event whose handler publishes another event is one chain. The chain logs are written in pairs so a log reader can follow it, and so a pod that dies in the middle (out of memory) leaves its start
without an end. The `RequestId` of ASP.NET Core is the key of one HTTP request (`HttpRequest` + `HttpResponse` share it); the W3C trace (`TraceId`, and `SpanId`, `ParentId` on the request logs) is what a gateway passes on in `traceparent`.

| Field | Meaning |
|---|---|
| `CorrelationId` | the same along the whole chain, however many hops. A client (a mobile app) that sends `X-Correlation-Id` has its own id kept, so it finds its whole chain in the logs; a request without one takes the `TraceId` of the request (32 hex digits), not a new id: the same value is the `TraceId` of the logs of the gateway and of the services, and of the consume logs (a message carries only the correlation id, so a consume log gets `TraceId` when it is one). The response carries it back. |
| `ScopeId` | also 32 hex digits; the same in the start and end log of one message (`ConsumeStart` + `ConsumeEnd`), different for every message; every other log written while the message is handled carries it too. An HTTP request has no `ScopeId`: it has the `RequestId`. |
| `From`, `To` | `httpclient -> token-login` for a request (`controller-action` in lower case; for anything that is not a controller action, such as a proxied route, the request path in lower case). A message: `token-login -> ordercreated` (where it was published from, the event) and, one hop later, `ordercreated -> stockreserved`. A message published outside any request or message (an agent, a scheduler) comes from `agent`. |
| `ElapsedMs` | how long the step took (`HttpResponse`, `ConsumeEnd`) |
| `Facility` | `HTTP_REQUEST`, `HTTP_RESPONSE`, `HTTP_REQUEST_RESPONSE`, `CONSUME_EVENT_START`, `CONSUME_EVENT_SUCCESS` / `CONSUME_EVENT_ERROR` (the end), `CONSUME_EVENT_RETRY` |

`ConsumeStart`/`ConsumeEnd` also carry `HopLevel`, `MessageId`, `ParentMessageId`, `EventName` and `Producer`; where a message comes from travels in its `Flow` header (a message from an older producer, without it, comes from its
`Producer`). The response log has `Status` (the level follows it, wherever it is logged: 2xx and 3xx Information, 4xx Warning, 5xx Error) and the user. The request log says who asked: `ClientIp`, `ForwardedFor`, `UserAgent`,
`AcceptLanguage`, `Origin` (the host of the `Origin`, else the `Referer`, else the `From` header), `Referer`, `Host`, `ContentType`, `RequestBytes` (the size the request says it has: 0 without a body, absent when nobody knows it
beforehand, such as a chunked upload or a gRPC stream) and the client's own words (`ClientChannel`, `ClientVersion`, `ClientLat`, `ClientLong`, from the headers below). What only a product knows (the device, a location) is added with
an `IRequestLogEnricher`, called for both logs. `UserId` and `UserRoles` (the role claims, distinct, comma separated) are on the request log too when the request is authenticated: authentication runs after the logging middleware, so the
middleware asks for it first. What a message's headers say about who caused it is on the consume logs.

Request and response bodies are logged only with `RequestLogging:LogBodies` and an `ILogMasker` (`AddBaseLogging()`): masked, JSON only, cut to the body limits. Headers are one JSON text; `Authorization` and cookies are summarized, never written.

**Who writes them.** In a system with a gateway the **services** write `HttpRequest` and `HttpResponse` (they know the action, the user, the exception and the classes of the bodies, so their `[SensitiveData]` masks exactly); every answer of
a service that logged carries the header `X-Logged-By`. The **gateway** (`"RequestLogging": { "Role": "Gateway" }`) stays quiet for those and removes the header; for what it answered **itself** it writes one `HttpRequestResponse` with the
whole request and its answer, its level the status of the answer. If that answer is a 5xx the log also has the masked request body (the gateway keeps the JSON bodies it forwards, up to `MaxBodyReadBytes`, only for this), and
`Reason`/`ExceptionType` say why the service did not answer. A service that does not log (`Enabled: false`, down) leaves the gateway to log: a log twice is better than none. The correlation id is set and passed on in every role. See
[ADR-0018](adr/0018-chain-logs.md).

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

## What a failure's logs say

A failure is logged **once**: a request that ends in a 500 is the `HttpResponse` Error, a message whose handler failed after its attempts is the `ConsumeEnd` `CONSUME_EVENT_ERROR` (with `Attempts`); both carry the stack. Each failed attempt
is a `CONSUME_EVENT_RETRY` Warning, and what follows a failed message (`RABBITMQ_CONSUMER_MOVED_TO_ERROR_QUEUE`, which names the error queue) is its consequence, not a second Error. A known error (a 4xx, a rejected message) is a Warning without a stack.
Besides `ExceptionType`, every log of a failure has:

- `ExceptionIdentifier`: where it was thrown, like `Solution:Abc|Assembly:Abc.OrderService.Http.Host|File:OrdersController.cs|Line:92`: the first place with a source file in the code of the application that runs, looking at the innermost exception
  first, so a throw deep inside a library is reported where the application called it. It needs the symbols (`.pdb`) next to the assemblies, which `dotnet publish` copies; without them the method is told, and an exception that was never
  thrown has only its type. Use `throw;`, not `throw ex;`, which resets the line. It has no length limit.
- `Reason`: the message of the exception and of every exception inside it joined with `" >> "` (`outer >> middle >> inner`), up to 8192 characters.

For a 500 these two are what to look at; for a 400 they are there when they can be told. See [ADR-0020](adr/0020-failure-logged-once.md).

## Fields and facility

A log has the fields the writer put on it (the arguments of the message, the scopes), the common ones the enrichers add, and what `NoiseEnricher` and `FieldGuardSink` leave (see [ADR-0019](adr/0019-log-fields-pipeline.md)). An object, a dictionary
or a list is written as **one field of its name holding its JSON**, texts are cut at `PersistentLogger:MaxPersistentFieldLength` (16384; the console still shows the readable object), and an `AppLog` (and a `SharedLog`) has what its developer
wrote, the `Facility`, the `CorrelationId` and `TraceId` of its chain and its category (`SourceContext`), not the endpoint, span and request fields ASP.NET Core puts on every log. **What each type really has is
[GraylogFields.md](GraylogFields.md), measured from a run.**

**To add or remove a field**

- For one log: put it in the arguments (`"Order {OrderNo}"`), or in a scope for a group of logs (`BeginScope`, `BeginFacility`).
- For every log, a new field: an enricher added in `AddBaseSerilog(configuration, name, configure: serilog => serilog.Enrich.With(new MyEnricher()))`.
- For every log, take a field away: the same, with `logEvent.RemovePropertyIfPresent("Name")` in the enricher (it runs after `NoiseEnricher`, so it can remove what the framework leaves). What the framework itself must never put on a type is in `NoiseEnricher`.
- A field nobody wrote on purpose (it came from a library) shows in the map, in the column of the type that carries it: look at it after adding a package.

**Facility: what a log is about.** `Facility` is a top-level field, so `Facility:CONSUME_EVENT_ERROR` finds every handler that failed, and the facilities of a chain in order tell the story. A log gets it from the name of its event
(`[LoggerMessage(EventName = "CONSUME_EVENT_SUCCESS")]`; only names written in capitals count, the platform's own `ExecutedEndpoint` is not a facility): a framework library gives every log an event name, use `RABBITMQ_CONSUMER_SUBSCRIBED`-style
names for new ones. The application names the logs of an operation with `logger.BeginFacility("ORDER_IMPORTED")` (a scope: every log written inside it has that `Facility`).

## What the framework packages log

Categories are the class names (`HsnSoft.Base.EventBus.RabbitMQ.RabbitMqConsumer`), so a category prefix controls a whole package
(`"LevelOverrides": { "HsnSoft.Base.EventBus": "Warning" }`). The event bus logs with event names (`PRODUCE_EVENT_ERROR`,
`CONSUME_EVENT_SUCCESS`, ...) and puts `CorrelationId`, `MessageId` and `EventName` in the log scope, which Serilog turns into
properties (Graylog fields).

The RabbitMQ client library does not use `ILogger`, it only writes to an `EventSource` (`rabbitmq-client`). The RabbitMQ package
listens to it while the host runs and passes it on under the category `HsnSoft.Base.EventBus.RabbitMQ.Client`: its information as
Debug (console only), its warnings and errors as they are, so a connection the client gives up on shows up in Graylog.

The `ConsumeStart` log carries the message (envelope and event, masked) as `MessageBody`; the raw body is only logged with `EventBusOptions.LogMessageBody` (see [EventBus.md](EventBus.md)).

## NLog, and another provider

`AddBaseNLog` reads the same settings as `AddBaseSerilog` and gives the same result, so a service moves from one to the other by changing one call ([ADR-0076](adr/0076-more-providers-and-the-rules-every-provider-shares.md)):

```csharp
builder.Logging.AddBaseNLog(builder.Configuration, "OrderService");   // console: Logging:LogLevel; kept: PersistentLogger (Graylog over HTTP, or a file)
```

- The console line and the file line are the ones of the Serilog provider; the persistent log has the same fields (`LogType`, `MessageType`, `Facility`, the scope properties such as `CorrelationId`), an object logged with `{@Object}` as the one field holding its masked JSON, text cut at `MaxPersistentFieldLength`.
- `[SensitiveData]` is masked in the fields **and** in the message (NLog would write an `{@Object}` into the message whole, so the message is rendered from the template here).
- Graylog is written by a background queue that sends again what failed (GELF over HTTP, `{scheme}://address:port/gelf`: `http`, Graylog's own input, or `https` with `PersistentLogger:GrayLog:UseHttps` or an address that says `https://`), so `GrayLog:SinkRefreshIntervalMinutes` is not used. `PersistentLogger:IsSelfLogActive` shows NLog's own errors.
- The service has its own NLog factory: the global `LogManager` is not touched and an `nlog.config` is not read; `AddBaseNLog(configuration, name, nlog => nlog.AddRule(...))` adds a target of the product.

A provider for another library does what these two do, with its own tools, and uses the rules of `HsnSoft.Base.Logging` that both share: `LogShape` (the type of a log from its category, the `Facility` from an event name, the noise fields), `LogRouting` (the console and the persistent sink, from level and category) and
`SensitiveDataTree` (an object as a tree with the secrets masked, to convert to the library's own values).

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
| `FrameworkLogger:*` settings | renamed `PersistentLogger:*`. `FrameworkLogger:LogLevel` (what the console showed) is now the standard `Logging:LogLevel`, and `PersistentLogger:LogLevel` is the level of what is kept (it replaces `PersistentMinimumLevel`); `LevelOverrides` is for what is kept too, the console has its own category rules |
| `IEventBusLogger` and its persisted log models | `ILogger` (see above) |
| `HsnSoft.Base.Json.*` | not ported: no product uses `IJsonSerializer`; they use Newtonsoft or `System.Text.Json` directly |
