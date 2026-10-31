# ADR-0118: A database is prepared by the host that owns it, one pod at a time, under a lock of the database

- **Status:** Accepted
- **Date:** 2026-10-31 (adds to [ADR-0117](0117-the-health-endpoints-are-live-startup-and-ready-and-answer-with-the-checks.md), whose test showed the problem)

## Context

A host prepares its database when it starts: it creates it, its schema, and seeds what must be there. That was written for one process. Two pods of the same host started together both ran it: they both created the database, both looked whether the first user was there, and both inserted it. One of them failed with
a duplicate or a conflict, and the pod restarted (it did, in the test of a two-pod deployment of the identity service). With four pods, and with a database that a second host (a WCF host beside the REST host of a service) also uses, it gets worse: **who prepares a database** has to be decided, and **a pod that prepares it must not meet another**.

## Decision

- **A database has one owner host**, which is a decision of the code of the system and not of an environment: the setting `SeedStartupConnections`, in the `appsettings.json` of a host, lists the databases (the names of their connection strings, separated by semicolons) that the host prepares. **Absent**: all the ones it has (what a service with
  one host wants, and how it was). **Empty**: none. The other hosts of a database do not prepare it; their readiness checks tell when it is there, and the platform keeps them out of the traffic until then.
- **Of the pods of the owner, one at a time prepares it, under a lock of the database.** The lock is the database's own, so nothing else is needed (the framework `HsnSoft.Base.Data` has `IStartupLock`; each provider package brings its own):
  SQL Server `sp_getapplock` (taken on `master`, the database may not be there yet), PostgreSQL an advisory lock (on the maintenance database `postgres`), MySQL `GET_LOCK`, and MongoDB, which has no lock of a session, a lease: a document in the collection `_startup_locks` that the holder renews and deletes.
  A lock of a session is released when the connection closes, so a pod that dies gives it up; a lease ends by itself after a minute. The pod that gets the lock next finds the work done: a step is written so that running it again changes nothing.
- **The steps are part of the persistence of a service, and run when the host starts**: `AddDatabaseStartupStep(connectionName, name, step)` and, for a context of Entity Framework, `AddSchemaStartupStep<TContext>(connectionName)` are called in the registration of the persistence (`AddOrderPersistence`), and the `Program.cs` of a host writes nothing about them.
  One hosted service runs them, in the order they were added, each in a scope of its own, **in `StartingAsync`** (which the host calls on every hosted service before it starts any): the consumers of the event bus start after, whatever the order of the registrations, and a message never meets a table
  that is not there yet. Each step is a database step: `RunDatabaseStepAsync(connectionName, name, step)` is the same rules for a step that is run by hand (the owner, the lock, and the retry while the database is not there: taking the lock is part of the attempt, so a server that is not up yet is waited
  for); `EnsureDatabaseCreatedAsync<TContext>`, `RunContextStepAsync<TContext>` and `RunScopedContextStepAsync<TContext>` are its forms for a context. The persistence registrations (`AddBaseEfCore`, `AddBaseMongo`) tell startup about each database. Which host does a step is the setting; the code of the hosts of a service is the same.
- **What a step is** stays the code of the service (it knows what to seed); **who runs it and when** is the framework's.

## Consequences

- Four pods, or many hosts, can start together on an empty database: it is created and seeded once, and nothing restarts. The test starts four hosts at once against each of the four databases and looks for one row.
- A step that fails after it was tried again stops the host from starting, and the platform restarts the pod (before, the same exception ended `Program.cs`).
- A host that has to wait for the owner cannot be ready before it: that is what the readiness probe is for. A table that the owner is still creating can be asked for in that moment (the database answers the readiness check before its tables are there); the window is the seconds of a first start.
- A step that takes longer than the lease of MongoDB (a minute) is not a problem: the holder renews it; one that is stuck for good is given up after the timeout of the lock (two minutes), and tried again like any step.
- A schema that evolves needs migrations, as before; this decides who runs them and not what they are.

## Related

[ADR-0117](0117-the-health-endpoints-are-live-startup-and-ready-and-answer-with-the-checks.md), [docs/DatabaseStartup.md](../DatabaseStartup.md).
