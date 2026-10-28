# ADR-0008: The CRUD pattern of the Abc services: order, product and shipment follow the identity service

- **Status:** Accepted (the per-service `ICurrentUser` is replaced by the framework's, [framework ADR-0036](../../../docs/adr/0036-icurrentuser-in-the-framework.md); the audit fields, soft delete and tenants are the framework's, [framework ADR-0037](../../../docs/adr/0037-audit-soft-delete-and-tenants.md))
- **Date:** 2026-10-25 (completes the skeletons of [ADR-0005](0005-microservice-project-pattern.md))

## Context

The identity service was the pilot of the layers (domain, application, persistence, host). The order, product and shipment services were skeletons; they get their first domain and the same CRUD endpoints, with the same style, so that the
four services read alike and a fifth is a copy.

## Decision

- **Each service has one aggregate**, in a folder named after it in every layer (`OrderDomain`, `ProductDomain`, `ShipmentDomain`): `Order` (with its `OrderLine`s, kept in EF Core), `Product` and `Shipment` (kept in MongoDB, one collection each). The entities have private setters and
  factory methods that check the rules (a broken rule is a `DomainException`, answered 400); the domain has no attribute of a database.
- **The endpoints are the ones of the users:** `POST create`, `POST get/{id}`, `POST paged-list`, `POST filter-list`, `POST search-list`, `PUT`, `DELETE`, under `api/{service}-service/v1/commercial/{controller}`; the controller only calls the application service.
  Filters are parts of text compared without regard to case (a `Normalized...` property is kept for it) and exact values (an enum is sent as its name, any case).
- **The repository of a service is `Find`, `GetList`, `Count`, `Search`, `Add`, `Update`, `Delete`** (all asynchronous, each one saves): MongoDB has no unit of work, so the interface does not promise one; the EF Core repository saves in each call. (The identity service
  keeps its `SaveChanges` shape for now.)
- **MongoDB mapping is in the persistence project** (`BsonClassMap` per entity, registered by `AddXPersistence`): the domain stays free of the driver. Enums are stored as text, money as `Decimal128`. A startup step makes the indexes
  (`EnsureProductDatabaseAsync`: the sku is unique; `EnsureShipmentDatabaseAsync`: the tracking number is unique, the order is looked up).
- **Who did it:** every row has `CreatedBy` / `UpdatedBy`, the `sub` of the token, through an `ICurrentUser` of the application layer that the host answers from `HttpContext` (as in [ADR-0007](0007-the-auth-server-reads-the-users-of-the-identity-service.md)). An order's customer is the signed-in user.
- **Seed data only for the product service:** when its collection is empty, two products are added, `LAMP-001` with 10 in stock and `CHAIR-001` out of stock (0), written by `system`. The order and shipment services start empty.
- **The commercial gateway forwards the three services** (`/order-service/v1`, `/product-service/v1`, `/shipment-service/v1` to `/api/{service}-service/v1/commercial`), with their health routes and their Swagger documents.
- **Order and shipment status follow simple rules:** a status is sent as text; a delivered or cancelled order, and a delivered or returned shipment, cannot change its status; an order's lines cannot change once it is made.

## Consequences

- The services do not call each other: an order keeps the product's name and price it was made with, a shipment keeps only the id of its order, and nothing reserves stock yet. That is for the event flow, when the event bus gets a provider.
- Each service copies the small base DTOs (`PagedDataRequestDto`, ...) and `ICurrentUser`: the services are independent, nothing shared between them but `Abc.Shared.Hosting`.
- The tables of the order service are made by `EnsureCreated`: a database made before this change has to be dropped (`drop database abc_order`); the MongoDB collections need nothing.
- Who may create, change or delete is not decided yet: every signed-in user can.

## Related

[ADR-0005](0005-microservice-project-pattern.md), [framework ADR-0026](../../../docs/adr/0026-route-pattern-and-jwt-by-default.md), [ADR-0002](0002-gateways-per-audience.md), [ADR-0007](0007-the-auth-server-reads-the-users-of-the-identity-service.md), `samples/README.md`.
