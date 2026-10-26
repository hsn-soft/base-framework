# ADR-0067: The event inbox is opt-in and a decorator of the dispatcher

- **Status:** Accepted
- **Date:** 2026-10-26 (follows [ADR-0003](0003-abstractions-separate-from-providers.md); the inbox of the previous framework was a base class every handler inherited, [ADR-0023](0023-event-bus.md))

## Context

A broker delivers at least once, so a service that must not do its work twice needs to remember the messages it has handled. In the previous system every handler inherited a base class that did it (an inbox table, a `Started/Completed/Failed` status, a reclaim of a failed or abandoned row), shared by
the services and implemented per database. Two problems: a service that did not need it still carried it, and the first step swallowed *every* exception of the insert and answered "do not handle": a database that was down made the message be acknowledged and lost.

## Decision

- **The inbox is a decorator of the dispatcher, opt-in per service.** `AddEventInbox<TContext>()` registers the store and says it is enabled; the dispatcher then asks the inbox before the handler (`TryStartAsync`) and tells it how the handling ended (`CompleteAsync` when handled or when the
  application said no, `FailAsync` with the reason when it failed after its attempts). A handling cut short by a shutdown tells nothing: its row stays `Started` and is taken again when its lease has run out. A service without `AddEventInbox` has no inbox and no cost. Handlers do not change and do not know it.
- **The contract is in `EventBus`** (`IInboxStore`, `InboxMessage`, `EventInboxOptions`, the dispatcher's use of it); **the implementation is a provider package**, `EventBus.EntityFrameworkCore` (`EfCoreInboxStore<TContext>` on the context of the service, `ConfigureEventInbox`, `AddEventInbox`), as the providers of the brokers
  are ([ADR-0003](0003-abstractions-separate-from-providers.md)); the same for MongoDB, `EventBus.MongoDB` (`MongoInboxStore` on a collection of the database of the service, `AddEventInbox(collectionName)`), with the same decisions, a conditional update instead of the `UPDATE ... WHERE`.
- **How a message is taken:** insert its row (the id of the message is the key). If the insert fails and the row is **not** there, the failure is thrown and the delivery fails like a failed handler: the message is not dropped because the inbox could not be asked. If the row is there: `Completed` is
  `AlreadyHandled`; `Failed` under `MaxRetryCount`, or `Started` past `ProcessingLease`, is taken again by one `UPDATE ... WHERE` (so of any number of deliveries at once, one wins); otherwise `InProgress` or `RetryLimitReached`. No provider-specific upsert and no duplicate-key detection is needed.
- **The retry stays in the services.** The inbox only lets a failed message be handled again when it comes back (the error client puts it back) and counts it; whether an error is worth retrying, the delays and the workers that retry an operation are the service's own.
- **In the Abc sample:** the identity service (SQL Server), the order service (PostgreSQL) and the product service (MongoDB) use the inbox (a table or collection `InboxMessage`, in their own databases); the shipment service does not, and the Xyz system does not use it at all.

## Consequences

- A message that comes twice is handled once in a service with an inbox (tested on a real PostgreSQL and a real MongoDB: of twenty deliveries at once one is taken; live, three copies of `OrderStockReserved` are left alone by the order service while the shipment service, which has no inbox, handles them again, and three copies of `OrderCreated` are left alone by the product service: the stock and the history stay as they were).
- **What it is not:** the row of the inbox and the changes of the handler are two transactions. A handler that crashes after its own commit and before `Completed` is run again once the lease has run out, so a handler must still stand a redelivery; making it exactly-once needs the row in the same transaction as the changes (the
  outbox discussion).
- A handling that really takes longer than `ProcessingLease` can be taken over by a redelivery: the lease must be longer than the slowest handler.
- The table grows: old `Completed` rows are for the service to clean up.

## Related

[ADR-0003](0003-abstractions-separate-from-providers.md), [ADR-0023](0023-event-bus.md), [ADR-0065](0065-the-consumers-outlive-their-own-failures.md), [ADR-0064](0064-the-stock-is-changed-in-one-step.md).
