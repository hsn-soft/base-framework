# ADR-0048: Many entities at once, and changes made without loading

- **Status:** Accepted
- **Date:** 2026-10-25 (extends [ADR-0045](0045-repository-levels-relational-and-document.md))

## Context

The repository changed one entity at a time. Importing a thousand rows, closing every open order of a customer or deleting everything that matches a filter meant a loop of loads and saves. The old framework had bulk methods on top of a commercially licensed package, only on the relational side, and its
set based changes skipped the audit fields and the soft delete: a statement that does not go through the change tracker (EF Core) or through a replace of the entity (MongoDB) does none of what those do.

## Decision

- **The common contract has the many and the filter:** `AddManyAsync`, `UpdateManyAsync`, `DeleteManyAsync(entities)` (one save, one round trip or as few as the store needs; the concepts apply as to one entity) and `DeleteWhereAsync(filter)` (every match, without loading them, the count back).
- **A change that loads nothing writes what the tracker would have written.** `DeleteWhereAsync` on an `ISoftDelete` entity marks it (`IsDeleted`, `DeletedAt`, `DeletedBy`, and `UpdatedAt`/`UpdatedBy` if it is audited), as `DeleteAsync` does; an entity that cannot be soft deleted is removed. The time and the user come from the same place
  as for an entity (`IAuditPropertySetter.Stamp()`). What the caller may not see (the deleted, the other tenants') is not matched.
- **What only a relational store does is on its interface:** `IEfCoreRepository.UpdateWhereAsync(filter, setters)` (`ExecuteUpdate`: the properties named, in one statement), which also writes the audit fields of the change. What only a document store does (one field, upsert, ...) comes with `IMongoRepository`.
- **No bulk copy package.** `AddRange` is batched by EF Core and `InsertMany`/`BulkWrite` by the driver; the packages that do more (a temporary table, `COPY`) are licensed commercially or are provider specific, and can be a separate package if a service ever needs a million rows.

## Consequences

- A loop of loads and saves becomes one call; a bulk delete of soft-deletable entities still leaves the rows, marked, as a single delete does.
- `UpdateWhereAsync` does not refresh entities that the context already tracks, and does not run the domain methods of the entities: a rule that matters belongs in the filter or in a loop that loads the entities.

## Related

[ADR-0045](0045-repository-levels-relational-and-document.md), [ADR-0037](0037-audit-soft-delete-and-tenants.md).
