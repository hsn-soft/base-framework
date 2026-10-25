# ADR-0036: `ICurrentUser` is the framework's; the JWT authentication registers it

- **Status:** Accepted
- **Date:** 2026-10-25 (replaces the per-service `ICurrentUser` of [ADR-0033](0033-the-abc-services-crud-pattern.md))

## Context

ADR-0033 had each service define an `ICurrentUser` in its application layer and implement it in its host with `IHttpContextAccessor`, and the host's `Program.cs` register both (`AddHttpContextAccessor`, `AddScoped<ICurrentUser, ...>`). That is four copies of the same
interface and class, and two lines in every host that have nothing to do with what the host is.

## Decision

- **`HsnSoft.Base.Security.ICurrentUser`** (`UserId`: the `sub` claim, `IsAuthenticated`) is in `HsnSoft.Base.Core`: the application and domain code of a service, which already reference Core, ask for it without knowing HTTP.
- **`HsnSoft.Base.Authentication` implements it from the `HttpContext` and registers it** in `AddBaseJwtAuthentication` (with `AddHttpContextAccessor`), as a singleton added with `TryAdd`: a host or a test that registered its own keeps it. A worker with no request has no current user (`UserId` is `null`).
- The services use it as it is: `CreatedBy` / `UpdatedBy` come from `currentUser.UserId`; their `Program.cs` has nothing about it.

## Consequences

- One interface and one implementation for every service; a new service does nothing to have it.
- The name is the framework's `UserId` (the claim `sub` is the user name in Abc), not `UserName`.
- The roles are not on it yet (`ClaimsPrincipal.GetRoles()` reads them where there is a principal); they are added when a use case needs them.

## Related

[ADR-0033](0033-the-abc-services-crud-pattern.md), [ADR-0032](0032-the-auth-server-reads-the-users-of-the-identity-service.md), [ADR-0031](0031-jwt-signed-with-an-rsa-private-key.md).
