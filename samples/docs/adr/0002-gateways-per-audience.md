# ADR-0002: One gateway per audience, built from a route registry; the gateway forwards, the services decide who may call

- **Status:** Accepted
- **Date:** 2026-10-25

## Context

A system has clients of different kinds: the company's own applications (commercial) and the public. Their endpoints must not mix, a service must not be reachable on a side it was not meant for, and a new service must be one more entry in a gateway,
not new code. The production gateway (`Hhs.Gateway.Commercial.Http.Host`) already does this; the Abc sample follows it with the framework packages ([framework ADR-0026](../../../docs/adr/0026-route-pattern-and-jwt-by-default.md) for the route pattern).

## Decision

- **One gateway host per audience:** `Abc.Gateway.Commercial.Host` (maps to the `commercial` segment of the services) and `Abc.Gateway.Public.Host` (maps to `public`). What they share is in **`Abc.Shared.Hosting.Gateways`**, on top of `Abc.Shared.Hosting` ([ADR-0003](0003-shared-hosting-projects.md)): each host is only a `Program.cs`, a project file and an `appsettings.json`.
- **Routes come from a registry, not from code:** `ReverseProxyRoutes:Routes` (`ClusterId`, `MatchPath`, `DownstreamPathPattern`, `ActivityTimeout`) and `ServiceEndpoints:Services` (a service key and its internal `Origin`) are turned into YARP routes (a path transform) and clusters.
  A route `identity-api` maps `/identity-service/v1/{**catch-all}` to `/api/identity-service/v1/commercial/{**catch-all}`; `identity-health` maps `/identity-service/health` to the service's liveness. A cluster is named `<service>-cluster` and its service is `<service>-service`
  (`identity-cluster` is `identity-service`). **Adding a service to a gateway is configuration:** a `ServiceEndpoints` entry, its routes, and its `SwaggerGateway` entry ([framework ADR-0024](../../../docs/adr/0024-swagger.md)).
- **The gateway forwards; the services decide who may call.** Every endpoint of a service needs a JWT unless it says `[AllowAnonymous]` (framework ADR-0026), so the gateway puts no authorization on its routes. The **commercial gateway reads the bearer token** (`AddBaseJwtAuthentication`) so that the logs it writes
  know the user; the public gateway has no authentication at all.
- **The gateways log with `Role: Gateway`:** they write only what they answered themselves (a 404, a health call, a 502); the services log the requests they handle ([framework ADR-0018](../../../docs/adr/0018-chain-logs.md)).
- **The limits of a public network** are settings (`GatewayPolicies`): the biggest request body, the time to send the headers, the keep-alive time, the number and size of headers (Kestrel), and the **trusted proxies** whose `X-Forwarded-For` is believed.
- **Each gateway's Swagger shows only what it forwards:** the documents of the services under the gateway's own paths (the commercial gateway shows `/identity-service/v1/users/...`, the public one `/identity-service/v1/users/check-user-name`).
- Ports of the sample: commercial 5500, public 5600, the identity service 5400.

## Consequences

- A service endpoint is reachable from a gateway only if it is under that gateway's segment; the other gateway answers 404 for it. Moving an endpoint between sides is a change of route ([framework ADR-0026](../../../docs/adr/0026-route-pattern-and-jwt-by-default.md)).
- The route registry code is the product's (`Abc.Shared.Hosting.Gateways`, [ADR-0003](0003-shared-hosting-projects.md)); a framework package for it is possible if a second product wants it unchanged.
- **Not taken from the production gateway, on purpose:** the `/docs` landing page and its access middleware (a product's page), rate limiting (it is commented out there; not decided), the per-route body-limit middleware, CORS settings, search engine and static files middleware.
- A gateway that cannot reach a service answers 502 for it and logs one `HttpRequestResponse`; the service's document is a 502 in the Swagger UI.

## Related

[framework ADR-0005](../../../docs/adr/0005-where-code-lives.md), [framework ADR-0018](../../../docs/adr/0018-chain-logs.md), [framework ADR-0024](../../../docs/adr/0024-swagger.md), [framework ADR-0026](../../../docs/adr/0026-route-pattern-and-jwt-by-default.md), `samples/abc/gateway`, `samples/README.md`.
