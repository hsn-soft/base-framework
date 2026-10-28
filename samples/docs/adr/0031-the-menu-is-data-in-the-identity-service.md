# ADR-0031: The permission tables of the identity service, and the menu as data with its permissions

- **Status:** Accepted
- **Date:** 2026-10-28 (changes [ADR-0029](0029-the-identity-service-manages-the-permissions.md) and [ADR-0030](0030-a-new-permission-is-granted-to-the-sample-roles-when-it-is-added.md))

## Context

The permissions of the framework have four types and a page type gives a back office what it needs to know about a page ([framework ADR-0083](../../../docs/adr/0083-four-permission-types-and-constraints-that-must-be-given.md)). The identity service of the sample keeps them, and a back office wants more than the codes: the menu a user may see, each page with the permissions it needs.

## Decision

- **The tables follow the model of the maintainer:** `RolePermission` (the role and the permission: a page, an endpoint or an operation), `RolePermissionConstraint` (the role, the constraint and its **value**), `Permission` (code, name, description, type, active). Both grant tables are `IMultiTenant` (the tenant of the role). **The sample no longer has grants to a user** (`UserPermission` is gone, with its endpoints):
  the framework still supports a user as a subject of a grant (`PermissionProviders.User`), and a system that wants it adds the table and writes the entries of its users.
- **The menu is data, in the identity service**, defined in the code (`MenuCatalog`) and kept in step at every start like the permissions: `Menu` (parent, code, title, route, icon, order, active, the channel `Mobile|BackOffice`, the dock `SidebarMenu|TopbarMenu`, the node `Caption|Node|Leaf`; a caption is only at the root, a leaf has a route) and
  `MenuPermissionMap` (the menu item, the permission and **what it is to the item**: `Visibility = 1`, `Required = 2`, `Action = 4`, `Operation = 5`, `Constraint = 6`). The relation is in the map and not in the permission because the same endpoint is required by one page and an optional action of another. **Every leaf has a visibility permission** (a `page.` permission): a leaf without one is a mistake of the code and the service does not start.
- **`POST me/menus/get` gives the menu of the caller**: only the leaves whose visibility permission the caller has, a node when something under it is shown, a caption when something after it is; each leaf comes with its other permissions and whether the caller has each. A caller who may see nothing gets an empty menu, and a client says so. The visibility permission concerns the client only: a page is not "protected" by it, the endpoints are, each by its own permission.

## Consequences

- The database of the identity service was made again (the tables changed); the starting grants are given as [ADR-0030](0030-a-new-permission-is-granted-to-the-sample-roles-when-it-is-added.md) says, now for the four types and with the constraints in their own table.
- A permission that an endpoint asks for and a menu item names must exist as a constant: the catalogue and the menu are both made from the code, and the menu refuses (at start) a permission that is not in the catalogue.
- A client sees limits and masked fields as the service made them; what it shows or enables is the user's convenience and the service checks everything again.

## Related

[ADR-0029](0029-the-identity-service-manages-the-permissions.md), [ADR-0030](0030-a-new-permission-is-granted-to-the-sample-roles-when-it-is-added.md), [ADR-0032](0032-the-edx-back-office-an-angular-app-that-asks-the-services-what-the-user-may-do.md), [../Authorization.md](../Authorization.md).
