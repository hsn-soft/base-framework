# ADR-0058: The names on the broker in the Abc sample

- **Status:** Accepted
- **Date:** 2026-10-25 (changes the names of [ADR-0054](0054-the-order-flow-through-events.md))

## Decision

- **The exchange is `ABC`** (the name of the system in capitals, what a product names its exchange) and **the consumer client name of a service is its name in Pascal case**: `IdentityService`, `OrderService`, `ProductService`, `ShipmentService`. It is what `Producer` says in an envelope and what a message sent to one
  service names (`SendAsync(message, "ShipmentService")`).
- **The queues are `ABC_<Service>_<Event>`** (`ABC_ProductService_OrderCreated`, `ABC_ShipmentService_OrderStockReserved`); the routing key is the event name.
- The URL routes and the areas of the services (`order-service` in `/api/order-service/...`) are not names on the broker and stay as they are.

## Consequences

- The exchange `abc.events` and its queues (`abc.events_<service>_<Event>`) are not used any more; a broker that ran the previous names has them left over and they are deleted by hand.

## Related

[ADR-0054](0054-the-order-flow-through-events.md), [ADR-0057](0057-the-caller-of-a-message-and-publish-or-send.md).
