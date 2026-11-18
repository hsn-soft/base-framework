# ADR-0163: MongoDB transactions are a unit of work, and a write of many without one is all or none as far as a document store can

- **Status:** Accepted (extends [ADR-0146](0146-several-repository-calls-run-as-one-unit-of-work-through-iunitofwork.md) to MongoDB)
- **Date:** 2026-11-19

## Context

`IUnitOfWork` had only an EF Core implementation. MongoDB has transactions only on a replica set or a sharded cluster: some services run on one, some on a standalone server, so a transaction can be neither always on nor always off. The documented guarantee of `AddManyAsync` ("all are added or none") held for a relational store and not for a document store without a transaction.

## Decision

- **`MongoUnitOfWork`** (`AddMongoUnitOfWork()`) is `IUnitOfWork` for MongoDB: the work runs in one transaction, and every repository call it makes (reads too, so it sees its own writes) goes through its session. The session follows the async flow (`AsyncLocal`), so no repository takes a new parameter; work started in another flow (`Task.Run` with the flow suppressed) is outside it. A unit of work inside a unit of work **joins** the open transaction. The driver may run the work again on a transient error, so the work is one that can be run again, as for EF Core.
- **A service that runs on a standalone server does not register it**, and a call that needs a transaction (`MongoUnitOfWork`, or the three methods of `IMongoRepository` below) throws `NotSupportedException` that says the server is standalone.
- **`IMongoRepository` has the three many-methods in a transaction**: `AddManyInTransactionAsync`, `UpdateManyInTransactionAsync`, `DeleteManyInTransactionAsync`: all or none, whatever fails; inside a unit of work they join it. The plain `AddManyAsync`, `UpdateManyAsync` and `DeleteManyAsync` stay and need no transaction.
- **Without a transaction, a write of many is all or none as far as a document store can:**
  - `AddManyAsync` inserts in order; if one fails it **removes the ones it had added** (best effort) and rethrows.
  - `DeleteManyAsync` of documents that are removed for good **reads the keys first** (one read) and fails, naming the records, before it removes anything; soft deleted documents stay and are replaced.
  - `UpdateManyAsync` writes each document atomically and **cannot undo the others**: when one is not there or not current the others were written, and the exception names exactly the ones that were not. A transaction is the way to write all or none.
- **Change tracking for `NotModified`**: `FindAsync` keeps a copy of the document it read (in the repository, for as long as the entity lives); `UpdateAsync` compares and writes nothing when they are equal.

## Consequences

A service picks the guarantee it needs and its server allows. The tests of the transactions run on a one-member replica set in a container (`MongoReplicaSetFixture`). A collection is created outside a transaction (a transaction cannot create one).

## Related

ADR-0146, ADR-0162.
