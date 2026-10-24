# ADR-0024: Swagger is its own package, off where a service runs for real, and a gateway shows its services

- **Status:** Accepted
- **Date:** 2026-10-25

## Context

The services used Swashbuckle through the old `HsnSoft.Base.Swashbuckle`, with `if (!IsHostProduction())` repeated in every `Program.cs` (once to register, once to use). The first rewrite had replaced it with the built-in OpenAPI document inside the
web api package, which forced a dependency on every web api and was not what the services use. The production gateway shows its own document and the documents of every service in one UI.

## Decision

- **`HsnSoft.Base.AspNetCore.Swashbuckle`** (depends on Core): `AddBaseSwagger`, `AddBaseSwaggerWithBearer`, `AddBaseSwaggerWithOAuth` register the document (all endpoints, types named by full name, an optional Bearer or OAuth2 button),
  `UseBaseSwagger` serves it and the UI. **The built-in OpenAPI support was removed from `AspNetCore.WebApi`** (a web api takes Swagger only if it wants it).
- **Where it is served is configuration, once:** the `Swagger` section (`Enabled`, `DisabledIn`, `RoutePrefix`); by default it is **off in `Production`, `Staging`, `stage`, `uat`**, the same environments that hide error details. `UseBaseSwagger` does nothing there, so
  `Program.cs` has no `if`. `/swagger` is already in the request log's excluded paths.
- **`HsnSoft.Base.AspNetCore.SwaggerGateway`** (depends on the Swashbuckle package) lets a **gateway** show the documents of its services in its UI: `AddBaseSwaggerGateway(configuration)` and the `SwaggerGateway` section (`PublicOrigin`, `Services`: `Key`, `DisplayName`,
  the internal `Origin`, `SwaggerPath`, `DownstreamPathPrefix`, `GatewayPathPrefix`; cache, timeout, retries). The gateway fetches each document from the service's **internal** address, rewrites the paths and the `servers` address to the gateway's, caches it, and a service that is
  down is a 502 where its document should be. A small hook in the Swashbuckle package (`ISwaggerExtension`) lets `UseBaseSwagger` list the extra documents, so the host calls the same method.
- What the hhs gateway also had (the `/docs` landing page with live probes of every service, an access middleware by CIDR and role) is **a product's own page**, not the framework's; the gateway's authorization before `UseBaseSwagger` decides who opens `/swagger`.

## Consequences

- Breaking: `AddBaseOpenApi`/`MapBaseOpenApi` are gone; the old package's `SwaggerConfigurationHelper` maps to the new methods (table in `docs/Swagger.md`).
- The `BaseResponse` envelope is not yet shown in the document (the result filter wraps at run time); an operation filter could.
- The remote stream content mapping of the old package was not ported (the type does not exist).

## Related

[ADR-0003](0003-abstractions-separate-from-providers.md), [ADR-0022](0022-web-api-answers.md), `docs/Swagger.md`.
