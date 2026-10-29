# ADR-0017: Tables and collections are named like the entity

- **Status:** Accepted
- **Date:** 2026-10-25

## Context

The tables and the collections of the Abc sample had names in small letters with an underscore between the words (`users`, `order_lines`, `product_stock_history`), a convention of its own that neither the entity nor the mappings of the other samples follow.

## Decision

- **A table or a collection is named like its entity: in Pascal case, singular, the words joined** (`User`, `UserHistory`, `Order`, `OrderLine`, `Product`, `ProductStockHistory`, `Shipment`, `ShipmentHistory`). The name is a constant of the domain (`UserConsts.TableName`, `ProductConsts.CollectionName`, ...); the table prefix and schema
  (framework ADR-0050) and the collection prefix (ADR-0012) are put in front by the persistence project.

## Consequences

- A name like `User` or `Order` is a reserved word of SQL: EF Core quotes every identifier, so the mappings are right, and a query written by hand quotes them too (`select * from "Order"`).
- The tables were renamed in place of the old ones: a database made before this change has to be dropped (the sample databases are made by the services when they start).

## Related

[framework ADR-0050](../../../docs/adr/0050-a-context-has-a-properties-class-for-its-database.md), [ADR-0016](0016-environments-of-the-abc-sample.md).
