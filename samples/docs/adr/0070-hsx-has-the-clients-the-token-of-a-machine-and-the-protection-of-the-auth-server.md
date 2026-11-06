# ADR-0070: Hsx has the clients, the token of a machine and the protection of the auth server

- **Status:** Accepted
- **Date:** 2026-11-06 (adds to [ADR-0068](0068-hsx-follows-the-production-identity-the-business-is-the-same.md); production auth server ADR-0016 and ADR-0018)

## Context

The production auth server knows its clients (an application that asks for a token, with the audiences its tokens may reach), gives a machine a token of its own (`client_credentials`), and, being open to the internet, has security headers, CORS and a rate limit. The auth server of Hsx had none of it: any caller signed in and every token had every audience.

## Decision

- **The auth server host of Hsx is the production one with the names of Hsx** (`Hsx.AuthServer.Host`): the application service behind a one-line controller, the DTOs one per file, `ClientRegistry`, `UserAuthenticator`, `TokenService`, the rate limit, `UseBaseSecurityHeaders` and `UseBaseCors`. A login, a refresh and a token of a client name their client (`clientId`).
- **Two clients in its settings:** `hsx-web`, a public client with every audience of Hsx (the commercial web, the smoke test and the Postman collection sign users in through it), and `hsx-site`, a machine with the audiences of the gateway and the content service. **The secret of `hsx-site` is a demo** (`hsx-site-demo-secret`, written in the README and the smoke test, only its SHA-256 is in the settings): a sample has no secret store, and a real system never does this.
- **The rate limits are far above production's** (600 a minute for the sign-in, 3000 overall; production: 10 and 300), because the smoke test signs in dozens of times a minute from one address; CORS lists no origin (the commercial web calls the gateway from its server).
- The commercial web, `smoke-test.sh`, `demo-scenario.sh` and the Postman collection follow (`clientId`, a refresh asks through a client); the smoke test has checks for the clients (an unknown client, a wrong secret, a public client that is no machine, a machine that signs no user in, the token of a machine and its audiences).

## Consequences

A token of Hsx has the audiences of its client, not all of them. The smoke test needs the whole system running; this change was checked by the build and by running the new part of the script with a stub, not against the running system.

## Related

[ADR-0068](0068-hsx-follows-the-production-identity-the-business-is-the-same.md), [ADR-0069](0069-hsx-reads-the-sign-in-audit-log-as-the-production-identity-does.md).
