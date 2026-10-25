# ADR-0059: Where the events and their handlers live

- **Status:** Accepted
- **Date:** 2026-10-25 (changes the places of [ADR-0054](0054-the-order-flow-through-events.md))

## Decision

- **An event that more than one service handles (external) is in `Abc.Shared.Contracts`,** folder `Events/<the service that publishes it>` (`Events/Order/OrderCreatedEto.cs`, `Events/Product/OrderStockReservedEto.cs`, `Events/Shipment/ShipmentCompletedEto.cs`): one file for an event (the small types it carries
  go with it), namespace `Abc.Shared.Contracts.Events.<Service>`. The project only has messages; no logic. (`Abc.Shared.Events` was renamed `Abc.Shared.Contracts`; the shared project is where other contracts go too.)
- **An event that only the service that publishes it handles (internal) is in the Application project of that service,** `Contracts/Events` (the contracts folder of the service, as the DTOs and the application service interfaces are). There is none in the sample yet.
- **The handlers are in the Host project of the service that handles the event,** `EventHandlers/<the service that publishes the event>` (the order service has `EventHandlers/Product` for what the product service publishes and `EventHandlers/Shipment`), and `EventHandlers/Internal` for the internal events.
  A handler is thin: it calls the application services and the domain, which stay in their projects. The host registers them by scanning its assembly (`AddEventHandlers(typeof(EventHandlersAssemblyMarker).Assembly)`), so a new handler needs no line of registration.

## Consequences

- Whoever reads a service finds what it consumes by the folders of `EventHandlers`, and what a service publishes in its own folder of `Events`.
- A service that handles an event publishes nothing back through its application layer: the handlers are the only code that knows about the broker's side of a flow.

## Related

[ADR-0054](0054-the-order-flow-through-events.md), [ADR-0057](0057-the-caller-of-a-message-and-publish-or-send.md), [ADR-0030](0030-microservice-project-pattern.md).
