# ADR-0004: One auth server issues the JWT; every other host only validates it

- **Status:** Accepted (the shared signing key it describes is replaced by an RSA key pair, [ADR-0006](0006-jwt-signed-with-an-rsa-private-key.md); the user list of its settings by the users of the identity service, [ADR-0007](0007-the-auth-server-reads-the-users-of-the-identity-service.md))
- **Date:** 2026-10-25

## Context

Every service endpoint needs a bearer token unless it says otherwise ([framework ADR-0026](../../../docs/adr/0026-route-pattern-and-jwt-by-default.md)), the commercial gateway reads it to know the user in its logs ([ADR-0002](0002-gateways-per-audience.md)),
and the web applications have users who sign in. Something has to check a password and issue the token, and there must be exactly one place that does.

## Decision

- **`Abc.AuthServer.Host`** (a web api, `samples/abc/app`) checks a user name and a password and issues a short-lived JWT access token (claims `sub`, `role`, `jti`; lifetime `Jwt:AccessTokenMinutes`). Its login endpoint is the one open endpoint
  (`[AllowAnonymous]`, route `api/auth-server/v1/commercial/auth/login`); `me` says who the token says the caller is. It is a microservice like the others: `Abc.Shared.Hosting.Microservices`, the base controller, the route pattern.
- **Everything else only validates**: every Abc host that reads a token (the identity service, the commercial gateway, the auth server's own `me`) has the **same issuer and audience** and uses `AddBaseJwtAuthentication`. Only the auth server holds the signing code (and, by ADR-0006, the private key).
- **Callers reach the auth server through the commercial gateway** (`/auth-server/v1/auth/login` is mapped to its `commercial` endpoint), like any other service; the commercial web application signs its employees in that way.
- **A web application keeps the token in its own cookie** (the cookie belongs to the page, the token is a claim of it) and sends it as `Authorization: Bearer` with every call to the gateway. The correlation id of the page request goes with it, so the page, the gateway and
  the services behind it are one chain in the logs ([framework ADR-0018](../../../docs/adr/0018-chain-logs.md)).
- **The sign-in of a page lasts as long as its token** (`expires_in` of the login answer), and the cookie does not slide past it. When the gateway or a service does not accept the token anyway (it expired, the key changed), the page signs the user out and sends him to the sign-in page with a message, rather than showing an error.
- The sample's users are a short list in the `Auth` section of the auth server, and the keys are made with a script and are not in the repository ([ADR-0006](0006-jwt-signed-with-an-rsa-private-key.md)). **A real auth server asks a store.**
- No refresh token in the sample (the Xyz auth server has one, with Redis): a user signs in again when the token ends (15 minutes).
- **Nothing in the Abc system is open by accident**: the open endpoints are the login of the auth server, `PublicUsersController` of the identity service and the health endpoints; a demo controller needs a token like the rest.

## Consequences

- Changing the issuer or the audience is a change in every host's `Jwt` section at once; a host with other values rejects every token (401).
- The public side (the public gateway, the public web application) has no sign-in: it only reaches `public` endpoints, which are open.
- The Xyz system keeps its own auth server and its own `Jwt` values; the two systems no longer share tokens.

## Related

[framework ADR-0026](../../../docs/adr/0026-route-pattern-and-jwt-by-default.md), [ADR-0002](0002-gateways-per-audience.md), [ADR-0003](0003-shared-hosting-projects.md), `samples/abc/app`, `HsnSoft.Base.Authentication`.
