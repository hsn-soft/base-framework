# ADR-0029: One auth server issues the JWT; every other host only validates it

- **Status:** Accepted
- **Date:** 2026-10-25

## Context

Every service endpoint needs a bearer token unless it says otherwise ([ADR-0026](0026-route-pattern-and-jwt-by-default.md)), the commercial gateway reads it to know the user in its logs ([ADR-0027](0027-gateways-per-audience.md)),
and the web applications have users who sign in. Something has to check a password and issue the token, and there must be exactly one place that does.

## Decision

- **`Abc.AuthServer.Host`** (a web api, `samples/abc/app`) checks a user name and a password and issues a short-lived JWT access token (claims `sub`, `role`, `jti`; lifetime `Jwt:AccessTokenMinutes`). Its login endpoint is the one open endpoint
  (`[AllowAnonymous]`, route `api/auth-server/v1/commercial/auth/login`); `me` says who the token says the caller is. It is a microservice like the others: `Abc.Shared.Hosting.Microservices`, the base controller, the route pattern.
- **Everything else only validates**: every Abc host that reads a token (the identity service, the commercial gateway, the auth server's own `me`) has the **same `Jwt` section** (`Issuer`, `Audience`, `Key`) and uses `AddBaseJwtAuthentication`. Only the auth server holds the signing code.
- **Callers reach the auth server through the commercial gateway** (`/auth-server/v1/auth/login` is mapped to its `commercial` endpoint), like any other service; the commercial web application signs its employees in that way.
- **A web application keeps the token in its own cookie** (the cookie belongs to the page, the token is a claim of it) and sends it as `Authorization: Bearer` with every call to the gateway. The correlation id of the page request goes with it, so the page, the gateway and
  the services behind it are one chain in the logs ([ADR-0018](0018-chain-logs.md)).
- The sample's users are a short list in the `Auth` section of the auth server, and the signing key in `appsettings.json` is a sample value. **A real auth server asks a store, and the key lives in a secret store, never in the repository.**
- No refresh token in the sample (the Xyz auth server has one, with Redis).

## Consequences

- Changing the key or the issuer is a change in every host's `Jwt` section at once; a host with another key rejects every token (401).
- The public side (the public gateway, the public web application) has no sign-in: it only reaches `public` endpoints, which are open.
- The Xyz system keeps its own auth server and its own `Jwt` values; the two systems no longer share tokens.

## Related

[ADR-0026](0026-route-pattern-and-jwt-by-default.md), [ADR-0027](0027-gateways-per-audience.md), [ADR-0028](0028-shared-hosting-projects.md), `samples/abc/app`, `HsnSoft.Base.Authentication`.
