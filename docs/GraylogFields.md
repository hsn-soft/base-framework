# The fields of a log in Graylog

What each type of log has, read from a real run (every scenario of the samples: a good order, 400, 500, a message rejected and one that failed, a request
the gateway answered itself, a file upload and download). `●` = every log of that type has it, `◐` = some do (it depends on the case: the user is known, there
was an exception, the client sent the header), empty = never.

| Alan | HttpRequest | HttpResponse | HttpRequestResponse | ConsumeStart | ConsumeEnd | FrameworkLog | PlatformLog | AppLog |
|---|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|
| `AcceptLanguage` | ◐ |  |  |  |  |  |  |  |
| `Assembly` | ● | ● | ● | ● | ● | ● | ● | ● |
| `ClientChannel` | ◐ |  |  | ● | ● | ◐ |  | ● |
| `ClientIp` | ● |  | ● |  |  |  |  |  |
| `ClientLat` | ◐ |  |  | ● | ● | ◐ |  | ● |
| `ClientLong` | ◐ |  |  | ● | ● | ◐ |  | ● |
| `ClientVersion` | ◐ |  |  | ● | ● | ◐ |  | ● |
| `ContentType` | ◐ |  | ● |  |  |  | ◐ |  |
| `CorrelationId` | ● | ● | ● | ● | ● | ◐ | ◐ | ● |
| `Details` |  | ◐ |  |  |  |  |  |  |
| `ElapsedMs` |  | ● | ● |  | ● |  |  |  |
| `EventName` |  |  |  | ● | ● | ◐ |  | ● |
| `ExceptionIdentifier` |  | ◐ |  |  | ◐ | ◐ |  |  |
| `ExceptionMessage` |  |  |  |  | ◐ | ◐ |  |  |
| `ExceptionSource` |  |  |  |  | ◐ | ◐ |  |  |
| `ExceptionType` |  | ◐ |  |  | ◐ | ◐ |  |  |
| `Facility` | ● | ● | ● | ● | ● | ● |  |  |
| `FileBytes` |  | ◐ |  |  |  |  |  |  |
| `FileCount` |  | ◐ |  |  |  |  |  |  |
| `Files` |  | ◐ |  |  |  |  |  |  |
| `ForwardedFor` | ● |  |  |  |  |  |  |  |
| `From` | ● | ● | ● | ● | ● |  |  |  |
| `Handler` |  |  |  |  | ● | ◐ |  |  |
| `HopLevel` |  |  |  | ● | ● | ◐ |  | ● |
| `Host` | ● |  | ● |  |  |  | ◐ |  |
| `LogType` | ● | ● | ● | ● | ● | ● | ● | ● |
| `LoggerName` | ● | ● | ● | ● | ● | ● | ● | ● |
| `MessageId` |  |  |  | ● | ● | ◐ |  | ● |
| `MessageType` | ● | ● | ● | ● | ● | ● | ● | ● |
| `Method` | ● |  | ● |  |  |  | ◐ |  |
| `OrderId` |  |  |  |  |  |  |  | ● |
| `Origin` | ◐ |  |  |  |  |  |  |  |
| `ParentId` | ● | ● | ● |  |  | ◐ | ◐ |  |
| `ParentMessageId` |  |  |  | ◐ | ◐ |  |  | ◐ |
| `Path` |  |  |  |  |  | ◐ | ◐ |  |
| `Producer` |  |  |  | ● | ● | ◐ |  | ● |
| `ProductId` |  |  |  |  |  |  |  | ◐ |
| `Quantity` |  |  |  |  |  |  |  | ◐ |
| `Reason` |  | ◐ |  |  | ◐ | ◐ | ◐ |  |
| `RequestBody` | ◐ |  |  |  |  |  |  |  |
| `RequestBytes` | ● |  | ◐ |  |  |  |  |  |
| `RequestHeaders` | ● |  | ● |  |  |  |  |  |
| `RequestId` | ● | ● | ● |  |  | ◐ | ◐ |  |
| `RequestPath` | ● | ● | ● |  |  | ◐ | ◐ |  |
| `ResponseBody` |  | ◐ |  |  |  |  |  |  |
| `ResponseBytes` |  | ● | ● |  |  |  |  |  |
| `ResponseHeaders` |  | ● | ● |  |  |  |  |  |
| `ScopeId` |  |  |  | ● | ● | ◐ |  | ● |
| `Solution` | ● | ● | ● | ● | ● | ● | ● | ● |
| `SourceContext` | ● | ● | ● | ● | ● | ● | ● | ● |
| `SpanId` | ● | ● | ● |  |  | ◐ | ◐ |  |
| `StackTrace` |  |  |  |  | ◐ | ◐ |  |  |
| `Status` |  | ● | ● |  |  | ◐ |  |  |
| `To` | ● | ● | ● | ● | ● |  |  |  |
| `TraceId` | ● | ● | ● | ● | ● | ◐ | ◐ | ● |
| `TrackingNumber` |  |  |  |  |  |  |  | ◐ |
| `UserAgent` | ● |  | ● |  |  |  |  |  |
| `UserId` | ◐ | ◐ | ◐ | ● | ● | ◐ |  | ● |
| `UserRoles` | ◐ | ◐ | ◐ | ● | ● | ◐ |  | ● |
| `Version` | ● | ● | ● | ● | ● | ● | ● | ● |
| `level` | ● | ● | ● | ● | ● | ● | ● | ● |
| `message` | ● | ● | ● | ● | ● | ● | ● | ● |
| `stringLevel` | ● | ● | ● | ● | ● | ● | ● | ● |
| `timestamp` | ● | ● | ● | ● | ● | ● | ● | ● |

**Only `PlatformLog` has** (ASP.NET Core, Entity Framework, YARP: their own fields, not ours): `ActionId`, `ActionName`, `AssemblyName`,
`AuthenticationScheme`, `ContentLength`, `ContentRoot`, `Controller`, `ElapsedMilliseconds`, `EndpointName`, `EnvName`, `FileDownloadName`, `FileResultType`,
`FullName`, `MethodInfo`, `ObjectResultType`, `Protocol`, `RouteData`, `Scheme`, `StatusCode`, `Type`, `address`, `statusCode`, `targetUrl`, `version`,
`versionPolicy`.

**Only `FrameworkLog` has** (the fields of the RabbitMQ logs and of retries): `Attempt`, `Channel`, `ConsumerTag`, `Error`, `HostName`, `Max`, `Port`, `Queue`.

`FrameworkLog` is not one shape: its `Facility` says which (`GLOBAL_EXCEPTION_HANDLER`, `CONSUME_EVENT_RETRY`, `RABBITMQ_CONSUMER_*`,
`RABBITMQ_FAILED_EVENT_*`, `RABBITMQ_CONNECTION_*`, ...) and the fields follow it. `AppLog` carries, besides what the developer wrote (`OrderId`, `ProductId`,
`Quantity`, `TrackingNumber` here), the fields of the request or message the code runs in.
