# ADR-0032: The auth server reads the users of the identity service; the author of a row is the `sub` of the token

- **Status:** Accepted (`CreatedBy`/`UpdatedBy` are now the id of the user, and the token has tenant claims, [ADR-0037](0037-audit-soft-delete-and-tenants.md))
- **Date:** 2026-10-25 (replaces the user list in the `Auth` section of [ADR-0029](0029-one-auth-server-issues-the-token.md))

## Context

The auth server of ADR-0029 knew two users from its `Auth` settings, with the passwords in clear text, while the identity service has the real users. Two lists of people, and a sample that
shows a password in a settings file, is not what a system is built like.

## Decision

- **The users are the identity service's, in its database.** `Abc.AuthServer.Host` references `Abc.IdentityService.EntityFrameworkCore` (and so the domain), registers the same `IdentityDbContext` on the same `ConnectionStrings:Postgres`
  and **only reads**: `UserAuthenticator` finds the user by name (`IUserRepository.FindByUserNameAsync`), checks the password and that the user is active, and the answer is the same "Invalid credentials" (401) for an unknown name, an
  inactive user and a wrong password; an unknown name does the same hashing work as a wrong password. The users are written only by the identity service.
- **A user keeps a password hash, never the password:** `PasswordHasher` (domain, `Security`) is PBKDF2 with SHA-256, 210 000 iterations, a random salt, in the text `v1.{iterations}.{salt}.{hash}`; the version and the cost are in the text so they can be
  changed later. A password is 8 to 128 characters; it is `[SensitiveData]` in the create DTO, so it is never in a log. There is no endpoint to change a password yet.
- **A user has `Roles`** (`admin`, `customer`; `customer` when none is given), kept in one column of names with commas, and **the token has the claims of the user:** `sub` (the user name), `user_id`, `name`, `email`, one `role` per role, `jti`.
- **`CreatedBy` and `UpdatedBy`** are the `sub` of the token of whoever made the request (`ICurrentUser` in the application layer, answered by the host from `HttpContext`); `null` without a signed-in user. The seed writes `system`.
- **The database and two users are made on start:** `EnsureIdentityDatabaseAsync` creates the schema when it is not there and, when there is no user, adds `admin` / `admin123` (admin, customer) and `alice` / `alice123` (customer). Only the identity service does this: the database is its, and the auth server only reads it. The passwords are sample values for a development database.
- The `Auth` section of the auth server is gone; its `ConnectionStrings:Postgres` is the identity service's.

## Consequences

- The two hosts are coupled by one database schema (the identity service owns it): a change of the `users` table is a change that the auth server has to be built with. A service of its own for the check (an HTTP call from the auth server to the identity service) was not chosen: it
  would need a token to get a token, or an open endpoint that returns hashes.
- `EnsureCreated` does not change a table that exists: a database made before this decision has no password column and has to be dropped (`drop database abc_identity`); a system that keeps data uses migrations.
- The auth server does not make the database: until the identity service has started once (it makes the schema and the users), a login finds no table and fails; the readiness check of the auth server (`/health/ready`) shows the database is not ready.
- Who may create, change or delete users (an `admin` policy) is not decided yet: every signed-in user can.

## Related

[ADR-0029](0029-one-auth-server-issues-the-token.md), [ADR-0031](0031-jwt-signed-with-an-rsa-private-key.md), [ADR-0030](0030-microservice-project-pattern.md), `samples/README.md`.
