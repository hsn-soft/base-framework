# The fields of a log in Graylog

What each type of log has, **measured** from the logs of one real run of the samples (`Version:tbl004922`: a good order, a 400, a 500, a message that
failed, a request the gateway answered itself, a file upload and download, the log examples of the Abc identity service). Nothing in the code declares it:
the writer of a log puts its fields on it, the enrichers add the common ones and `NoiseEnricher` removes what a type must not have (see
[Logging.md](Logging.md), *Fields and facility*, and [ADR-0019](adr/0019-log-fields-pipeline.md)). Regenerate it with `scripts/graylog-fields/generate.py` after a change to the fields.
`●` = every log of that type has it, `◐` = some do (it depends on the case: the user is known, there was an exception), empty = never, `?` = the run had no log of
that type (a `SharedLog` is written by the shared library of a product: the rate limit of the Xyz samples writes one only when Redis is down), so nothing can be said.

Logs of this run: `HttpRequest` 12, `HttpResponse` 12, `HttpRequestResponse` 4, `ConsumeStart` 6, `ConsumeEnd` 6, `FrameworkLog` 29, `PlatformLog` 2, `SharedLog` 0, `AppLog` 8

| Alan | `HttpRequest` | `HttpResponse` | `HttpRequestResponse` | `ConsumeStart` | `ConsumeEnd` | `FrameworkLog` | `PlatformLog` | `SharedLog` | `AppLog` |
|---|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|
| `AcceptLanguage` | ◐ |  | ◐ |  |  |  |  | ? |  |
| `Assembly` | ● | ● | ● | ● | ● | ● | ● | ? | ● |
| `Attempt` |  |  |  |  |  | ◐ |  | ? |  |
| `Attempts` |  |  |  |  | ◐ |  |  | ? |  |
| `Channel` |  |  |  |  |  | ◐ |  | ? |  |
| `ClientChannel` | ◐ |  | ◐ | ● | ● |  |  | ? | ◐ |
| `ClientIp` | ● |  | ● |  |  |  |  | ? |  |
| `ClientLat` | ◐ |  | ◐ | ● | ● |  |  | ? | ◐ |
| `ClientLong` | ◐ |  | ◐ | ● | ● |  |  | ? | ◐ |
| `ClientVersion` | ◐ |  | ◐ | ● | ● |  |  | ? | ◐ |
| `ConsumerTag` |  |  |  |  |  | ◐ |  | ? |  |
| `ContentType` | ◐ |  | ● |  |  |  |  | ? |  |
| `CorrelationId` | ● | ● | ● | ● | ● | ◐ | ● | ? | ● |
| `CreatedUserId` |  |  |  |  |  |  |  | ? | ◐ |
| `CreatedUserName` |  |  |  |  |  |  |  | ? | ◐ |
| `ElapsedMs` |  | ● | ● |  | ● |  |  | ? |  |
| `ErrorQueue` |  |  |  |  |  | ◐ |  | ? |  |
| `EventName` |  |  |  | ● | ● | ◐ |  | ? | ◐ |
| `ExceptionIdentifier` |  | ◐ |  |  | ◐ | ◐ |  | ? |  |
| `ExceptionMessage` |  | ◐ |  |  | ◐ | ◐ | ● | ? | ◐ |
| `ExceptionSource` |  | ◐ |  |  | ◐ | ◐ | ● | ? | ◐ |
| `ExceptionType` |  | ◐ | ◐ |  | ◐ | ◐ | ● | ? | ◐ |
| `Facility` | ● | ● | ● | ● | ● | ● |  | ? | ◐ |
| `FileBytes` |  | ◐ |  |  |  |  |  | ? |  |
| `FileCount` |  | ◐ |  |  |  |  |  | ? |  |
| `Files` |  | ◐ |  |  |  |  |  | ? |  |
| `ForwardedFor` | ◐ |  |  |  |  |  |  | ? |  |
| `From` | ● | ● | ● | ● | ● |  |  | ? |  |
| `Handler` |  |  |  |  | ● | ◐ |  | ? |  |
| `HopLevel` |  |  |  | ● | ● | ◐ |  | ? | ◐ |
| `Host` | ● |  | ● |  |  |  |  | ? |  |
| `HostName` |  |  |  |  |  | ◐ |  | ? |  |
| `LogType` | ● | ● | ● | ● | ● | ● | ● | ? | ● |
| `LoggerName` | ● | ● | ● | ● | ● | ● | ● | ? | ● |
| `Max` |  |  |  |  |  | ◐ |  | ? |  |
| `MessageBody` |  |  |  | ● |  |  |  | ? |  |
| `MessageId` |  |  |  | ● | ● | ◐ |  | ? | ◐ |
| `MessageType` | ● | ● | ● | ● | ● | ● | ● | ? | ● |
| `Method` | ● |  | ● |  |  |  |  | ? |  |
| `Order` |  |  |  |  |  |  |  | ? | ◐ |
| `OrderId` |  |  |  |  |  |  |  | ? | ◐ |
| `OrderNo` |  |  |  |  |  |  |  | ? | ◐ |
| `Origin` | ◐ |  | ◐ |  |  |  |  | ? |  |
| `ParentId` | ● | ● | ● |  |  |  | ● | ? |  |
| `ParentMessageId` |  |  |  | ◐ | ◐ |  |  | ? | ◐ |
| `Port` |  |  |  |  |  | ◐ |  | ? |  |
| `Producer` |  |  |  | ● | ● | ◐ |  | ? | ◐ |
| `ProductId` |  |  |  |  |  |  |  | ? | ◐ |
| `Quantity` |  |  |  |  |  |  |  | ? | ◐ |
| `Queue` |  |  |  |  |  | ◐ |  | ? |  |
| `Reason` |  | ◐ | ◐ |  | ◐ | ◐ |  | ? |  |
| `RequestBody` | ◐ |  |  |  |  |  |  | ? |  |
| `RequestBytes` | ● |  | ◐ |  |  |  |  | ? |  |
| `RequestHeaders` | ● |  | ● |  |  |  |  | ? |  |
| `RequestId` | ● | ● | ● |  |  |  | ● | ? |  |
| `RequestPath` | ● | ● | ● |  |  |  | ● | ? |  |
| `ResponseBody` |  | ◐ |  |  |  |  |  | ? |  |
| `ResponseBytes` |  | ● | ● |  |  |  |  | ? |  |
| `ResponseHeaders` |  | ● | ● |  |  |  |  | ? |  |
| `ScopeId` |  |  |  | ● | ● | ◐ |  | ? | ◐ |
| `Solution` | ● | ● | ● | ● | ● | ● | ● | ? | ● |
| `SourceContext` | ● | ● | ● | ● | ● | ● | ● | ? | ● |
| `SpanId` | ● | ● | ● |  |  |  | ● | ? |  |
| `StackTrace` |  | ◐ |  |  | ◐ | ◐ | ● | ? | ◐ |
| `Status` |  | ● | ● |  |  |  |  | ? |  |
| `To` | ● | ● | ● | ● | ● |  |  | ? |  |
| `Total` |  |  |  |  |  |  |  | ? | ◐ |
| `TraceId` | ● | ● | ● | ● | ● | ◐ | ● | ? | ● |
| `TrackingNumber` |  |  |  |  |  |  |  | ? | ◐ |
| `UserAgent` | ● |  | ● |  |  |  |  | ? |  |
| `UserId` | ◐ | ◐ | ◐ | ● | ● |  |  | ? | ◐ |
| `UserRoles` | ◐ | ◐ | ◐ | ● | ● |  |  | ? | ◐ |
| `Version` | ● | ● | ● | ● | ● | ● | ● | ? | ● |
| `error` |  |  |  |  |  |  | ● | ? |  |

**Only `PlatformLog` has:** `error`.

**Only `FrameworkLog` has:** `Attempt`, `Channel`, `ConsumerTag`, `ErrorQueue`, `HostName`, `Max`, `Port`, `Queue`.

`FrameworkLog` is not one shape: its `Facility` says which (`CONSUME_EVENT_RETRY`, `RABBITMQ_CONSUMER_*`, `RABBITMQ_CONNECTION_*`, ...) and the fields follow it.
`AppLog` is what its developer wrote (the arguments of the message are fields, `{@Object}` is one JSON field) plus the common fields and the `Facility` they
gave it with `BeginFacility`.
