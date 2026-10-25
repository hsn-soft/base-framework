# ADR-0040: `ApiControllerBase` is the framework's; a service's base controller derives from it

- **Status:** Accepted
- **Date:** 2026-10-25 (replaces the part of [ADR-0039](0039-guard-and-translating-a-response-body.md) that put `Logger`, `CurrentUser` and `CurrentTenant` in each service's base controller)

## Context

Every service has a base controller (ADR-0026) with the area, the JWT authorization, `[Produces]` and `[ApiController]`. ADR-0039 gave it `Logger`, `L`, `CurrentUser` and `CurrentTenant`, found from `HttpContext.RequestServices`. Three of the four are the same in every service, so each service copied them.

## Decision

- **`HsnSoft.Base.AspNetCore.ApiControllerBase` (`HsnSoft.Base.AspNetCore.WebApi`)** is a `ControllerBase` with `Logger`, `CurrentUser` and `CurrentTenant`, each found from the services of the request the first time it is used (no constructor, no `IServiceProvider` to pass). It carries **no attributes**: the area, the authorization and the rest are the service's.
- **The base controller of a service derives from it** (`BaseServiceController : ApiControllerBase`) and keeps the attributes of ADR-0026 and **`L`** (the standard `IStringLocalizer`), which stays in the service: the web api package does not reference the localization abstractions.

## Consequences

- A service's base controller is the attributes and `L`; what the framework gives a controller is in one place.
- `ApiControllerBase` needs `ICurrentUser` (registered by `AddBaseJwtAuthentication`) and `ICurrentTenant` (by the database providers, `AddBaseEntityConcepts`) in the services when the controller asks for them.

## Related

[ADR-0026](0026-route-pattern-and-jwt-by-default.md), [ADR-0036](0036-icurrentuser-in-the-framework.md), [ADR-0039](0039-guard-and-translating-a-response-body.md).
