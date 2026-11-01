# ADR-0057: Techsummus sees its customers through a tree of tenants

- **Status:** Accepted
- **Date:** 2026-11-01 (applies *framework ADR-0128*, see `../../../docs/adr/0128-the-tenants-that-a-tenant-sees-are-the-ones-under-it-by-a-path-of-ids.md`)

## Decision

- Edx has a fourth tenant, **Techsummus** (`techsummus`), and the tree: the system tenant and Techsummus are at the root, **Veriban and Bilgera are the customers of Techsummus**. A user of Techsummus sees its own data and the data of Veriban and Bilgera; a user of Veriban or Bilgera sees only
  its own; the system tenant sees every tenant.
- **`Tenant.ParentId` and `Tenant.AccessPath`** (`TenantAccessPath`), with indexes. A database that was made before them gets the columns from `TenantTreeColumnsSeeder`, the first seeder of the identity database (an idempotent `ALTER TABLE`, SQL Server, since `EnsureCreated` does nothing to a database that is
  there); `TenantsSeeder` makes the tenants and places a tenant that has no place, and leaves one an administrator moved. `ITenantRepository` finds the tenants under a tenant (`GetDescendantIdsAsync`) and moves a tenant with everything under it (`MoveAsync`, refusing a move under itself).
- **The auth server** writes `allowed_tenant_id` for the tenants under the tenant of the user (at most `Tenants:MaxAllowedPerToken`, 50, else the sign-in is refused with the reason), none for the system tenant; `GET /auth/me` shows them.
- Techsummus has the four sample users every tenant has (`Seed:Users` of each environment).
- Tried in Docker: Techsummus has `allowed` = Veriban and Bilgera and sees 12 users (its 4 and theirs); Veriban and Bilgera see their own 4; the system tenant sees 16.
