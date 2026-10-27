# ADR-0028: A product's hosts share their composition in `{Product}.Shared.Hosting`, with one specialization per kind of host

- **Status:** Accepted
- **Date:** 2026-10-25
- **Note:** [ADR-0072](0072-a-host-chooses-its-localization-and-its-cache.md): `Abc.Shared.Hosting` and `Abc.Shared.Hosting.Microservices` no longer register the localization or Redis (`AddAbcRedis` is gone, `AddAbcMicroservice` has no `localizedAssemblies`): a host chooses them.
- **Note:** [ADR-0069](0069-who-references-which-shared-project-in-abc.md): the rule of who references which Abc shared project (Helper, Contracts, Localization, Hosting); `AbcTenants` is in `Abc.Shared.Helper` now.
- **Note:** [ADR-0068](0068-the-shared-texts-of-a-product-are-a-project-of-their-own.md): the shared texts moved out of `Abc.Shared.Hosting` into `Abc.Shared.Localization`; the hosting projects do not reference it, a host that wants the texts adds the reference and the layer itself.

## Context

Every host of a product (a gateway, a microservice) repeats the same composition of the framework: logging, the request log, the answers of a web api, Swagger, health, reading the bearer token, in an order that matters (the request log outside the exception handler, authentication before
authorization). Left in each `Program.cs` it drifts. The production system keeps it in `Hhs.Shared.Hosting` (common) with `Hhs.Shared.Hosting.Gateways` and `Hhs.Shared.Hosting.Microservices` on top; the Xyz sample did it in one `Xyz.Shared`. The framework cannot hold it: it knows no product
([ADR-0005](0005-where-code-lives.md)).

## Decision

The product's **shared hosting projects** live in `samples/abc/shared/` and are layered:

| Project | Is for | Has |
|---|---|---|
| `Abc.Shared.Hosting` | every host | `AddAbcHosting` / `UseAbcHosting`: the masker and console logger, the request log, the web api answers, Swagger (with or without the bearer button, with a document name), health, the JWT validation when the host reads the token, and the middleware **in the order the hosts need** |
| `Abc.Shared.Hosting.Gateways` | a gateway | on top of Hosting: the route registry and the YARP configuration built from it, the Kestrel limits and the forwarded headers of a public network, the Swagger of the services behind it (`AddAbcGateway` / `UseAbcGateway`, `ConfigureAbcGatewayKestrel`) |
| `Abc.Shared.Hosting.Microservices` | a microservice | on top of Hosting: the bearer token required and a Swagger that can send it (`AddAbcMicroservice` / `UseAbcMicroservice`) |

- **A specialization references `Abc.Shared.Hosting` and nothing of the other**: `.Gateways` and `.Microservices` never reference each other. A host references the one that fits it; the framework packages come with it.
- **A host is thin:** its `Program.cs` adds the shared composition and then only what is its own (a service its stores and application services, a gateway nothing but its settings). The service's controllers and its base controller stay in the service ([ADR-0026](0026-route-pattern-and-jwt-by-default.md)).
- **The MVC web applications (`samples/abc/app`) do not use them**: they answer pages, not `BaseResponse`, so the web api shell of `Abc.Shared.Hosting` does not fit; each takes the framework's logging, request log and health directly.
- A new kind of host (an agent) uses `Abc.Shared.Hosting` directly or gets its own `Abc.Shared.Hosting.<Kind>`.
- Names follow the production ones (`{Product}.Shared.Hosting`, `.Gateways`, `.Microservices`). **A project that is the only host of its kind has no folder level of its own**: `samples/abc/gateway/Abc.Gateway.Commercial.Host`, `.../Abc.Gateway.Public.Host`;
  a service has its layers beside its host (`samples/abc/service/identity/...`) ([ADR-0006](0006-repository-and-solution-layout.md)).

## Consequences

- A change in the order of the middleware, or a new default of every host, is made once.
- The shared projects are samples of how a product composes the framework; a part that a second product would want unchanged becomes a framework package ([ADR-0002](0002-one-package-one-capability.md)).
- The solution file has them in the system's solution folder (`abc`) like every other project of the system.

## Related

[ADR-0005](0005-where-code-lives.md), [ADR-0006](0006-repository-and-solution-layout.md), [ADR-0026](0026-route-pattern-and-jwt-by-default.md), [ADR-0027](0027-gateways-per-audience.md), `samples/abc/shared`.
