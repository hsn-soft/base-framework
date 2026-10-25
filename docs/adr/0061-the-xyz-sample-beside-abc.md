# ADR-0061: The Xyz sample beside Abc

- **Status:** Accepted
- **Date:** 2026-10-25

## Context

The first Xyz sample was a small order system written before the framework had its present shape (own Mongo/RabbitMQ composition, a hand-made user list, an HMAC key shared by every host). Abc grew into the reference: tenants, audit, soft delete, repositories, two stores in a service, environments, the order flow with its caller in the envelope. A second system that does the same job with other choices shows that the framework is not shaped by one product, and it exercises the parts Abc does not use: PostgreSQL alone, Kafka, one auth server, one gateway, services without class-library layers.

## Decision

- **Xyz is rewritten beside Abc and stays in parity with it** (users and their history, products and the stock history, orders, shipments, customers, payments, shipment history, the same events, the same five environments, the same RS256 tokens): when Abc changes a behaviour, Xyz gets the same one unless it is one of the differences below. The old Xyz projects are gone; nobody depends on them.
- **The differences are the point:**
  - one **auth server** that owns the users (`User`, `UserHistory`, users CRUD) and issues the tokens (RS256, refresh tokens in Redis, rotated on use); no identity service;
  - one **commercial gateway** (YARP from the route registry, Redis rate limit through `UseXyzGateway(beforeProxy)`);
  - the layers of a service are **folders of its Host project** (`Domain`, `Application`, `Infrastructure`, `Controllers`, `EventHandlers`), namespaces `Xyz.<Service>.<Layer>`; one `Xyz.Shared` project instead of the several `Abc.Shared.*` (hosting, gateway shell, event contracts, rate limit);
  - **PostgreSQL only**: a service with two stores has two contexts on two databases (`Xyz_<Context>_<Dev|Stage|Uat|Prod>`; product: products and stock history, shipment: shipments/customers/payments and shipment history, auth server: users and user history);
  - **Kafka** with the exchange name `XYZ`: topics `xyz.<Event>`, one consumer group per service and event (`XYZ_<Service>_<Event>`, [ADR-0062](0062-a-kafka-consumer-for-every-service-and-event.md)), one message at a time. `SendAsync` to one service stays unsupported on Kafka ([ADR-0057](0057-the-caller-of-a-message-and-publish-or-send.md)); Xyz publishes only.
- **Two framework gaps the sample showed were closed in the same change:** two contexts on the same provider in one host no longer collide on the name of their health check (the second is `<provider>-<Context>`), and the Kafka client tries again when the broker does not answer yet (topic creation retried 10 times, 2 s apart) instead of stopping the host on the first refusal.
- The ports are 5100 (gateway), 5110 (auth server), 5120 order, 5130 product, 5140 shipment; the RSA key pair is made with `scripts/generate-rsa-keys.cs` and git-ignored, the key of Abc is never reused ([ADR-0031](0031-jwt-signed-with-an-rsa-private-key.md)).

## Consequences

- Both systems run side by side on one machine (different ports, databases, exchange/topic prefixes and Redis key prefixes) and `samples/xyz/smoke-test.sh` runs the four outcomes of an order through the gateway.
- A change to the shape of an Abc service has to be repeated in Xyz by hand; there is no shared template. That is accepted: the repetition is what proves the framework carries it.
- Folders instead of projects mean the layers are not enforced by references; the convention (Domain knows nobody, Application knows Domain, Infrastructure knows both) is kept by review.

## Related

[ADR-0054](0054-the-order-flow-through-events.md), [ADR-0055](0055-environments-of-the-abc-sample.md), [ADR-0057](0057-the-caller-of-a-message-and-publish-or-send.md), [ADR-0059](0059-where-events-and-their-handlers-live.md), [ADR-0060](0060-customer-payment-and-an-internal-event-in-the-shipment-service.md).
