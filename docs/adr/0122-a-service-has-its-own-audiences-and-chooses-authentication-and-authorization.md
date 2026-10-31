# ADR-0122: A service has its own audience, the token has a list, the issuer is checked outside Development, and authentication and authorization are choices

- **Status:** Accepted
- **Date:** 2026-10-31 (adds to [ADR-0026](0026-route-pattern-and-jwt-by-default.md) and [ADR-0081](0081-permissions-one-framework-package-and-the-grants-in-the-distributed-cache.md))

## Context

`Jwt:Audience` was one string, the same for every service of a system, so a token that was good for one service was good for all of them, and a token could name only one. Gateways will multiply and a client must reach **only the services whose audience its
token has**. The issuer was checked everywhere, so a developer's machine could not use the token of another auth server; the old system checked it in production only, which left `uat` and `docker` open. The clock skew was fixed in code.
`AddBaseJwtAuthentication` also registered the authorization policies, so a host that only reads the token (a gateway that wants the user for its logs) got an authorization it did not ask for, and a service could not choose the standard
authorization or its own.

## Decision

- **`Jwt:Audience`** stays one string, the name of this service (`abc-order`): the `aud` claim of a **token** may be a list, and the token is accepted when the list has this audience. **`Jwt:IssuedAudiences`** (a list) is what the
  host that issues tokens puts into the `aud` of a token when it is not told others (a client that may reach fewer services is given fewer); an empty one stops the auth server of the samples.
- **The issuer is checked in every environment except Development** (`Jwt:ValidateIssuer`, true or false, says it for an environment). Without a host environment it is checked.
- **`Jwt:ClockSkewSeconds`** (30, between 0 and 300) is the tolerance of the lifetime of a token.
- **`AddBaseAuthorization()`** is its own call (the standard policies and `BasePolicies.Authenticated`); `AddBaseJwtAuthentication` only reads the token. The product shell names the choices in `Program.cs`: `AddXAuthentication`,
  `AddXAuthorization` (and `AddBasePermissions` for a service that grants by permission); a host leaves out what it does not use, and the matching `UseBaseAuthentication`/`UseBaseAuthorization`.

## Consequences

- A token names the services it may be sent to; a service refuses the others, whatever else is right with the token.
- A development machine can use a token of another issuer; `uat`, `docker` and production cannot.
- A host that reads the token but does not authorize says so by what it calls.
- Not here: the choice of the audiences of a client (the auth server of a product decides it, per client, from its own data); a token carries the audiences it was given.

## Related

[ADR-0026](0026-route-pattern-and-jwt-by-default.md), [ADR-0081](0081-permissions-one-framework-package-and-the-grants-in-the-distributed-cache.md), [docs/Authorization.md](../Authorization.md).
