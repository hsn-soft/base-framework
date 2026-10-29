# ADR-0030: A new permission is granted to the sample roles when it is added

- **Status:** Accepted
- **Note:** [framework ADR-0083](../../../docs/adr/0083-four-permission-types-and-constraints-that-must-be-given.md): the starting grants cover four types and the constraints are in their own table.
- **Date:** 2026-10-27 (changes how the starting grants of [framework ADR-0081](../../../docs/adr/0081-permissions-one-framework-package-and-the-grants-in-the-distributed-cache.md) are given)

## Context

framework ADR-0081 gave the sample roles their starting grants once, on a database that had no grants. The invoices (every kind of permission at work) added permissions to a system whose database already existed: nobody was granted them, and the only way to try them was to drop the database.
A real system has the same need: a release adds permissions, and the roles that should have them from the start have to get them without taking back what an administrator took away.

## Decision

- **`EnsurePermissionsAsync` grants a permission to the roles `DefaultPermissionGrants` names it for at the moment it adds that permission to the catalogue**, and never again. The first start adds every permission, so it gives every starting grant; a later release that adds a permission gives only the grants of that one.
- A permission that was added before is left alone: a grant an administrator removed stays removed, and one added stays added.
- The rest of framework ADR-0081 stands: the table in `DefaultPermissionGrants` is a starting point of the sample, not a rule, and a real system's roles are managed through the endpoints.

## Consequences

- A developer who adds a permission and its default grants only restarts the identity service (the grants are written to Redis by the same start).
- A starting grant that is added to a permission that exists already is not given by this; it is a change to make through the endpoints (or by a new permission).

## Related

[framework ADR-0081](../../../docs/adr/0081-permissions-one-framework-package-and-the-grants-in-the-distributed-cache.md), [docs/Authorization.md](../Authorization.md).
