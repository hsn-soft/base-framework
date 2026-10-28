# ADR-0083: Four permission types, the menu as data with its permissions, and constraints that must be given

- **Status:** Accepted
- **Date:** 2026-10-28 (changes [ADR-0081](0081-permissions-a-package-grants-in-the-cache-and-one-service-that-manages-them.md) and [ADR-0082](0082-a-new-permission-is-granted-to-the-sample-roles-when-it-is-added.md))

## Context

The permissions of ADR-0081 had three types and gave a page nothing to say what it needed. A back office needs more: a page a user may enter (and that the menu shows), what the page reads with, the buttons it offers, what it shows field by field, and the limits of the role.
Two things of the first design were also wrong for the business: a constraint that was not given meant "no limit" (a role without a limit could do most), and the value of a constraint sat in the same row as the grant of a permission that has none.

## Decision

- **`PermissionType` is `Page = 1`, `Endpoint = 2`, `Operation = 3`, `Constraint = 4`**, kept in the `Permission` table, and the code of a permission starts with the type in small letters (`page.invoice.view`, `endpoint.invoice.read`, `operation.invoice.price.view`,
  `constraint.invoice.max-view-amount`); the entity derives the type from the code, so the two cannot disagree. A code has three parts at least (`{type}.{resource}.{action}`). The page types were new; `Endpoint` (what ADR-0081 called the same) is what `[PermissionAuthorize]` asks.
- **A constraint must be given.** A caller whose roles have no grant of the constraint is refused with 403 **before anything of the business is done** (no query is made for it); the framework has `RequireValueAsync`, `RequireLongAsync`, `RequireDecimalAsync`, `RequireIntAsync` for it. The value, when there is one, is the filter or the limit of the business
  (the operator sees invoices up to 1 000, the manager up to 100 000, the administrator up to the largest number). The earlier "no value is no limit" is gone, with the places that took it.
- **The tables follow the model of the maintainer:** `RolePermission` (the role and the permission: a page, an endpoint or an operation), `RolePermissionConstraint` (the role, the constraint and its **value**), `Permission` (code, name, description, type, active). Both grant tables are `IMultiTenant` (the tenant of the role).
  **The sample no longer has grants to a user** (`UserPermission` is gone, with its endpoints): the framework still supports a user as a subject of a grant (`PermissionProviders.User`), and a system that wants it adds the table and writes the entries of its users.
- **The menu is data, in the identity service**, defined in the code (`MenuCatalog`) and kept in step at every start like the permissions: `Menu` (parent, code, title, route, icon, order, active, the channel `Mobile|BackOffice`, the dock `SidebarMenu|TopbarMenu`, the node `Caption|Node|Leaf`; a caption is only at the root, a leaf has a route) and
  `MenuPermissionMap` (the menu item, the permission and **what it is to the item**: `Visibility = 1`, `Required = 2`, `Action = 4`, `Operation = 5`, `Constraint = 6`). The relation is in the map and not in the permission because the same endpoint is required by one page and an optional action of another.
  **Every leaf has a visibility permission** (a `page.` permission): a leaf without one is a mistake of the code and the service does not start.
- **`POST me/menus/get` gives the menu of the caller**: only the leaves whose visibility permission the caller has, a node when something under it is shown, a caption when something after it is; each leaf comes with its other permissions and whether the caller has each. A caller who may see nothing gets an empty menu, and a client says so.
  The visibility permission concerns the client only: a page is not "protected" by it, the endpoints are, each by its own permission.
- **Dependencies between permissions** (granting one means granting others the code needs) are not built; they are to be discussed and decided.

## Consequences

- The database of the identity service of the sample was made again (the tables changed); the starting grants are given as ADR-0082 says, now for the four types and with the constraints in their own table.
- A permission that an endpoint asks for and a menu item names must exist as a constant: the catalogue and the menu are both made from the code, and the menu refuses (at start) a permission that is not in the catalogue.
- A client sees limits and masked fields as the service made them; what it shows or enables is the user's convenience and the service checks everything again.

## Related

[ADR-0081](0081-permissions-a-package-grants-in-the-cache-and-one-service-that-manages-them.md), [ADR-0082](0082-a-new-permission-is-granted-to-the-sample-roles-when-it-is-added.md), [ADR-0084](0084-the-edx-back-office-an-angular-app-that-asks-the-services-what-the-user-may-do.md), [docs/Authorization.md](../Authorization.md).
