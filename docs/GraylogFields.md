# The fields of a log in Graylog

What each type of log has, read from a real run (every scenario of the samples: a good order, 400, 500, a message rejected and one that failed, a request
the gateway answered itself, a service that was down, a file upload and download). `●` = every log of that type has it, `◐` = some do (it depends on the case: the user is known, there
was an exception, the client sent the header), empty = never.

| Alan | HttpRequest | HttpResponse | HttpRequestResponse | ConsumeStart | ConsumeEnd | FrameworkLog | PlatformLog | AppLog |
|---|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|
| `AcceptLanguage` | ◐ |  | ◐ |  |  |  |  |  |
| `ActionId` |  |  |  |  |  |  | ◐ |  |
| `ActionName` |  |  |  |  |  |  | ◐ |  |
| `Assembly` | ● | ● | ● | ● | ● | ● | ● | ● |
| `AssemblyName` |  |  |  |  |  |  | ◐ |  |
| `Attempt` |  |  |  |  |  | ◐ |  |  |
| `AuthenticationScheme` |  |  |  |  |  |  | ◐ |  |
| `Channel` |  |  |  |  |  | ◐ |  |  |
| `ClientChannel` | ◐ |  | ◐ | ● | ● |  |  | ● |
| `ClientIp` | ● |  | ● |  |  |  |  |  |
| `ClientLat` | ◐ |  | ◐ | ● | ● |  |  | ● |
| `ClientLong` | ◐ |  | ◐ | ● | ● |  |  | ● |
| `ClientVersion` | ◐ |  | ◐ | ● | ● |  |  | ● |
| `ConsumerTag` |  |  |  |  |  | ◐ |  |  |
| `ContentLength` |  |  |  |  |  |  | ◐ |  |
| `ContentRoot` |  |  |  |  |  |  | ◐ |  |
| `ContentType` | ◐ |  | ● |  |  |  | ◐ |  |
| `Controller` |  |  |  |  |  |  | ◐ |  |
| `CorrelationId` | ● | ● | ● | ● | ● | ◐ | ◐ | ● |
| `ElapsedMilliseconds` |  |  |  |  |  |  | ◐ |  |
| `ElapsedMs` |  | ● | ● |  | ● |  |  |  |
| `EndpointName` |  |  |  |  |  |  | ◐ |  |
| `EnvName` |  |  |  |  |  |  | ◐ |  |
| `Error` |  |  |  |  |  | ◐ |  |  |
| `EventName` |  |  |  | ● | ● | ◐ |  | ● |
| `ExceptionIdentifier` |  | ◐ |  |  | ◐ | ◐ |  |  |
| `ExceptionMessage` |  |  |  |  | ◐ | ◐ | ◐ |  |
| `ExceptionSource` |  |  |  |  | ◐ | ◐ | ◐ |  |
| `ExceptionType` |  | ◐ | ◐ |  | ◐ | ◐ | ◐ |  |
| `Facility` | ● | ● | ● | ● | ● | ● |  |  |
| `FileBytes` |  | ◐ |  |  |  |  |  |  |
| `FileCount` |  | ◐ |  |  |  |  |  |  |
| `FileDownloadName` |  |  |  |  |  |  | ◐ |  |
| `FileResultType` |  |  |  |  |  |  | ◐ |  |
| `Files` |  | ◐ |  |  |  |  |  |  |
| `ForwardedFor` | ● |  |  |  |  |  |  |  |
| `From` | ● | ● | ● | ● | ● |  |  |  |
| `FullName` |  |  |  |  |  |  | ◐ |  |
| `Handler` |  |  |  |  | ● | ◐ |  |  |
| `HopLevel` |  |  |  | ● | ● | ◐ |  | ● |
| `Host` | ● |  | ● |  |  |  | ◐ |  |
| `HostName` |  |  |  |  |  | ◐ |  |  |
| `LogType` | ● | ● | ● | ● | ● | ● | ● | ● |
| `LoggerName` | ● | ● | ● | ● | ● | ● | ● | ● |
| `Max` |  |  |  |  |  | ◐ |  |  |
| `MessageId` |  |  |  | ● | ● | ◐ |  | ● |
| `MessageType` | ● | ● | ● | ● | ● | ● | ● | ● |
| `Method` | ● |  | ● |  |  |  | ◐ |  |
| `MethodInfo` |  |  |  |  |  |  | ◐ |  |
| `ObjectResultType` |  |  |  |  |  |  | ◐ |  |
| `OrderId` |  |  |  |  |  |  |  | ● |
| `Origin` | ◐ |  | ◐ |  |  |  |  |  |
| `ParentId` | ● | ● | ● |  |  | ◐ | ◐ |  |
| `ParentMessageId` |  |  |  | ◐ | ◐ |  |  | ◐ |
| `Path` |  |  |  |  |  |  | ◐ |  |
| `Port` |  |  |  |  |  | ◐ |  |  |
| `Producer` |  |  |  | ● | ● | ◐ |  | ● |
| `ProductId` |  |  |  |  |  |  |  | ◐ |
| `Protocol` |  |  |  |  |  |  | ◐ |  |
| `Quantity` |  |  |  |  |  |  |  | ◐ |
| `Queue` |  |  |  |  |  | ◐ |  |  |
| `Reason` |  | ◐ | ◐ |  | ◐ | ◐ | ◐ |  |
| `RequestBody` | ◐ |  |  |  |  |  |  |  |
| `RequestBytes` | ● |  | ◐ |  |  |  |  |  |
| `RequestHeaders` | ● |  | ● |  |  |  |  |  |
| `RequestId` | ● | ● | ● |  |  | ◐ | ◐ |  |
| `RequestPath` | ● | ● | ● |  |  | ◐ | ◐ |  |
| `ResponseBody` |  | ◐ |  |  |  |  |  |  |
| `ResponseBytes` |  | ● | ● |  |  |  |  |  |
| `ResponseHeaders` |  | ● | ● |  |  |  |  |  |
| `RouteData` |  |  |  |  |  |  | ◐ |  |
| `Scheme` |  |  |  |  |  |  | ◐ |  |
| `ScopeId` |  |  |  | ● | ● | ◐ |  | ● |
| `Solution` | ● | ● | ● | ● | ● | ● | ● | ● |
| `SourceContext` | ● | ● | ● | ● | ● | ● | ● | ● |
| `SpanId` | ● | ● | ● |  |  | ◐ | ◐ |  |
| `StackTrace` |  |  |  |  | ◐ | ◐ | ◐ |  |
| `Status` |  | ● | ● |  |  | ◐ |  |  |
| `StatusCode` |  |  |  |  |  |  | ◐ |  |
| `To` | ● | ● | ● | ● | ● |  |  |  |
| `TraceId` | ● | ● | ● | ● | ● | ◐ | ◐ | ● |
| `TrackingNumber` |  |  |  |  |  |  |  | ◐ |
| `Type` |  |  |  |  |  |  | ◐ |  |
| `UserAgent` | ● |  | ● |  |  |  |  |  |
| `UserId` | ◐ | ◐ | ◐ | ● | ● |  |  | ● |
| `UserRoles` | ◐ | ◐ | ◐ | ● | ● |  |  | ● |
| `Version` | ● | ● | ● | ● | ● | ● | ● | ● |
| `address` |  |  |  |  |  |  | ◐ |  |
| `error` |  |  |  |  |  |  | ◐ |  |
| `level` | ● | ● | ● | ● | ● | ● | ● | ● |
| `message` | ● | ● | ● | ● | ● | ● | ● | ● |
| `statusCode` |  |  |  |  |  |  | ◐ |  |
| `stringLevel` | ● | ● | ● | ● | ● | ● | ● | ● |
| `targetUrl` |  |  |  |  |  |  | ◐ |  |
| `timestamp` | ● | ● | ● | ● | ● | ● | ● | ● |
| `version` |  |  |  |  |  |  | ◐ |  |
| `versionPolicy` |  |  |  |  |  |  | ◐ |  |

Who asked (`UserId`, `UserRoles`, and the client of the request: `ClientChannel`, `ClientVersion`, `ClientLat`, `ClientLong`) is only on the logs of the request
(`HttpRequest`, `HttpResponse`, `HttpRequestResponse`), of the message (`ConsumeStart`, `ConsumeEnd`) and of the application (`AppLog`); not on `FrameworkLog` and
`PlatformLog`. `HttpRequestResponse` (the gateway, for what it answered itself, or when the service could not be reached) has every field of `HttpRequest` and of
`HttpResponse`; for a 5xx without an exception it says why in `Reason` (and `ExceptionType`) as a 500 does. `Facility` of an `AppLog` is whatever the developer
sets with `logger.BeginFacility("ORDER_IMPORTED")`.

**Only `PlatformLog` has** (ASP.NET Core, Entity Framework, YARP: their own fields, not ours): `ActionId`, `ActionName`, `AssemblyName`,
`AuthenticationScheme`, `ContentLength`, `ContentRoot`, `Controller`, `ElapsedMilliseconds`, `EndpointName`, `EnvName`, `FileDownloadName`, `FileResultType`,
`FullName`, `MethodInfo`, `ObjectResultType`, `Protocol`, `RouteData`, `Scheme`, `StatusCode`, `Type`, `address`, `statusCode`, `targetUrl`, `version`,
`versionPolicy`.

**Only `FrameworkLog` has** (the fields of the RabbitMQ logs and of retries): `Attempt`, `Channel`, `ConsumerTag`, `Error`, `HostName`, `Max`, `Port`, `Queue`.

`FrameworkLog` is not one shape: its `Facility` says which (`CONSUME_EVENT_RETRY`, `RABBITMQ_CONSUMER_*`,
`RABBITMQ_FAILED_EVENT_*`, `RABBITMQ_CONNECTION_*`, ...) and the fields follow it. `AppLog` carries, besides what the developer wrote (`OrderId`, `ProductId`,
`Quantity`, `TrackingNumber` here), the fields of the request or message the code runs in.
