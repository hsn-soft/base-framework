# ADR-0060: Customer, payment and an internal event in the shipment service

- **Status:** Accepted
- **Date:** 2026-10-25 (extends [ADR-0054](0054-the-order-flow-through-events.md); uses [ADR-0059](0059-where-events-and-their-handlers-live.md))

## Context

The shipment service sent a parcel as soon as the stock was set aside. A real order is paid first, and the service that takes the payment has to know whom it takes it from. The sample also needed an event that only one service handles, to show where those live.

## Decision

- **Two more entities in the shipment service, in its MongoDB** (collections `Customer` and `Payment`, mapped in `Abc.ShipmentService.MongoDB`), each with a full CRUD like the others (domain, application service, DTOs read by projection or `ToDto`, controller: `customers`, `payments`).
  A **customer** is unique by user name in a tenant; the service makes one for a user it does not know when an order of that user reaches it (`ICustomerAppService.EnsureAsync`: full name = user name, address of the order, the id of the user who is acting). A **payment** is made pending and ends `Succeeded` or `Failed` (a succeeded one can be
  `Refunded`; a failed or refunded one is final: `Payment.Succeed/Fail/ChangeStatus`); the CRUD can also make and correct one by hand.
- **The flow in the shipment service has two steps, joined by an internal event.** The external handler (`EventHandlers/Product/OrderStockReservedHandler`) makes the customer when there is none, takes the payment (`PaymentProcessor`: the provider is simulated and declines an address that says `nopay`; an order is paid once) and
  publishes `PaymentProcessedEto`, which only this service handles (`Application/Contracts/Events`, handler `EventHandlers/Internal/PaymentProcessedHandler`). The internal handler sends the parcel when the payment succeeded (the carrier delivers, unless the address says `fail`) and tells the order service the result in either case
  (`ShipmentCompletedEto`; `ShipmentId` and `TrackingNumber` are empty when no parcel was sent). The order is cancelled with the reason (`Not delivered: Payment failed: ...`).
- **The amount is the product service's:** `OrderStockReservedEto` carries the customer's user name and the total at the prices of the product service, so the payment does not depend on what the client sent.

## Consequences

- One order now leaves a record in a customer (once for a user), a payment, a shipment and its history, besides the order, the stock and its history.
- Three outcomes can be tried with the address of the order: any address (delivered), `nopay` (payment declined: no shipment), `fail` (paid, the carrier cannot reach it: returned).

## Related

[ADR-0054](0054-the-order-flow-through-events.md), [ADR-0059](0059-where-events-and-their-handlers-live.md), [ADR-0053](0053-more-than-one-store-in-a-service.md).
