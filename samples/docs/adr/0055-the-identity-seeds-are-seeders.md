# ADR-0055: The identity seeds are seeders

- **Status:** Accepted
- **Date:** 2026-11-01 (applies *framework ADR-0125*, see `../../../docs/adr/0125-the-data-a-database-starts-with-is-written-by-seeders-that-are-steps-of-the-database.md`)

## Decision

- Abc's `IdentityUsersSeeder` (the two users, with the tenants of `AbcTenants`) and Edx's `TenantsSeeder`, `PermissionsSeeder` and `SampleIdentitySeeder` (in this order: the roles are given their grants when they are made, so the
  permissions come first) are `IDataSeeder`s added with `AddDataSeeder` after the schema step. The logic is the one they had; it takes its dependencies in a constructor instead of asking a service provider.
- The people of the samples (and their passwords) are for a development database: the `uat` and `production` settings of a system that is not a sample list `IdentityUsersSeeder` / `SampleIdentitySeeder` in `Seeding:Disabled`.
- The collections and indexes of the Mongo services stay startup steps (schema, not data).
