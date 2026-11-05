# ADR-0131: A schema that evolves is migrated by a startup step

- **Status:** Accepted
- **Date:** 2026-11-01 (adds to [ADR-0118](0118-a-database-is-prepared-by-the-host-that-owns-it-one-pod-at-a-time.md))

## Context

ADR-0118 made the schema a startup step (`AddSchemaStartupStep`, `EnsureCreated`) and said that a schema that evolves needs migrations, deciding only who runs them. There was no step for it. `EnsureCreated` makes the tables of a database that is not there and adds nothing to one that is, so the first
service whose model changed after its database was made (the Hsx services, which take the business of hhs one piece at a time) would have had to drop its databases, or run `MigrateAsync` by hand in its own code, as hhs did, without the lock of ADR-0118.

## Decision

- **`AddMigrationStartupStep<TContext>(connection)`** is a database step like the schema step: it runs `Database.MigrateAsync` on the host that owns the database, one pod at a time under the lock of the database, tried again while the database is not there. **`MigrateDatabaseAsync<TContext>()`** is the same by hand. It makes the
  database when it is not there, so a service uses it **instead of** the schema step, never with it.
- **The migrations are the service's**, in the project of its context, committed with the change that needs them. The framework does not generate or hide them.
- A database made by `EnsureCreated` has no history of migrations, so its first migration fails on a table that exists: it is made again, or its `__EFMigrationsHistory` is filled once by hand. The framework does not guess.
- The schema step stays for a service whose schema does not change (a sample of one table, a test).

## Consequences

- A service chooses, in its persistence registration, between a schema made once and a schema that evolves; the host's `Program.cs` does not change.
- A model that changed without a migration stops the host at start (EF compares the model with the snapshot), which is what is wanted.
- Tested with SQLite and a migration of the test project: applied under the lock, applied once, not run on a database the host does not own, added to the registration.
