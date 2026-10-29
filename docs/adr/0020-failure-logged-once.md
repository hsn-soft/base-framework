# ADR-0020: A failure is logged once, with where it was thrown and why; a known error is a Warning

- **Status:** Accepted
- **Date:** 2026-10-24

## Context

A 500 produced two Error logs (the exception handler's and the response log's) and a failed message several (the dispatcher's, the consumer's, a report); alerts fired twice and counts doubled. Developers need to find the
line that failed even when a platform library threw it, and the framework's own code is not what they know. Not every exception is a failure: a business rule that says no is how the application answers.

## Decision

- **Known errors** (`KnownExceptions`: `BusinessException`, `BusinessRuleException`, `DomainException`, `NotFoundException`, ...) thrown without a try/catch from the controller, application or domain layers are **client errors (4xx) and
  Warnings, with no stack**. **Unknown errors** (a null reference, a driver exception) are **500 and Error**. In a message handler a known error is **rejected**: `CONSUME_EVENT_REJECTED`, a Warning on the `ConsumeEnd`, not retried, not sent to the error queue.
- **One Error per failure, on the log that ends the pair:** the `HttpResponse` (5xx) or the `ConsumeEnd` (`CONSUME_EVENT_ERROR`, with `Attempts`, how many times the handler ran) carries the exception fields and the **`StackTrace`**. The
  `GLOBAL_EXCEPTION_HANDLER` log and `RABBITMQ_CONSUMER_FAILED_AFTER_ERROR` were removed. Each failed attempt is a `CONSUME_EVENT_RETRY` Warning (the only place that shows the attempts and each exception); what follows a failed message is a
  consequence, not a second Error: `RABBITMQ_CONSUMER_MOVED_TO_ERROR_QUEUE` (a Warning naming the error queue); the report itself is Debug.
- **The exception fields** (`ExceptionLogFields`) are `ExceptionType`, `ExceptionIdentifier` and `Reason` (and `ErrorCode` for a `BusinessException`). The exception itself is attached to the log of a failure (a 5xx response, a failed `ConsumeEnd`), and the Graylog sink then
  adds `StackTrace`, `ExceptionMessage` and `ExceptionSource`; a known error (4xx, a rejected message) has no exception attached, so no stack.
  - `ExceptionIdentifier` is `Solution:<solution>|Assembly:<assembly>|File:<file>|Line:<n>`: **the file and the line, no method name**, and the line must be right. It looks at the frames of the solution that is running (shared code
    and the microservice's, not the framework's), innermost exception first, so a driver exception thrown from a repository points at the developer's line; if none has a file it falls back to `Method:` or `Type:`. It has no length limit.
    Lines are right because the services publish their `.pdb` files and `Version` says which build ran. `throw ex;` resets the stack and hides the real line: use `throw;`.
  - `Reason` is every message of the exception chain joined with ` >> `, at most 8192 characters (a cut is marked).
- The answer to the client is in [ADR-0022](0022-web-api-answers.md); the log is where the exception is.

## Consequences

- An alert on `level:Error` counts failures once.
- The `ExceptionIdentifier` is only as good as the symbols: a service must publish pdbs.
- A developer writing a known error must throw one of the known exception types, or it is a 500.

## Related

[ADR-0018](0018-chain-logs.md), [ADR-0022](0022-web-api-answers.md), `docs/Logging.md` (What a failure's logs say), `ExceptionLogFields`, `KnownExceptions`.
