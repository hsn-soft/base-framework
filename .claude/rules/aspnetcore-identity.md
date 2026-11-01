---
paths:
  - "src/HsnSoft.Base.AspNetCore*/**"
  - "src/HsnSoft.Base.Authentication/**"
  - "src/HsnSoft.Base.Authorization/**"
  - "src/HsnSoft.Base.MultiTenancy.Authentication/**"
  - "docs/WebApi.md"
  - "docs/Authorization.md"
  - "docs/Swagger.md"
  - "docs/Health.md"
  - "docs/Hosting.md"
---

# ASP.NET Core, API, and identity

- Keep ASP.NET Core integration layered on the framework's provider-neutral packages. Hosts explicitly choose authentication and authorization registrations; do not make optional middleware or Swagger mandatory for all consumers.
- Preserve the API's `BaseResponse` envelope, global exception handling, validation behavior, and trace identifiers. Do not expose exception details or secrets in 500 responses.
- Use the established route shape and require JWT authentication by default; anonymous access must be deliberate and explicit. Validate issuer outside Development and use a service-specific audience.
- Keep permission authorization in the existing framework authorization package and preserve grant scope/cache behavior. For constrained permissions, a missing grant for a required constraint is a 403 before business work; do not treat a missing value as an unlimited grant.
- Preserve health endpoint meanings (`live`, `startup`, `ready`) and host startup safety; do not turn dependency checks into liveness failures.
- Preserve Swagger's environment-based defaults and its `Enabled`/`DisabledIn` controls; do not hard-code it off in every production-like environment. Preserve gateway document routing. Scheduler endpoints require the configured API key.
- Preserve host safety: validate required settings at startup, centralize Kestrel/proxy configuration, and trust forwarded headers only from configured proxies or networks. Keep `AppInfo`, thread-pool minimum settings, and the cross-event handler limit when changing host setup.
- Read the matching feature reference (`WebApi.md`, `Authorization.md`, `Swagger.md`, `Health.md`, or `Hosting.md`) and relevant ADRs before changing public HTTP behavior.

Relevant decisions: ADR-0022, ADR-0024, ADR-0026, ADR-0039–0041, ADR-0044, ADR-0071, ADR-0081, ADR-0083, ADR-0117, ADR-0120–0122, ADR-0126.
