# ADR-0091: The Edx integrations are reached directly; the services are reached through the gateway

- **Status:** Accepted
- **Date:** 2026-10-28

## Context

The gateway ([ADR-0078](0078-the-edx-sample-a-file-intake-system.md)) fronted every Edx host, the intake and integration hosts among them (REST, WCF and, on a second port, gRPC). The two integrations ([ADR-0090](0090-edx-services-are-named-integration-firm-invoice-and-integration-gib-invoice.md)) are different from the services: they face a party outside the system (the customers' programs, the center), speak three protocols (REST, gRPC, WCF) whose clients do not want a proxy
in between, move big files, and each has its own MongoDB, Redis and Kafka. The services (identity, invoice) and the auth server are what the back office and the integrations' own users reach through one front door.

## Decision

- **`integration/firm-invoice` and `integration/gib-invoice` are called directly**, each on its own address (firm invoice: REST `:5510`, gRPC `:5511`, WCF `:5512`; GİB invoice: REST `:5540`, the post box WCF `:5541`), under the routes of their controllers (`/api/intake-service/v1/commercial/...`, `/api/intake-service/v1/back-office/...`, `/api/integration-service/v1/commercial/...`, `/wcf/intake.svc`). The gateway has no route, cluster or
  Swagger entry for them, and no gRPC endpoint any more. The token still comes from the auth server (through the gateway); the integrations validate it themselves, like every host.
- **The services are reached through the gateway** (`identity-service`, `invoice-service`, `auth-server`), as before.
- **Each integration has its own Redis (the permission grants, the caches), Kafka (the event bus) and MongoDB** (its collections, its inbox).
- **A browser may call an integration directly** from the origins in its `Cors:AllowedOrigins` (development: the back office on `http://localhost:4200`); a service that sets none allows no origin. The registration is the shared `AddEdxMicroservice`/`UseEdxMicroservice`, so another host that is called directly only needs the setting.
- The back office (`urlOf` in `core/config.ts`), the customer examples (`EDX_FIRM_INVOICE_REST`, `_GRPC`, `_WCF`), the Python client and the smoke test call the integrations directly and the rest through the gateway.

## Consequences

- A customer's gRPC and WCF clients reach the host without a proxy that would have to speak HTTP/2 or SOAP; a deployment puts its own front (a load balancer) before each integration if it wants one.
- The integrations carry what the gateway did for the others: CORS (above), the size of a request (already their own setting), and the limits of the network are their deployment's.
- The route of a controller (`api/intake-service/v1/commercial/...`) is now the public address of the integration; renaming it to the new service names is a later decision.

## Related

[ADR-0078](0078-the-edx-sample-a-file-intake-system.md), [ADR-0086](0086-the-intake-service-is-three-hosts-rest-grpc-and-wcf.md), [ADR-0089](0089-the-edx-integration-service-sender-unit-and-post-box.md), [ADR-0090](0090-edx-services-are-named-integration-firm-invoice-and-integration-gib-invoice.md); `samples/edx/gateway`, `samples/edx/app/back-office/src/app/core/config.ts`.
