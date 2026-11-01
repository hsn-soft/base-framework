# ADR-0125: The data a database starts with is written by seeders, which are steps of the database

- **Status:** Accepted
- **Date:** 2026-11-01 (adds to [ADR-0118](0118-a-database-is-prepared-by-the-host-that-owns-it-one-pod-at-a-time.md))

## Context

The old system had one `IBasicDataSeeder` per service, registered by replacing a default, run by a loader after a delay, with no protection when a service had more than one pod (two pods seeded at once) and no way to leave it out of an
environment. This framework already prepares a database under the lock of its owner (ADR-0118), but the data a database starts with was written as lambdas and static methods passed to `AddDatabaseStartupStep`: no name to switch it off, no
constructor to take what it needs.

## Decision

- **`IDataSeeder`** (`HsnSoft.Base.Data`): `SeedAsync(CancellationToken)`; written to be run again (it checks what is there and adds what is not).
- **`AddDataSeeder<TSeeder>(connectionName)`** registers the seeder (scoped) and adds it as a step of that database: **the seed is the step**, so it has everything ADR-0118 gives a step: only the owner runs it, one pod at a time under the lock, tried again
  while the database is not there. Seeders run in the order they were added, after the schema step; each in a scope of its own, so it takes its dependencies in its constructor.
- **`Seeding:Disabled`** (a list of class names, case-insensitive) skips a seeder with an Information log: an environment that must not have the people of a sample says so in its settings, with no change of code.
- Schema work (collections, indexes) stays `AddSchemaStartupStep` / `AddDatabaseStartupStep`; a seeder is for data.

## Consequences

- A seed is a class that can be tested, named in the log, switched off by a setting and shared by no static state.
- Two pods of the owner never seed at once, and a second pod finds the work done.
- The samples' identity seeds are seeders (`IdentityUsersSeeder`; `TenantsSeeder`, `PermissionsSeeder`, `SampleIdentitySeeder`).

## Related

[ADR-0118](0118-a-database-is-prepared-by-the-host-that-owns-it-one-pod-at-a-time.md), [docs/DatabaseStartup.md](../DatabaseStartup.md).
