# ADR-0084: The Edx back office: an Angular app that asks the services what the user may do

- **Status:** Accepted
- **Date:** 2026-10-28

## Context

The Edx system needs a screen for its people: the invoices, the invoice reports, and the management of users, roles and what the roles are granted. The old system had a large Angular back office. A new one for a sample should show how a client lives with the permissions of
[ADR-0083](0083-permission-types-the-menu-and-constraints-that-must-be-given.md), not repeat a template.

## Decision

- **`samples/edx/app/back-office` is an Angular 19 application** (standalone components, signals, lazy routes, no UI library: plain CSS), outside the .NET solution (it has no project file; `npm start` serves it on `http://localhost:4200`, `npm run build` builds it). Its texts are Turkish.
- **It talks to the gateway only** (`API_BASE`, `http://localhost:5600`): REST and JSON, the token in the header. The gateway answers the browser's origin (`GatewayPolicies:Cors:AllowedOrigins`, `http://localhost:4200` in development; none configured, no browser from another origin is answered).
- **Signing in** takes the tenant, the user name and the password (the auth server issues the token; it is kept for the tab in session storage). Then the **session** is loaded before any page: the menu (`me/menus/get`) and the permissions the user has (`me/permissions/get`, with the values of the constraints).
- **The screens decide nothing by themselves.** The sidebar is the menu the service gave, and has Home as its only item of its own; a route that is a menu item names its code and **`menuGuard` opens it only when the menu has it** (an address typed by hand to a page the user may not see goes to the forbidden page); a button is
  enabled when its permission is among the grants; what the user may not see arrives masked from the service (`••••`); the limits of the role are shown above the list. A user who has no page sees Home and the warning that it has no right to use any menu. All of it is for the user's convenience: **the services check every call again**.
- **The pages:** invoices (list, filters, make, change a draft, approve, delete), invoice reports (make, list, delete), users (list, make, change, delete, choose roles), roles (list, make, delete, and the editor of what a role is granted: the catalogue by type, a checkbox for a page, an endpoint or an operation, a checkbox and a value for a constraint; saving replaces the grants of the role as a whole).

## Consequences

- A change of the menu or of a permission is made in the identity service and needs no change of the client; a new page is a component, a route with its menu code, and a leaf in `MenuCatalog`.
- The client is not part of the detector or of the coverage gate; `ng build` is how it is checked, and its tests are written with the other sample tests.
- A back office in production is served by a web server of its own and its origin is put in the gateway's settings; the token in session storage is the sample's choice, not a recommendation for a product with other risks.

## Related

[ADR-0027](0027-gateways-per-audience.md), [ADR-0078](0078-the-edx-sample-a-file-intake-system.md), [ADR-0083](0083-permission-types-the-menu-and-constraints-that-must-be-given.md).
