# ADR-0011: The Abc services' databases, and a project references only the provider it uses

- **Status:** Accepted (the settings of the identity databases changed: [framework ADR-0050](../../../docs/adr/0050-a-context-has-a-properties-class-for-its-database.md))
- **Date:** 2026-10-25 (changes the database of the identity service of [ADR-0007](0007-the-auth-server-reads-the-users-of-the-identity-service.md); uses [framework ADR-0047](../../../docs/adr/0047-relational-provider-chosen-by-a-setting.md))

## Context

The identity service ran on PostgreSQL like the order service. The maintainer wants the sample to show both relational providers: the identity service on SQL Server and the order service on PostgreSQL. While the provider became a setting (framework ADR-0047) the persistence projects were given both provider
packages; a service that runs on one provider should not carry the other.

## Decision

- **Identity: SQL Server** (`abc_identity`, `ConnectionStrings:SqlServer`, `Database:Provider` `sqlserver`); **order: PostgreSQL** (`abc_order`, `ConnectionStrings:Postgres`, `Database:Provider` `postgres`); product and shipment stay on MongoDB. The auth server reads the identity database, so it has the same setting and
  connection string as the identity service.
- **A persistence project references the provider packages of the databases it offers, and nothing else:** `Abc.IdentityService.EntityFrameworkCore` the SQL Server package, `Abc.OrderService.EntityFrameworkCore` the PostgreSQL one. The auth server does not reference a provider at all: the identity persistence project brings it.
  Offering a second provider later is a reference and one more `Use...` call.

## Consequences

- A database made by the identity service on PostgreSQL is not read any more: the services start on a new SQL Server database (the sample seeds its two users again).
- The provider of a service is still a setting: the project that offers one provider reads the setting only to refuse a provider it does not offer.

## Related

[framework ADR-0047](../../../docs/adr/0047-relational-provider-chosen-by-a-setting.md), [ADR-0007](0007-the-auth-server-reads-the-users-of-the-identity-service.md), [ADR-0010](0010-the-persistence-project-registers-its-database.md).
