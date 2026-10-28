# ADR-0071: The `AspNetCore` layer packages (`WebApi`, `Swashbuckle`) build on `AspNetCore`

- **Status:** Accepted
- **Date:** 2026-10-27 (follows [ADR-0003](0003-abstractions-separate-from-providers.md) and [ADR-0070](0070-a-framework-package-brings-what-its-api-exposes.md); changes the rows of `AspNetCore.WebApi` and `AspNetCore.Swashbuckle` in [ADR-0004](0004-core-once-and-the-dependency-graph.md))

## Context

A package named `X.Y` is a layer on `X` ([ADR-0003](0003-abstractions-separate-from-providers.md): `EventBus.Kafka` on `EventBus`, `EntityFrameworkCore.PostgreSql` on `EntityFrameworkCore`, `AspNetCore.SwaggerGateway` on `Swashbuckle`). `AspNetCore.WebApi` (controllers, the answer of
a web api, the exception handler) and `AspNetCore.Swashbuckle` (the Swagger document and UI) are layers on the host basics of `AspNetCore` (the health endpoints a platform probes), but both referenced only `Core`, so a host that used them also had to name `AspNetCore` itself, and a product's shared hosting listed all three.

## Decision

- **`HsnSoft.Base.AspNetCore.WebApi` and `HsnSoft.Base.AspNetCore.Swashbuckle` reference `HsnSoft.Base.AspNetCore`**, so a project that references either gets the host basics with it. Nothing else changes: `AspNetCore` still knows only `Core`, there is no cycle.
- A product project that uses either does not list `AspNetCore` too ([ADR-0070](0070-a-framework-package-brings-what-its-api-exposes.md)).

## Consequences

- The graph of ADR-0004 reads `AspNetCore.WebApi` and `AspNetCore.Swashbuckle` on `AspNetCore` (`SwaggerGateway` is on `Swashbuckle`, so it gets it too).
- A service that wants the web api or Swagger without the host basics cannot have it; every host has the health endpoints, so this costs nothing.

## Related

[ADR-0003](0003-abstractions-separate-from-providers.md), [ADR-0004](0004-core-once-and-the-dependency-graph.md), [ADR-0070](0070-a-framework-package-brings-what-its-api-exposes.md).
