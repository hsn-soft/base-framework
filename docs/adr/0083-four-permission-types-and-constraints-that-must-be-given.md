# ADR-0083: Four permission types and constraints that must be given

- **Status:** Accepted
- **Date:** 2026-10-28 (changes [ADR-0081](0081-permissions-one-framework-package-and-the-grants-in-the-distributed-cache.md))

## Context

The permissions of ADR-0081 had three types and gave a page nothing to say what it needed. A client (a back office, say) needs more: a page a user may enter, what the page reads with, the buttons it offers, what it shows field by field, and the limits of the role.
Two things of the first design were also wrong for the business: a constraint that was not given meant "no limit" (a role without a limit could do most), and the value of a constraint sat in the same row as the grant of a permission that has none.

## Decision

- **`PermissionType` is `Page = 1`, `Endpoint = 2`, `Operation = 3`, `Constraint = 4`**, and the code of a permission starts with the type in small letters (`page.orders.view`, `endpoint.orders.read`, `operation.orders.price.view`, `constraint.orders.max-view-amount`); the entity derives the type from the code, so the two cannot disagree. A code has three parts at least (`{type}.{resource}.{action}`).
  The page type is new; `Endpoint` (what ADR-0081 called the same) is what `[PermissionAuthorize]` asks.
- **A constraint must be given.** A caller whose roles have no grant of the constraint is refused with 403 **before anything of the business is done** (no query is made for it); the framework has `RequireValueAsync`, `RequireLongAsync`, `RequireDecimalAsync`, `RequireIntAsync` for it. The value, when there is one, is the filter or the limit of the business
  (a limit on what a role may see, say). The earlier "no value is no limit" is gone, with the places that took it.
- **Where a constraint's value is kept is the product's:** a grant of a permission that has no value and a grant of a constraint, which has one, are kept apart (a table each), so a value never sits in the row of a grant that has none.
- **Dependencies between permissions** (granting one means granting others the code needs) are not built; they are to be discussed and decided.

## Consequences

- A permission that an endpoint asks for must exist as a constant: the catalogue is made from the code.
- A client sees limits and masked fields as the service made them; what it shows or enables is the user's convenience and the service checks everything again.

## Related

[ADR-0081](0081-permissions-one-framework-package-and-the-grants-in-the-distributed-cache.md), [docs/Authorization.md](../Authorization.md).
