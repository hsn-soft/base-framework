---
paths:
  - "src/**/*.cs"
  - "src/**/*.csproj"
  - "test/**/*.cs"
  - ".github/workflows/**"
---

# Security and sensitive data

- Never put real tokens, passwords, private keys, or connection strings in source, tests, documentation, workflow files, or logs. Use environment variables or ignored local `.env` files; use test-only fake credentials in tests.
- Treat authentication, authorization, tenant isolation, and secret handling as security boundaries. Reuse the repository's established registrations and checks; do not weaken validation or expose an endpoint by default.
- Validate the issuer outside Development and use the owning service's configured audience. Scheduler endpoints require their configured key. Do not log keys, tokens, or sensitive payload fields.
- Preserve startup validation of JWT settings: issuer/audience, minimum 32-byte HMAC keys, minimum 2048-bit RSA keys, RSA key-file presence, and valid access-token duration.
- Preserve tenant-or-scope isolation: an entity is isolated by its tenant or its scope, never both. Apply the established filters at the persistence boundary.
- Mark sensitive log properties with `[SensitiveData]`. Log a failure once, with the established distinction between expected/domain errors and unexpected server failures.
- Keep workflow permissions and publishing credentials least-privileged; NuGet publishing uses trusted OIDC, not a stored long-lived API key.

Relevant decisions: ADR-0021, ADR-0037, ADR-0081, ADR-0119, ADR-0122, ADR-0126, ADR-0129–0130.
