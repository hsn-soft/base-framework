# Web api

`AddBaseWebApi()` and `UseBaseExceptionHandling()` give every web api of a system the same answers. Everything a client receives, a
result or a failure, is the one type `BaseResponse` (`HsnSoft.Base.Communication`, in Core), so a caller reads it the same way
whatever happened:

```json
{ "statusCode": 200, "statusMessages": ["Success"], "traceId": "0HN...", "payload": { "id": "..." } }
{ "statusCode": 400, "statusMessages": ["Validation failed.", "Name: Name is required."], "traceId": "0HN...", "errorCode": "validation_error" }
```

| What happens | The answer |
|---|---|
| an action returns an object, a `JsonResult`, a bare status, `Ok()` | `BaseResponse<object>` with the object as `payload` (a bare status or an empty result is a `BaseResponse` without one) |
| it returns a `BaseResponse`, a `ProblemDetails`, a file or a redirect | left as it is |
| the model is invalid | 400, `errorCode: validation_error`, the fields named outside production |
| an exception | the status it means (below), its message when it is written for the client, the inner messages outside production |
| a status without a body (404 for an unknown route, 405) | a `BaseResponse` with `errorCode: http_404`; not for files and other binary answers |

| Exception | Status |
|---|---|
| `NotFoundException` | 404 |
| `BusinessRuleException`, `DomainException`, `ArgumentException`, `BadHttpRequestException` | 400 |
| `BusinessException(errorCode, message)` | 400, with `errorCode` and the `WithData(...)` values |
| `BaseHttpException(status, message)` | the status |
| `UnauthorizedAccessException` | 401 |
| anything else | 500, `INTERNAL_SERVER_ERROR` (a key, the client shows its own text); the trace id is how support finds it (it is on the `HttpResponse` Error log, which carries the stack) |

**What the client learns of a 500.** In production it learns the status 500, the key `INTERNAL_SERVER_ERROR` and the `traceId`, nothing of the exception. The
`traceId` of every answer (success or failure) is the `TraceId` of the logs of that request: the W3C trace id a gateway passes on, so the logs of the
gateway and of every service it went through have it (without a trace, the `RequestId` of ASP.NET Core). Support asks for it and searches `TraceId:<value>` in Graylog,
where the log of the failure has the exception, `ExceptionIdentifier` (file and line) and `Reason`. Where the service does not run for real (development, a
developer's docker) the answer also says why and where (`Reason` and `ExceptionIdentifier` as two more messages), so the developer sees it at once. The environments
that hide the details are `Production`, `Staging`, `stage` and `uat`; change them with `ApiErrors:HideDetailsIn` (`AddBaseWebApi(configuration)`; a list in the
configuration replaces the default). A 4xx always tells its message (it is the caller's error); a validation failure names the fields only where details are shown.

JSON is camel case, without nulls, enums as text. A string a request leaves out is not an error by itself (mark what is required with
`[Required]`), as in the services in production. Register your own `IStatusMessageProvider` (the texts, e.g. localized) or
`IApiExceptionMapper` (the exceptions of a product) before `AddBaseWebApi()` to replace the defaults. A gateway that only forwards what
the services answer does not wrap their answers again: the filter only touches the results of its own controllers.

The request and response logs are described in [Logging.md](Logging.md); in a system with a gateway they belong to the gateway.
