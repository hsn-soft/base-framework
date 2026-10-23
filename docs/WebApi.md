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
| anything else | 500, "Internal server error." and nothing of the message (logged as `GLOBAL_EXCEPTION_HANDLER`) |

JSON is camel case, without nulls, enums as text. A string a request leaves out is not an error by itself (mark what is required with
`[Required]`), as in the services in production. Register your own `IStatusMessageProvider` (the texts, e.g. localized) or
`IApiExceptionMapper` (the exceptions of a product) before `AddBaseWebApi()` to replace the defaults. A gateway that only forwards what
the services answer does not wrap their answers again: the filter only touches the results of its own controllers.

The request and response logs are described in [Logging.md](Logging.md); in a system with a gateway they belong to the gateway.
