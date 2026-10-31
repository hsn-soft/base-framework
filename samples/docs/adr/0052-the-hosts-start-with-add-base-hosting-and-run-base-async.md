# ADR-0052: The hosts of both samples start with `AddBaseHosting` and end with `RunBaseAsync`, and write their limits in their settings

- **Status:** Accepted
- **Date:** 2026-10-31 (applies *framework ADR-0120*, see `../../../docs/adr/0120-the-host-takes-its-settings-from-one-section-and-starts-safely.md`)

## Decision

- Every host of Abc and Edx calls `builder.AddBaseHosting()` right after `CreateBuilder` and ends with `return await builder.RunBaseAsync(app => { ... })`; a failed start is a critical log and the exit code 1.
- The time to stop moved from `AddAbcHosting` and `AddEdxHosting` to the framework (`Hosting:ShutdownTimeoutSeconds`, same key). `UseAbcHosting` and `UseEdxHosting` start with `UseBaseForwardedHeaders`; the gateways (`UseAbcGateway`, `UseEdxGateway`) feed
  `GatewayPolicies:TrustedProxyIps` to the same options, so the client address is believed once, from one place.
- The microservices, the integration hosts, the mock and the auth servers write the limits of Kestrel and the forwarded headers they run with in `Hosting` of `appsettings.json`; the port is `Urls` of each environment's settings
  (a specific port in development, `http://+:8080` in docker, uat and production). The gateways keep their own policy for the limits of the public network.
