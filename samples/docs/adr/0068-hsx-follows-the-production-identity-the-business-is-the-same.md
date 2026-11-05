# ADR-0068: Hsx follows the production identity: the business that is in both is the same

- **Status:** Accepted
- **Date:** 2026-11-05

## Context

Hsx was written to test the framework against the whole hhs system. The production system is now rebuilt on the framework in its own repositories (`hhs-shared`, the identity service), and Hsx is its sample: what Hsx has of the business must be what the production system has, so a reader (and a service copied from it) meets one design. The identity of Hsx was the older shape: permission codes as text constants in a helper, a password given when a user is made, the email unique in the whole system, an `IsDefault` role, a stored scope key, a repository that normalizes what it is given, services that answer entities in places.

## Decision

- **The identity service of Hsx is the identity service of the production system, with the names of Hsx** (`Hsx.IdentityService.*`, `Hsx.Shared.*`): the domain pattern (consts, sorting, exceptions in one file, enums by number, normalized fields never empty and unique in their set), the permission catalog made from the codes in `Hsx.Shared.Contracts/Permissions` (pages, endpoints, operations, constraints with a value), a role that starts with the grants of its type, the temporary password and its events (`UserCreatedEto`, `UserPasswordResetEto`), the tenant tree and the sales as real sales given to the roles that cover them, DTOs one per file answered by thin controllers, `IUnitOfWork`, and the seeders. What the production system has by name or by real data is **not** copied: the real companies, customers and users are not in the sample; the sample's own data (the tenants of `HsxTenants`, the company `ilbak`, the customer `cnbce.com`, the roles and the users of the demo) is in the same seeders.
- **The sign-in names the tenant** (`tenant`, `account`, `password`; the account is an email or a user name, normalized in the application layer, production auth server ADR-0014). The auth server of Hsx reads the identity database through `IAppUserRepository`, `IAuthRefreshTokenRepository` and `IAuthPasswordPolicyRepository`, and calls `AddServiceEfCoreDatabaseConfiguration` only: the identity host adds `AddServiceEfCoreDatabaseStartup` (it owns the database).
- **The shared libraries of Hsx follow the production ones**: the permission codes and the events are in `Hsx.Shared.Contracts`, `Hsx.Shared.Hosting.EventBus` is its own project, and `AddHsxPermissions` adds the tenant claims (ADR-0067, hhs-shared ADR-0019 to ADR-0021).
- **The one deviation, for a sample with no notification service**: `Identity:TemporaryPassword:SamplePassword` makes every temporary password the demo password, so the users the API makes can sign in and the smoke test can use them; a real system never sets it.

## Consequences

The routes are the same, but the contract changed: the permission codes moved (`IdentityPermissions.Endpoints.Tenants.Read` and so on), a user or a reseller is made without a password, `PUT app-users/password` is `PUT app-users/temporary-password`, a sign-in carries a tenant, a subscription has no `scopeKey` (it is made from the customer and the product), and a role starts with its type's grants. `smoke-test.sh`, `demo-scenario.sh`, the Postman collection and the web login follow it. **A development database made before this change is dropped and made again (the identity database has a new `InitialCreate`; the content database has a migration that adds the checks of the normalized fields).**

## Related

[ADR-0065](0065-the-samples-store-an-enum-by-its-number-in-their-relational-databases.md), [ADR-0066](0066-the-samples-follow-the-rules-of-the-normalized-fields.md), [ADR-0067](0067-the-shared-hosting-of-a-sample-is-in-layers.md), [ADR-0062](0062-the-permissions-and-the-menu-of-hsx-are-the-codes-and-live-in-the-identity-service.md), [ADR-0064](0064-who-owns-a-record-in-hsx-tenant-scope-neither-and-how-a-sale-moves-between-resellers.md).
