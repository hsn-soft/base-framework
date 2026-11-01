# ADR-0061: Administration is part of the identity service, and the auth server reads its database

- **Status:** Accepted
- **Date:** 2026-11-01 (extends [ADR-0060](0060-the-hhs-rewrite-in-hsx-the-order-and-what-is-decided.md); the auth server part keeps [ADR-0007](0007-the-auth-server-reads-the-users-of-the-identity-service.md))

## Decision

- **The administration service of hhs is not a service of Hsx**: its permissions, the permissions of the roles, the menu and the permissions of a session are part of the **identity service**, in its database and its context. The reason is the data: a role and the permissions of that role are one thing (hhs keeps the role in one
  database and its permissions in another, with a role id that is only a promise, which is how the two models of permissions came apart), and a change of both is one transaction. The grants are written to the permission store of the framework (`IPermissionStore`) by the identity service itself, so the event that told every
  service to reload them and the worker that reloaded them are not needed ([Authorization.md](../../../docs/Authorization.md)).
- **The auth server reads the database of the identity service directly** (the persistence project of the identity service is a library of the auth server, as in hhs), and only reads it: the schema, the migrations and the seed are the identity service's.
- What of the sign-in hhs stores but never uses is used or left out: the **password policy** is enforced (a user is made or changes its password under the rules of its tenant, and its failed logins lock it), the **refresh token** (a hash, rotated on use) and the **login audit** are kept; the token revocation and the email confirmation are left out until a use case needs them; the **client credentials** grant and the list of clients (hard-coded in hhs for Swagger and a test console) wait for a client that needs them.
