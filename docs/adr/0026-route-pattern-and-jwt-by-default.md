# ADR-0026: Routes are `api/{area}/v1/{commercial|public}/{controller}`, and every endpoint needs a JWT unless it says otherwise

- **Status:** Accepted
- **Date:** 2026-10-25

## Context

The services behind a gateway all expose controllers; the gateway routes by path. There is a gateway for the company's own (commercial) clients and, in the same way, one for the public, and a service must say which of them
an endpoint is for. Without one pattern each service invented its own prefix, the gateway's routes could not be written once, and an endpoint could be left open by forgetting an attribute. The maintainer's production
services already follow one pattern for nearly every endpoint.

## Decision

- **Route pattern:** `api/{AREA_NAME}/v1/{commercial|public}/{controller}`, written on the controller: `[Route("api/identity-service/v1/commercial/users")]`. `{AREA_NAME}` is the service's area (`identity-service`), `v1` the version of the api, and the third
  segment says **which gateway the endpoint is for** (`commercial` or `public`). Actions add their own part (`create`, `paged-list`, `{id}`).
- **The gateway adds that part, the client never sees it.** A client calls `/identity-service/v1/users/...` on the gateway; the gateway's route maps it to the service:

  ```json
  "identity-api": {
    "ClusterId": "identity-cluster",
    "MatchPath": "/identity-service/v1/{**catch-all}",
    "DownstreamPathPattern": "/api/identity-service/v1/commercial/{**catch-all}",
    "ActivityTimeout": "00:00:30"
  }
  ```

  (the commercial gateway's setting; the public gateway maps to `.../public/...`). The gateway's Swagger shows the same mapping with `DownstreamPathPrefix: "/api/identity-service/v1/commercial"` and
  `GatewayPathPrefix: "/identity-service/v1"` ([ADR-0024](0024-swagger.md)).
- **Every service has a base controller** (`BaseServiceController`) that carries what all its controllers share: `[Area("identity-service")]`, `[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]`, `[Produces("application/json")]`,
  `[ApiController]` and the `Logger` for application logs. Controllers derive from it and only add their `[Route]`.
- **Secure by default:** every endpoint works with a valid JWT bearer token. **An endpoint that must be open says so with `[AllowAnonymous]`** on the controller or the action; nothing else changes. A forgotten attribute leaves an endpoint closed, not open.
- Controllers only; no minimal APIs in the services ([ADR-0022](0022-web-api-answers.md)).
- Health endpoints are mapped separately and are open.

## Consequences

- One gateway route per service and per gateway: `/identity-service/v1/**` on the gateway becomes `/api/identity-service/v1/commercial/**` at the service. Each gateway only reaches the endpoints of its own segment.
- The pattern is written literally on each controller (not generated from `[area]`/`[controller]` tokens), so a route is found by searching for it; a controller whose name differs from its path segment stays correct.
- A host that derives from the base controller must register JWT authentication (`AddBaseJwtAuthentication`, `UseBaseAuthentication`, `UseBaseAuthorization`), or `[Authorize]` fails every request.
- Moving an endpoint between `commercial` and `public` is a change of its route, so a breaking change for its callers.

## Related

[ADR-0005](0005-where-code-lives.md), [ADR-0022](0022-web-api-answers.md), [ADR-0024](0024-swagger.md).
