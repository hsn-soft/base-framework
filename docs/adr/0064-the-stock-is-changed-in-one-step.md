# ADR-0064: The stock is changed in one step of the store

- **Status:** Accepted
- **Date:** 2026-10-26 (completes the set-based update of [ADR-0048](0048-bulk-and-set-based-changes.md) for MongoDB; applies to [ADR-0062](0062-a-kafka-consumer-for-every-service-and-event.md))
- **Note:** [ADR-0066](0066-kafka-handles-fetchcount-messages-at-the-same-time.md): Kafka runs `fetchCount` handlers at the same time too, so Xyz uses it (product 10, order 5).

## Context

A broker hands a service a message at least once, and RabbitMQ runs `fetchCount` handlers of an event at the same time. The stock reservation of the product services read the product, checked the stock, changed it in memory and saved it: with several handlers at once two orders
read the same stock and both took it. The code was right only because `fetchCount` was 1.

## Decision

- **The check and the change of a counter are one statement of the store.** EF Core had `UpdateWhereAsync` already; MongoDB gets the same through `IMongoRepository<TEntity, TKey>` (`UpdateWhereAsync(filter, set => set.Inc(...))`, audit fields written, the filters of the concepts applied; `AddMongoRepository`
  registers it beside `IRepository`).
- **The product services say what the domain needs in `IProductRepository`** (`TryTakeStockAsync`, `ReturnStockAsync`) and the persistence layer implements it: `UPDATE ... SET Stock = Stock - n WHERE Stock >= n` (PostgreSQL), `$inc` on the document that still has enough (MongoDB). The method of the
  entity (`Product.Reserve`) is gone: the rule lives where it can be atomic.
- **The reservation keeps its shape:** every line is checked, then each is taken with `TryTakeStockAsync` and recorded in the stock history; a line the history holds already is skipped. An order that loses a piece to another one between the check and the taking is rejected.
- The Abc product service runs `OrderCreated` with `fetchCount` 10 as the proof; Kafka reads one message at a time, so Xyz does not use more than one yet.

## Consequences

- Twenty orders at once for ten pieces end with ten delivered, ten cancelled, the stock at zero and ten history records, on both samples (tested on a real PostgreSQL and a real MongoDB too: twenty parallel `UpdateWhere` calls change a counter of ten exactly ten times).
- **Gaps that stay, on purpose (kept simple):** (1) an order of several lines that loses a later line to a competing order is rejected but the lines it took before are not given back; (2) two copies of one message handled at the same time can both pass the "was this line done?" check, as there is no unique
  key on the history; (3) the products and their history are two databases, so a crash between the taking and the recording leaves the stock taken without a record. Closing them needs a unique key with a compensation, or a transaction or an outbox.
- The stock before/after in a history record is what the code read before the change; the change (the quantity) is exact.

## Related

[ADR-0048](0048-bulk-and-set-based-changes.md), [ADR-0062](0062-a-kafka-consumer-for-every-service-and-event.md), [ADR-0023](0023-event-bus.md).
