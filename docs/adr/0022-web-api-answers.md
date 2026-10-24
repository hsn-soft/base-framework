# ADR-0022: Every answer is a `BaseResponse`; what a client learns of a 500 is a key and a trace id

- **Status:** Accepted
- **Date:** 2026-10-24

## Context

Clients of several services read results and failures in different shapes; a 500 sometimes leaked an exception message, and support could not find the log of a failure the client reported.

## Decision

- **Everything a client receives is one type, `BaseResponse`** (`HsnSoft.Base.Communication`, in Core): `statusCode`, `statusMessages`, `traceId`, `payload`, `errorCode`. `AddBaseWebApi()` and `UseBaseExceptionHandling()` produce it for results (an object, a bare
  status), validation failures (400, `errorCode: validation_error`), exceptions and bodyless statuses (404, 405: `errorCode: http_404`); a `BaseResponse`, a `ProblemDetails`, a file or a redirect is left as it is. JSON is camel case, without nulls,
  enums as text. A string a request leaves out is not an error by itself (mark what is required with `[Required]`, **on the parameter** of a record, not `[property: Required]`).
- **Exceptions map to statuses:** `NotFoundException` 404; `BusinessRuleException`, `DomainException`, `ArgumentException`, `BadHttpRequestException` 400; `BusinessException` 400 with its `errorCode` and data; `BaseHttpException` its status;
  `UnauthorizedAccessException` 401; anything else **500**.
- **What the client learns of a 500: the status, the key `INTERNAL_SERVER_ERROR` (a key: the client shows its own text) and the `traceId`, nothing of the exception.** The `traceId` of **every** answer is the `TraceId` of the logs of that request (the W3C
  trace id a gateway passes on; without a trace, the `RequestId`), so support searches `TraceId:<value>` in Graylog and finds the failure with its stack ([ADR-0020](0020-failure-logged-once.md)).
- **Outside production-like environments the answer also says why and where** (`Reason`, `ExceptionIdentifier`), and a validation failure names its fields. The environments that hide them are `Production`, `Staging`, `stage`, `uat`
  (`ApiErrors:HideDetailsIn`; a list in configuration replaces the default). A 4xx always tells its message.
- A product replaces `IStatusMessageProvider` (texts, localization) or `IApiExceptionMapper` (its exceptions) by registering its own before `AddBaseWebApi()`. A gateway that only forwards does not wrap the services' answers again.
- Controllers: no minimal APIs in the services, controllers only; a controller only calls the application service.

## Consequences

- One reader for every client; a 500 never leaks internals in production.
- The same answer shape applies to the gateway's own answers.

## Related

[ADR-0018](0018-chain-logs.md), [ADR-0020](0020-failure-logged-once.md), `docs/WebApi.md`.
