# ADR-0136: An entity is protected from a lost update by an opt-in stamp (`IHasConcurrencyStamp`), in EF Core and in MongoDB

- **Status:** Accepted
- **Date:** 2026-11-03

## Context

Two callers read the same record, both change it, the second save overwrites the first without a word. The old framework had an `IHasConcurrencyStamp` and an exception for it ([the analysis](../old-framework/1-foundation.md)); the rewrite did not bring either back, so nothing protects an edit today. The hhs system never used the old one, but Hsx has concurrent writers (a transfer of a sale, the subscriptions of a customer, the roles of a tenant) and its back office edits records that someone else may be editing.

## Decision

- **An entity opts in** by implementing `IHasConcurrencyStamp` (`HsnSoft.Base.Ddd.Domain`): `ConcurrencyStamp { get; private set; }`, started with `ConcurrencyStamps.New()`, and `SetConcurrencyStamp(string)`. An entity without it is not affected.
- **The stamp is the token of the save.** In EF Core `BaseDbContext` makes the property a concurrency token (40 characters at most) and `BaseEntityInterceptor` treats the stamp the entity carries when it is changed or deleted as the one the save is checked against, then gives a changed entity a new stamp. In MongoDB the repository replaces or deletes a document only where its stamp is the one the entity carries (and the bulk variants do the same); a replace gives a new stamp. A statement that changes rows without loading them (`UpdateWhereAsync`, the soft delete of `DeleteWhereAsync`) writes a new stamp too, so a reader from before is stale.
- **The caller gives back what it saw.** The DTO of the entity carries the stamp to the client; the update sends it back and the application service calls `SetConcurrencyStamp` with it before saving. Without one only a change made between the load and the save is found.
- **A conflict is a 409.** A row that is not as the entity saw it, whether changed or removed, is `ConcurrencyConflict.For(entityName)`: a `BaseHttpException` with 409 and the text `Concurrency:Conflict` (English, Turkish, Russian). `BaseDbContext` turns `DbUpdateConcurrencyException` into it; the Mongo repository throws it when nothing matched.

## Consequences

A record that people edit is protected by one interface and a DTO field; a service that does not need it pays nothing. The hard delete and the bulk statements of a stamped entity are part of the rule (a delete with a stale stamp is refused). `DeleteWhereAsync` on a hard-deleted entity cannot check stamps (nothing is loaded). A client has to send the stamp back for a check of what it saw; otherwise the protection is the narrow one of load to save. Retrying after a conflict is the client's job (read again, apply the change again); the framework does not merge.

## Related

[ADR-0037](0037-audit-soft-delete-and-tenants.md), [ADR-0045](0045-repository-levels-relational-and-document.md), [ADR-0048](0048-bulk-and-set-based-changes.md), [Entity concepts](../EntityConcepts.md), [the old framework analysis](../old-framework/README.md).
