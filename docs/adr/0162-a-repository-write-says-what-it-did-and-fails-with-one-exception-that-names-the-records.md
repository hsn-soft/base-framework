# ADR-0162: A repository write says what it did and fails with one exception that names the records

- **Status:** Accepted (amends [ADR-0136](0136-an-entity-is-protected-from-a-lost-update-by-an-opt-in-stamp.md): a record that is not there is a 404, not a 409)
- **Date:** 2026-11-19

## Context

The writes of `IRepository` returned nothing. A service that answers a client with the record it just wrote read it again (a second round trip), could not tell an update that changed nothing from one that did, and could not tell a record that is gone from one that someone else changed: both were one `BaseHttpException(409)` that named only the entity type. For a write of many, one missing record failed the write and the client was not told which. A key could come from the caller or from the store, with no rule for when.

## Decision

- **`AddAsync` returns the entity it was given** (`Task<TEntity>`): the same instance, now with what the store wrote (the key, the audit fields, the stamp, the tenant), so a caller that answers with it needs no second read. A DTO is made from it in the application layer (`EntityProjection` applies the same rules to an entity in memory as to a query); the domain never knows a DTO.
- **A key comes from the caller or from the store.** An entity whose key is empty (`Guid.Empty`) gets a version 7 `Guid` from the repository (`EntityKeys.EnsureKey`, through `IKeyAssignable`, implemented explicitly by `Entity<TKey>` so only the persistence reaches it); a key the caller gave is never changed; another type of key (a number) is made by the database.
- **`UpdateAsync` returns a `WriteOutcome`**: `Modified`, or `NotModified` when nothing in the entity differs from what was read (nothing is written, the stamp and the audit fields stay): not an error, the caller decides what it means. `UpdateManyAsync` returns a `WriteResult(Requested, Modified)`. Only an entity that `FindAsync` returned is followed (EF Core: the change tracker; MongoDB: a copy of what was read); one that was not (the entities of a list too) is saved whole and is always `Modified`. A record that is gone is found only when something is written.
- **One exception: `RepositoryException`** (`HsnSoft.Base.Domain.Repositories`, a `BaseHttpException`): `Failure` is `NotFound` (404, `entity_not_found`, key `Repository:NotFound`), `ConcurrencyConflict` (409, `concurrency_conflict`, key `Concurrency:Conflict`) or `DuplicateKey` (409, `duplicate_key`, key `Repository:Duplicate`), `EntityName` names the type and `Ids` **every** record that failed (not only the first), also in `Data` (`entity`, `ids`) which the client reads, so a screen can show which row of a list failed. The text is in the three languages of the framework. A soft deleted record is gone to the caller. An application service that wants another answer catches it and throws a business exception of its own. `BaseHttpException` is no longer sealed and has an `ErrorCode` the mapper puts in the response.
- **A write of many fails as a whole when one record is not there or not current**, and says which: a relational store writes nothing (one `SaveChanges` is one transaction: it is rolled back); what a document store does is [ADR-0163](0163-mongodb-transactions-are-a-unit-of-work-and-a-write-of-many-without-one-is-all-or-none-as-far-as-a-document-store-can.md).
- A `DbUpdateConcurrencyException` is asked once what the database holds (only on the path that fails), to tell `NotFound` from `ConcurrencyConflict`.
- **A write that would break a unique key or index is a `DuplicateKey`**, with the name of the key or index in `Constraint` (also in `Data`, as `constraint`) and what the store threw as the inner exception; **the value that is duplicated is never told**. EF Core reads it from PostgreSQL (state `23505`, the constraint), SQL Server (2601 and 2627, the index in the text), MySQL (1062, the key) and SQLite (the columns) without referencing their drivers; MongoDB from the duplicate key error (11000, the index). It is translated by the repositories only: the inbox and the startup lock still see the error of the store, since they act on it. A service still asks the repository first where a rule needs the answer before the write ([ADR-0150](0150-a-normalized-field-is-never-empty-is-unique-in-its-set-and-is-checked-before-the-database.md)), and this is the net under it when two callers race.

## Consequences

A service answers a create or an update with the entity it has, in one round trip. `Task` to `Task<TEntity>` is source compatible for a caller that ignores the result; a fake of `IRepository` returns the new types. The exact type of the exception of a missing record changes (`BaseHttpException(409)` to `RepositoryException`, 404): a caller that caught the status 409 for a missing record catches 404 now.

## Related

ADR-0042, ADR-0045, ADR-0136, ADR-0146, ADR-0163.
