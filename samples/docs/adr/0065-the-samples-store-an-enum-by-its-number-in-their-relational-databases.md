# ADR-0065: The samples store an enum by its number in their relational databases

- **Status:** Accepted
- **Date:** 2026-11-05

## Context

The EF Core models of the samples converted some enum properties to text (`HasConversion<string>().HasMaxLength(..)`: the order status of Abc, the permission, menu and role types of Edx, the tenant, role, subscription, permission and menu types of Hsx). The framework has now decided that a relational model stores an enum by its number (framework ADR-0148): a column of text is wider, slower to index and compare, and a renamed member would have to be migrated. A sample is the pattern a service is copied from, so it must not teach the opposite.

## Decision

No EF Core model of a sample converts an enum to text; the properties are mapped without a converter and without a max length. Every member of those enums already has its number written, and numbers are never reused. Hsx has migrations of databases that exist, so it gets a migration, `EnumsAsNumbers`, that converts the rows in both directions (the names to the numbers with `UPDATE .. CASE`, then `ALTER COLUMN .. TYPE integer USING ..`, written by hand because the provider does not write the `USING`). Abc and Edx make their tables with `EnsureCreated`, which does not change a table that is there: **a development database made before this change is dropped and made again.**

MongoDB class maps (Abc shipment, Edx services) keep their enum serializers; the decision is about relational stores.

## Consequences

Samples and framework agree. An old development database of Abc or Edx fails to read an enum column until it is recreated.

## Related

Framework ADR-0148 (`../../../docs/adr/0148-an-enum-is-stored-by-its-number-in-a-relational-database-never-by-its-name.md`), [ADR-0055](0055-the-identity-seeds-are-seeders.md).
