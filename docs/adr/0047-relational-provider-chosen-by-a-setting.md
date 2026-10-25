# ADR-0047: The relational provider is chosen by a setting

- **Status:** Accepted
- **Date:** 2026-10-25 (follows [ADR-0045](0045-repository-levels-relational-and-document.md))

## Context

A service registered its database in code (`AddBasePostgres<IdentityDbContext>`), so moving it to SQL Server meant changing the registration in the persistence project, and another project reference. The decision of the maintainer: a service on one relational database that moves to another
changes configuration, not the code of the domain or the application.

## Decision

- **A provider package says how its database is configured; the service says which it can run on; a setting says which one runs.** `IEfCoreProvider` (name, the name of its connection string, how it configures a context) is in `EntityFrameworkCore`; each provider package (`EntityFrameworkCore.PostgreSql`,
  `EntityFrameworkCore.SqlServer`) offers it with a `Use...` method on `EfCoreProviderOptions` (`UsePostgres()`, `UseSqlServer()`).
- **`AddBaseEfCore<TContext>(configuration, providers => providers.UsePostgres().UseSqlServer())`** registers the context on the provider that **`Database:Provider`** names (`postgres`, `sqlserver`; case does not matter), with that provider's connection string (`ConnectionStrings:Postgres`, `ConnectionStrings:SqlServer`, or the name given to
  `Use...`), the retry on transient failures, the concepts of the framework and a readiness check named after the provider. With one provider offered the setting may be left out; with several it is required, and a name the service does not offer is an error that lists what it does.
- **`AddBasePostgres` and `AddBaseSqlServer` stay** as the call of a service that runs on one database only; they go through the same path.
- **The persistence project of a service is `<Service>.EntityFrameworkCore` whichever provider runs,** and references the provider packages it offers. The Abc order and identity services offer PostgreSQL and SQL Server and set `Database:Provider` to `postgres`.
- **MySQL is not offered yet:** the provider of the community (Pomelo) has no release for EF Core 10, and the one of Oracle is under the GPL (with an exception for open source): a choice for the maintainer before a package is added.

## Consequences

- What can still differ between providers is the schema, not the code: the sample services create their tables with `EnsureCreated`; a service with migrations keeps one set of migrations for each provider it offers. The queries use the `Normalized...` twin of a text, not the collation of the database, so they behave the same everywhere.
- Moving to another provider: set `Database:Provider` and the connection string (and migrate the data).

## Related

[ADR-0045](0045-repository-levels-relational-and-document.md), [ADR-0042](0042-ddd-domain-and-application-packages.md).
