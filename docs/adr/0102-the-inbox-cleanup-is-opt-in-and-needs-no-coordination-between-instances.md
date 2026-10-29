# ADR-0102: The inbox cleanup is opt-in and needs no coordination between instances

- **Status:** Accepted
- **Date:** 2026-10-28 (completes [ADR-0067](0067-the-event-inbox-is-opt-in-and-a-decorator.md), which left old rows to the service)

## Context

The inbox keeps one row for every message a service handles, for ever. A busy service adds millions of rows a month to a table that is read and written on the path of every message. A service runs as several pods, so a cleanup cannot be a job of "the" instance: there is no
leader, and the framework must not introduce one.

## Decision

- **The cleanup is a registration of its own, `AddEventInboxCleanup`** (EF Core: `AddEventInboxCleanup<TContext>(configuration)`; MongoDB: `AddEventInboxCleanup(collectionName, ...)`), as the inbox is ([ADR-0067](0067-the-event-inbox-is-opt-in-and-a-decorator.md), [ADR-0074](0074-multitenancy-is-a-package-of-its-own.md)). A service that
  does not call it has no worker and keeps its rows (or cleans them in its database with a job of the database). There is no switch in the settings that turns the registration off; `RetentionPeriod` of zero turns the cleanup off.
- **Settings** (`EventInboxCleanupOptions`, section `EventInbox:Cleanup`): `RetentionPeriod` (7 days), `Interval` (1 hour), `BatchSize` (1000).
- **Relational databases:** a `PeriodicBackgroundService` in every instance. It needs no leader and no lock: a run takes the ids of one batch of old `Completed` rows and deletes those rows with the condition checked again, so two instances that take the same ids delete each row once and the other deletes nothing. Batches keep one delete from being one long
  lock. `ConfigureEventInbox` adds an index on `Status, ProcessedAt` (the next migration of the service creates it).
- **MongoDB:** no worker. A TTL index on `ProcessedAt`, partial on `Status = Completed`, made at start (a database that is not there yet is waited for) and changed in place with `collMod` when the retention changes; the server removes the documents.
- **Only `Completed` rows are removed**: a started or failed row holds a lease or a retry count.
- **The retention must be longer than the broker keeps messages** (Kafka: 7 days by default, the default here): a copy that comes after its row was removed is handled again.

## Consequences

- The table stops growing in a service that asks for the cleanup, on every pod count, with no coordination and no leader.
- A migration is needed for the new index on relational services (the cleanup works without it, with a scan).
- A TTL index removes in the server's own pass (once a minute): a document can outlive its retention by about a minute.
- Tests: batches, only old handled rows, zero retention, two instances at once, the settings from configuration, no registration no worker (SQLite); the index, its change in place and the server removing only handled documents (a real MongoDB); the index on PostgreSQL and MySQL (the existing inbox tests build the model).

## Related

[ADR-0067](0067-the-event-inbox-is-opt-in-and-a-decorator.md), [ADR-0098](0098-a-message-another-handling-holds-is-put-back-not-acknowledged.md).
