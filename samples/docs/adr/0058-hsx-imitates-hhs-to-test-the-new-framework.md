# ADR-0058: Hsx imitates the hhs system to test that the new framework can do what the old one did

- **Status:** Accepted
- **Date:** 2026-11-01 (applies *framework ADR-0128*, *0129*, *0126*, *0125*)

## Context

The system the framework is for already exists: **hhs**, built on the old framework (branch `dev-backup`). The rewrite is only proven when what hhs does with the old framework can be done with the new one. A full port is not wanted; a copy that keeps hhs's *shape* is.

## Decision

- **Hsx** is a third sample (`hsx/Hsx.sln`): the **identity** and **content** services of hhs with their entities and a CRUD controller for each, one **auth server**, one **commercial gateway** and one **commercial web** (Razor Pages), with the shared projects of the other samples. Business that is
  not CRUD is not carried over.
- **Both services use PostgreSQL** (as hhs does), so Hsx exercises `EntityFrameworkCore.PostgreSql`; Abc uses SQL Server for identity.
- **Identity** (`Hsx.IdentityService`): `Tenant` (tree: `ParentId`/`AccessPath`), `AppUser` and `AppRole` and their links `AppUserRole`, `AppRoleSubscription` (tenant-isolated, `IMultiTenant`), and `Company`, `Customer`, `Subscription` (a product a reseller sold a customer; its scope key is `{customerId:N}:{product}`). Users sign in with the **email**
  (unique in the whole system).
- **Content** (`Hsx.ContentService`): `CustomerContent`, `AnalysisContent`, `AnalysisContentItem`, `CustomerContentVisit`, `CustomerVpSetting`, all **scope-isolated** (`IScopeSubscription`, the key is set by the caller, who must hold it: 403 `scope_not_allowed` otherwise).
- **What the token carries** (auth server): `tenant_id`, `allowed_tenant_id` (the tenants under the user's, at most 50), `allowed_scope_key` (the keys of the subscriptions of the user's roles, at most 50; more refuses the sign-in with 422 `token_too_large`), `is_system_tenant`; audiences `hsx-auth`, `hsx-gateway`,
  `hsx-identity`, `hsx-content`, issuer `hsx-auth`.
- **Demo data** (seeders, `Seeding:Disabled` turns them off): tenants System, Techsummus (reseller), Pixel (reseller), Acme (an account **under** Techsummus); one demo customer to which Techsummus sold the video platform and Pixel the vertical video; three demo contents under each key; an admin user for each tenant
  (`admin@techsummus.hsx.local`, `admin@pixel.hsx.local`, `admin@acme.hsx.local`, `system-admin@hsx.local`, the password `Hsx-Demo-123`).
- **`hsx/smoke-test.sh`** checks it through the gateway: the system tenant sees 4 tenants, 4 users, 6 contents; Techsummus 2/2/3 (itself and Acme, only its own product); Pixel 1/1/3 (the **same customer**, the other product); Acme 1/1/0.
- Ports in development: auth 7100, gateway 7200, web 7300, identity 7410, content 7450.

## What this tests (and the answer so far)

| In hhs, with the old framework | In Hsx, with the new one |
|---|---|
| Tenant tree with an access path | `TenantAccessPath`, `allowed_tenant_id`, one query filter (works, smoke test) |
| The scope key of a subscription on the data | `IScopeSubscription`, `allowed_scope_key`, query filter, `EnsureScope` on write (works) |
| Two resellers, one customer, different products | Techsummus and Pixel (works) |
| CRUD controllers over EF Core and PostgreSQL | repositories, `FilterBuilder`, `SortFields`, paging (works) |
| Database and data at start | `AddSchemaStartupStep`, `IDataSeeder` (works) |
| Email sign-in, one auth server | `ISignInRepository`, `TokenService` (works) |

Left out on purpose: the events between the services, the scheduler jobs, files and the rest of the hhs business.

## Consequences

- `check.sh hsx` covers it. Sample tests are written later, with the other samples' tests.
- Subscriptions, companies and customers are not isolated by tenant (a tenant sees every subscription a role may be given is checked against the reseller, `subscription_not_allowed`); isolating them by `ResellerTenantId` is the next step if hhs needs it.
