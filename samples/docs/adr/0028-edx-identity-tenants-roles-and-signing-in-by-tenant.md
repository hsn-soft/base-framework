# ADR-0080: The Edx identity service: tenants, roles, and signing in by the code of the tenant

- **Status:** Accepted
- **Note:** [ADR-0081](0081-permissions-a-package-grants-in-the-cache-and-one-service-that-manages-them.md): the service now also holds the permissions, and "read only for now" below is true of users, roles and tenants.
- **Date:** 2026-10-27

## Context

The Edx system has tenants (the system itself and two customers, Veriban and Bilgera), users and roles. In Abc a user name is unique in the whole system and the auth server finds the user across tenants. Here every tenant has its own `admin`, so a name alone does not name a user, and a tenant must not see another's data.

## Decision

- **Four tables in SQL Server** (`Edx_Identity_*`), by the identity service, with the framework's multi-tenant context ([ADR-0074](0074-multitenancy-is-a-package-of-its-own.md)): `Tenant` (id, code, name, active; not `IMultiTenant`, it is what the tenants are), and `User`, `Role`, `UserRole` (all `IMultiTenant`, with the tenant filter and the tenant given on
  insert). **The user name is unique in a tenant** (unique index on tenant and normalized name), so is a role name; every tenant has roles of its own, and a user can only be linked to a role of its tenant.
- **The ids of the three tenants are constants of `Edx.Shared.Helper`** (`EdxTenants.System`, `Veriban`, `Bilgera`) and their codes (`system-tenant`, `veriban`, `bilgera`), so code that must name one does not look it up; the identity service seeds the rows from them.
- **Seed:** the six users, each with a role of its own name in its tenant: system `admin`, `user`; Veriban `admin`, `operator`; Bilgera `admin`, `manager`. The passwords are **configuration** (`Seed:Users` of `appsettings.development.json` only), never code, and nothing is made without that section.
- **Signing in names the tenant:** `POST auth-server/v1/auth/login` takes `{ tenant, username, password }`. The auth server reads the identity database (the same project, read only, as in Abc), finds the tenant by its code, then the user **of that tenant by its id** on purpose (nobody is signed in yet, so the tenant filter has no tenant), and issues the token with `tenant_id`, `role`
  claims and, for the system tenant, `is_system_tenant`. A wrong tenant, name, password, an inactive user or an inactive tenant are all one answer (401 `invalid_credentials`).
- **Isolation:** every request is held to the tenant of its token by the filter of the context (users, roles) and by the application service (tenants: a customer sees its own row). **The system tenant is exempt**: its token's `is_system_tenant` lifts the filters ([ADR-0074](0074-multitenancy-is-a-package-of-its-own.md)), so it sees every tenant's rows. The same holds in the
  intake and invoice services (MongoDB), and in the events (the tenant travels in the envelope).
- **The service is read only for now:** users, roles and tenants are listed and read; creating and changing them is not built.

## Consequences

- The auth server needs the identity service to have started once (it makes the database), as in Abc.
- A client that signs in must know its tenant code. The token carries the tenant, so nothing else asks for it.

## Related

[ADR-0029](0029-one-auth-server-issues-the-token.md), [ADR-0032](0032-the-auth-server-reads-the-users-of-the-identity-service.md), [ADR-0037](0037-audit-soft-delete-and-tenants.md), [ADR-0074](0074-multitenancy-is-a-package-of-its-own.md), [ADR-0078](0078-the-edx-sample-a-file-intake-system.md).
