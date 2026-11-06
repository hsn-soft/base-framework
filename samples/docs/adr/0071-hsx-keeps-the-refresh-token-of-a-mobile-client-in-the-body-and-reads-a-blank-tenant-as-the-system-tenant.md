# ADR-0071: Hsx keeps the refresh token of a mobile client in the body and reads a blank tenant as the system tenant

- **Status:** Accepted
- **Date:** 2026-11-06 (adds to [ADR-0070](0070-hsx-has-the-clients-the-token-of-a-machine-and-the-protection-of-the-auth-server.md); production auth server ADR-0019)

## Context

The production auth server lets a client say where its refresh token travels (the cookie of a browser, or the body for a mobile application) and reads a login with no tenant as the system tenant. Hsx follows the production auth server (ADR-0070).

## Decision

- The auth server of Hsx has the same: `RefreshTokenInBody` of a client, `refreshToken` in the body of the login answer, of the refresh and of the sign-out, and an optional `tenant` of the login (blank: the tenant of type `System`). The code is the production code with the names of Hsx.
- A third client in its settings: **`hsx-mobile`**, a public client (no secret) with the audiences of the gateway and the content service and `RefreshTokenInBody`. The smoke test has checks for it (a refresh token in the body, a refresh through the body that gives a new one, one that is used once) and for the sign-in with no tenant.

## Consequences

The sample shows both ways of keeping a refresh token. The smoke test needs the whole system running; this change was checked by the build and by the script's syntax and bodies, not against the running system.

## Related

[ADR-0070](0070-hsx-has-the-clients-the-token-of-a-machine-and-the-protection-of-the-auth-server.md).
