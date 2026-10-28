# ADR-0057: The caller of a message is in the envelope; publish an event, send a command

- **Status:** Accepted
- **Date:** 2026-10-25 (changes [ADR-0023](0023-event-bus.md); nothing is owed to the event bus of the previous framework, which was a source of ideas)
- **Note:** [ADR-0074](0074-multitenancy-is-a-package-of-its-own.md): the tenant is no longer a property of the envelope but two headers (`TenantId`, `IsSystemTenant`) that `HsnSoft.Base.MultiTenancy.EventBus` writes and reads; `ActingUser` has no tenant.

## Context

What a handler needs to act for the user who started a chain (the tenant, the user) was in the payload of the events (`TenantId` in every event) or in a dictionary of headers that nothing read. A handler therefore ran as nobody: its audit fields (`CreatedBy`, `UpdatedBy`) were empty and it had to switch
the tenant by hand. A message was published either to every service or, with a flag, to a queue that was only ever the publisher's own.

## Decision

- **The caller of the chain is part of the envelope:** `TenantId`, `IsSystemTenant`, `UserId`, `UserName`, `UserRoles`. They are filled when a message is published, from the message being handled (a chain keeps its caller) or else from `ICurrentUser`. What is not about the caller (`ClientChannel`,
  `ClientVersion`, ...) stays in the headers. The events carry only business data: no `TenantId` in a payload.
- **A handler runs as the caller.** `ActingUser` (in Core) is the user that code acts as when no request says who it is; `ICurrentUser` answers from it when no user is signed in on the request, and `ICurrentTenant` follows `ICurrentUser`. The dispatcher opens the scope around every handler; a message without a caller runs as nobody
  (not as whoever happened to be acting). A seed, a worker or the auth server (which writes the login history as the user who signs in) uses the same scope. `ICurrentUser` has `Roles`.
- **Publish an event, send a command:** `IEventBus.PublishAsync` goes to every service that listens (an event is something that happened, the publisher does not know who cares); `IEventBus.SendAsync(message, toService)` goes straight to the queue of one service, which is made if it is not there (a command asks one service to do
  something; a failed message is put back the same way). The `isExchangeEvent` flag is gone. Kafka has no place for a message to one service and refuses it.
- **Inbox and outbox are optional.** The event bus core knows neither; a service that needs them adds them as parts of their own (an inbox that makes a handler idempotent, an outbox that publishes with the change that caused the message), and one that does not carries nothing of them.

## Consequences

- A handler needs no `tenant.Change(...)`; the stock history, the shipment and the order the handlers write have the user who made the order.
- A message cannot be read by an older reader that expected `UserId` to be a free text (it is a guid now); the wire format is not kept for the previous framework.

## Related

[ADR-0023](0023-event-bus.md), [ADR-0037](0037-audit-soft-delete-and-tenants.md), [ADR-0045](0045-repository-levels-relational-and-document.md).
