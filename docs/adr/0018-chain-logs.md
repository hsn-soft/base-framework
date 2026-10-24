# ADR-0018: The chain logs: a request and a message are each a pair of logs, with ids that tie a whole chain together

- **Status:** Accepted
- **Date:** 2026-10-24

## Context

In a system of a gateway, services and events, one user action travels through several processes. To follow it, and to know what was in flight when a pod died, every hop needs the same ids, a start and an end,
and who talked to whom. The old request/response logging middleware did part of this and was the reference for what the new one keeps.

## Decision

- **Five log types follow a chain** (their own `LogType`, said by the writer): `HttpRequest` and `HttpResponse` (the request log is written **first**, on purpose: if the pod dies in the middle, it says what was in flight),
  `HttpRequestResponse`, `ConsumeStart` and `ConsumeEnd` (the event bus dispatcher).
- **Ids:** `CorrelationId` is the client's `X-Correlation-Id` when it sends one, else the W3C `TraceId`; it is kept across HTTP hops and messages (a header of the envelope). `TraceId` is the W3C trace id (also derived from the
  correlation id of a message when that is a 32-hex trace id). The two logs of an HTTP request share `RequestId`; the two logs of a message share `ScopeId`. `From` and `To` say who talked to whom (`httpclient` ->
  `controller-action` in lower case or the request path; a message's producer or flow -> the event name), and the `Flow` header of the envelope carries the step to the next hop. `ElapsedMs` is on the end log.
- **Level is the status alone**, the same at a service and at a gateway: 5xx is Error, 4xx is Warning, the rest Information (a gRPC call by its gRPC status). A known error of the application (ADR-0020) is a 4xx and a Warning.
- **Who writes them:** the **services** write the request and response logs (`RequestLogging:Role` is `Service`). A **gateway** (`Role: Gateway`) writes **one `HttpRequestResponse` only for what it answered itself** (a rejected
  token, a 404, a 429, a 502 because the service was unreachable): the service never saw it. Logging the same request at both would double it, and the level would differ.
- **`HttpRequestResponse` is the union of the request and the response logs**: every field of both. When the gateway answers a 5xx without an exception (the service did not answer), it fills the response fields as for a 500:
  `Reason` and `ExceptionType` from YARP's forwarder error feature (read by name, no YARP dependency), else the text of the status.
- **Contents:** request: method, `RequestPath`, query, selected headers, client address and `ForwardedFor`, `RequestBytes`, `ContentType`, the user (`UserId`, `UserRoles`) when authenticated, the client headers; response:
  `Status`, `ResponseBytes`, headers, `ErrorCode`, the exception fields (ADR-0020), files (`Files`, `FileCount`, `FileBytes`), `GrpcStatus`. **Bodies are logged only for JSON, within limits, masked** ([ADR-0021](0021-sensitive-data-by-attributes.md));
  the sample gateway does not log bodies. Paths such as `/health`, `/swagger`, `/openapi` are not logged (`ExcludedPaths`). Lengths are bounded (`MaxUrlLength`, `MaxHeadersLength`, body limits) and cut text is marked.
- **A message**: `ConsumeStart` carries the message it started with (`MessageBody`: the envelope and the event together, masked, on by default `EventBusOptions.LogConsumedMessage`, cut at `MaxLoggedMessageLength`, left out when no
  masker is registered); `ConsumeEnd` has the result; the who-fields come from the envelope headers.

## Consequences

- A system needs the services to log requests; a gateway alone leaves holes (that is by design).
- `PersistentLogger:LogLevel: Warning` drops the Information logs of this chain (ADR-0017).
- The field list of each type is in `docs/GraylogFields.md`, measured from a run.

## Related

[ADR-0019](0019-log-fields-pipeline.md), [ADR-0020](0020-failure-logged-once.md), `docs/Logging.md` (The chain logs), `docs/EventBus.md`, `RequestLoggingMiddleware`, `EventDispatcher`.
