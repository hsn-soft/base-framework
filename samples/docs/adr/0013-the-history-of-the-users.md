# ADR-0013: The history of the users: a CRUD of its own, written by the identity service and the auth server

- **Status:** Accepted
- **Date:** 2026-10-25 (follows [framework ADR-0050](../../../docs/adr/0050-a-context-has-a-properties-class-for-its-database.md))

## Context

The identity service has a second context, on its own database and provider (framework ADR-0050). It needs a purpose and the same care as the users: a domain, an application service, an endpoint.

## Decision

- **`UserHistory`** (`FullAuditedEntity`, `IMultiTenant`): `UserId`, `UserName` (and its normalized twin), `Action` (a word in small letters: `created`, `updated`, `deleted`, `login`, or any other), `Note`. The user is only referenced by its id: the two databases share nothing.
- **A CRUD like the users':** `IUserHistoryAppService` (get, paged list, filter list, search list, create, update, delete) with its DTOs, filters and `UserHistorySorting` (the latest first), and `UserHistoriesController` (`api/identity-service/v1/commercial/user-histories`). Its reads use the DTO projection of framework ADR-0046 (no mapper, only the columns of the DTO).
- **Who writes:** the identity service writes a record for each create, update and delete of a user; **the auth server writes a `login` record for every token it issues** (the tenant is the user's: nobody is signed in yet). Both register the history context (`AddIdentityHistoryPersistence`); only the identity service creates the database.
- **No transaction over the two databases yet:** a record is written after the change (and after the token is made), and a failure of the history database fails the call. A transaction, or an outbox, is a decision for later.

## Consequences

- The auth server needs the history database to sign anybody in, as it needs the identity database.
- The history of a user can be read (and corrected) through the identity service; a deleted record is marked, not removed.

## Related

[framework ADR-0050](../../../docs/adr/0050-a-context-has-a-properties-class-for-its-database.md), [framework ADR-0046](../../../docs/adr/0046-dto-projection-without-a-mapper.md), [ADR-0008](0008-the-abc-services-crud-pattern.md).
