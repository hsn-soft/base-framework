# ADR-0031: The JWT is signed with an RSA private key; the other hosts only have the public key

- **Status:** Accepted
- **Date:** 2026-10-25 (replaces the shared signing key of [ADR-0029](0029-one-auth-server-issues-the-token.md))

## Context

ADR-0029 had one HMAC key in the `Jwt` section of every Abc host: the auth server signed with it and every other host checked with it. A key that checks is a key that signs: any host (a gateway, a service) could make a token, and a leak of
the settings of any of them was a leak of the signing key, copied into six `appsettings.json` files. The production services already do it the other way: the auth server holds an RSA private key and the other hosts have only `public_key.xml`.

## Decision

- **Tokens are signed with RS256, an RSA 2048 private key** (`Jwt:PrivateKeyFile`) that **only the auth server has**; every other host checks them with the **public key** (`Jwt:PublicKeyFile`) and cannot make one.
- **`HsnSoft.Base.Authentication` supports both modes:** `Jwt:Key` (HMAC, a shared key; what the Xyz system uses) or, when `Jwt:PublicKeyFile` is given, the RSA public key, which wins and accepts **RS256 only** (a token that says it is signed another way is refused; an HMAC token made with the public key as the secret fails).
  `RsaKeys.LoadPublic` and `RsaKeys.LoadPrivate` read the XML of `RSA.ToXmlString` (the format of the production services) or a PEM. A relative path is relative to where the application runs, so the key files are copied next to the assemblies.
- **A missing or unreadable key stops the host when it starts** with the path in the message (the public key at registration, the private key when the auth server starts), not at the first request.
- **Where the files are:** the private key is `private_key.xml` in `Abc.AuthServer.Host` and is copied only to its output; the public key is `public_key.xml` in `Abc.Shared.Hosting`, copied to the output of every host that uses it, so no service or gateway has the private key next to it.
- **No key is in git.** `private_key.xml` and the shared `public_key.xml` are ignored; `scripts/generate-rsa-keys.cs` makes a pair (`dotnet run scripts/generate-rsa-keys.cs -- <private-file> <public-file>`, it does not overwrite). **A key of another system is never reused for a sample, and a private key is never committed**: a
  private key that is also used elsewhere is a leak the day it is pushed.
- The `Jwt` section of an Abc host that checks tokens is `Issuer`, `Audience`, `PublicKeyFile`; the auth server's also has `PrivateKeyFile` and `AccessTokenMinutes`.

## Consequences

- Only the auth server can make tokens; the other hosts hold nothing secret. In Kubernetes the private key is a secret mounted as a file in the auth server, and the public key is the same file for everyone else.
- **Key rotation** is a new pair and a redeploy of the hosts (no `kid`, so one key at a time); supporting two public keys during a rotation is not done.
- A fresh clone has no keys: the hosts that need them do not start until `scripts/generate-rsa-keys.cs` has made a pair (the message says which file is missing).
- The Xyz system keeps its HMAC `Jwt:Key`; the two systems do not share tokens.
- The validation is covered by unit tests (valid, wrong key, wrong issuer or audience, the HMAC-with-the-public-key attack, the shared key ignored, a missing file).

## Related

[ADR-0029](0029-one-auth-server-issues-the-token.md), [ADR-0026](0026-route-pattern-and-jwt-by-default.md), `HsnSoft.Base.Authentication` (`JwtOptions`, `RsaKeys`), `scripts/generate-rsa-keys.cs`, `samples/README.md`.
