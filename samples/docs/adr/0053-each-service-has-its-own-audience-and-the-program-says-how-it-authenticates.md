# ADR-0053: Each service has its own audience, and `Program.cs` says how it authenticates and authorizes

- **Status:** Accepted
- **Date:** 2026-10-31 (applies *framework ADR-0122*, see `../../../docs/adr/0122-a-service-has-its-own-audiences-and-chooses-authentication-and-authorization.md`; replaces the single `Jwt:Audience` of [ADR-0006](0006-jwt-signed-with-an-rsa-private-key.md))

## Decision

- Every host that checks tokens has its own `Jwt:Audience` (one string; the `aud` of the token is the list): `abc-order`, `abc-product`, `abc-shipment`, `abc-identity`, `abc-gateway` (Edx: `edx-signer`, `edx-identity`, `edx-invoice`, `edx-integration-gib-invoice`,
  `edx-integration-firm-invoice`, `edx-gateway`); the auth server accepts `abc-auth`/`edx-auth` and **issues** every audience of its system, its own included (a token must be good for its endpoints too) (`Jwt:IssuedAudiences`). `TokenService.Issue(user, audiences)` can be told fewer.
- `Program.cs` of a host says what it has: `AddAbcAuthentication` and `AddAbcAuthorization` (`AddEdx…`) with the matching `UseBaseAuthentication` and `UseBaseAuthorization`. The commercial gateways only authenticate (the
  user is in their logs; the services decide who may do what); the public gateway and the hosts open to their callers (the WCF host of the mock and of the GIB integration) have neither. The Edx services that grant by permission add
  `AddEdxPermissions` as before.
- The issuer is checked in `docker`, `uat` and `production`, not in `development`.
