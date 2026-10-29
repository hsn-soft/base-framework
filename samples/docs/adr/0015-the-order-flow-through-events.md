# ADR-0015: The order flow runs through events: order, product, shipment, order

- **Status:** Accepted (the events and the tenant handling changed: [framework ADR-0057](../../../docs/adr/0057-the-caller-of-a-message-and-publish-or-send.md); the names on the broker: [ADR-0018](0018-names-on-the-broker-in-the-abc-sample.md); where they live: [ADR-0019](0019-where-events-and-their-handlers-live.md))
- **Date:** 2026-10-25 (uses [ADR-0014](0014-more-than-one-store-in-a-service.md); the event bus is the one of the framework, RabbitMQ)

## Context

The services of the Abc sample had CRUDs and histories but did not talk to each other. The system should work as one when a customer (the mobile user, signed in at the auth server) makes an order: the stock is checked, the parcel is sent, the order shows how it ended, and every service keeps its own records.

## Decision

- **Four events** (records in `Abc.Shared.Events`, which only holds messages; every one carries the tenant of the order): `OrderCreatedEto` (order → product), `OrderStockCheckedEto` (product → order: set aside, or not and why), `ShipmentRequestedEto` (product → shipment, only when the stock was set aside)
  and `ShipmentCompletedEto` (shipment → order: delivered, or failed and why). The exchange is `abc.events`; each service reads one durable queue for each event it handles (`abc.events_<service>_<Event>`).
- **A handler is thin and in the application layer** (`EventHandlers/`), registered by the service's `Add<X>Application` with `AddEventHandler`; it runs in its own scope for each message. The rules are in the domain (`Order.ApplyStockCheck`, `Order.ApplyDelivery`, `Product.Reserve`) and in application services
  (`StockReservationService`, `ShipmentAppService`), which the handler calls as a controller would.
- **A message has no request, so the handler acts in the tenant of the message:** `ICurrentTenant.Change(message.TenantId)`; the query filters and the tenant of what it writes follow. Nobody is signed in, so the audit user of what a handler writes is empty (the user that started the chain is in the headers of the message).
- **A message that comes twice does no harm:** the stock of a line is taken once (its stock history record has a `Reference` of `order:{orderId}:{productId}`), the shipment of an order is made once and finished from the state it is in, and the order ignores an answer that is late or already applied.
- **The shipment is simulated:** the carrier delivers, unless the address says `fail`, which it cannot reach (the parcel is returned). An order whose stock was refused or whose delivery failed is cancelled, with the reason in its note.
- **What it does not do yet, on purpose:** no outbox (an order saved whose event was not published stays `Pending`), no compensation (the stock taken for an order whose delivery failed is not given back), no transaction over a service's databases, and the handling threads and scopes of a consumer are still to be discussed.

## Consequences

- One request to the order service leaves records in every store: the order (PostgreSQL), the stock and its history (MongoDB, two databases), the shipment (MongoDB) and its history (PostgreSQL), the login (PostgreSQL), and four queues that show what went through.
- An order now has a `ShippingAddress` (required); a database made before this change has to be dropped.

## Related

[ADR-0014](0014-more-than-one-store-in-a-service.md), [ADR-0013](0013-the-history-of-the-users.md), [ADR-0008](0008-the-abc-services-crud-pattern.md).
