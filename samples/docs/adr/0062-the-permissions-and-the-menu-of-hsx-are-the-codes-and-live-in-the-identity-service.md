# ADR-0062: The permissions and the menu of Hsx are in the code, and the identity service keeps them

- **Status:** Accepted
- **Date:** 2026-11-01 (applies [ADR-0061](0061-administration-is-part-of-the-identity-service-and-the-auth-server-reads-its-database.md); the model is the one of Edx, [framework ADR-0122](../../../docs/adr/0122-a-service-has-its-own-audiences-and-chooses-authentication-and-authorization.md) and `docs/Authorization.md`)

## Decision

- **The constants of `Hsx.Shared.Helper` are the only list of permissions**: `HsxPagePermissions` (`page.{page}.view`, the visibility of a page) and `HsxEndpointPermissions` (`endpoint.{resource}.{action}`, one for each action of a controller). The identity service keeps the table `Permission` in step with them at every start (new: added; gone: made
  inactive, never deleted). Operation and constraint permissions are not there: the system has no use for them yet, and a constraint with nobody to enforce it is what hhs left half built.
- **Every endpoint of the four services asks for its permission** (`[PermissionAuthorize]`; 69 actions): hhs had the attributes commented out, so its endpoints needed a valid token and nothing more. What the caller was granted is read by each service from the store the identity service writes (`IPermissionStore` on Redis,
  one entry for each role under the scope of its tenant, `AddHsxPermissions`); a role is found by its name, so renaming a role moves its entry.
- **The menu is in the code** (`MenuCatalog`), and the permissions of an item are in the same record, by what each is to it: `Visibility` (a `page.` permission: the item is in the menu and the page opens), `Required` (what the page reads with), `Action` (its buttons), `Operation`. The tables `Menu` and `MenuPermissionMap` are kept in step at every start;
  an item that is not in the code any more is made inactive. A tenant has no menu of its own: a menu that is added to the database only is made inactive at the next start, on purpose.
- **A role starts with what its name is granted** (`DefaultPermissionGrants`: `SystemAdmin` all of it, `Admin` all of it but making, changing, moving and deleting a tenant) when it is made, and a permission that is new is granted to the roles that name it once; what an administrator took away is never given back.
- **The grants of a role are one transaction** (`RolePermission`, replaced as a whole) and are written to the store at once; the identity service writes every role again when it starts and every `Permissions:SyncMinutes` (5), so a lost cache is made right. The services keep what they read for `Permissions:CacheSeconds` (30), so a
  grant reaches an endpoint of another service after that at most.
- `me/menus` and `me/permissions` answer for the caller and need no permission; `permissions/filter-list` and `app-roles/{id}/permissions` are the catalogue and the grants.

## Consequences

- A new service or page adds its constants, its controller's attributes and its items to the menu in one change; a permission that no menu item and no controller names is a smell the review looks for.
- The smoke test makes a role with nothing granted and shows the menu empty and every endpoint refused, then grants a page and a read and shows both open.
