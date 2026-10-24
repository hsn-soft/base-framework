# ADR-0003: Abstractions are separate from their providers

- **Status:** Accepted
- **Date:** 2026-10-22

## Context

A product picks a broker (RabbitMQ or Kafka), a blob store (MinIO, later Azure) or a database (PostgreSQL or SQL Server) per service, and must be able to change
it with one registration. If the contract and one implementation share a package, choosing another implementation drags the first one along.

## Decision

- The **contract** is a package of its own (`EventBus`, `BlobStoring`, `EntityFrameworkCore`, `Logging`, `AspNetCore.Swashbuckle`). Each **provider** is a separate
  package that references its contract (`EventBus.RabbitMQ`, `EventBus.Kafka`, `BlobStoring.Minio`, `EntityFrameworkCore.PostgreSql`, `EntityFrameworkCore.SqlServer`,
  `Logging.Serilog`).
- **A provider references its abstraction, never the other way round.** An abstraction knows no provider.
- A provider only moves the bytes or talks to the technology; the shared behaviour (the envelope, naming, the dispatcher, retry) is in the abstraction so every provider
  behaves the same way.
- A layer that builds on another capability is its own package too (`AspNetCore.SwaggerGateway` on `AspNetCore.Swashbuckle`): it references the one below it and nothing else new.

## Consequences

- A product registers one provider (`AddRabbitMqEventBus`) and can swap it; handlers and publishers do not change.
- A new provider (Azure Service Bus, Azure Blob) is a sibling package with its own tests, including integration tests (see [ADR-0010](0010-integration-tests-with-testcontainers.md)).
- Behaviour that is common to providers must live in the abstraction, even when only one provider uses it today; otherwise the second provider diverges.

## Related

[ADR-0002](0002-one-package-one-capability.md), [ADR-0004](0004-core-once-and-the-dependency-graph.md), `docs/EventBus.md`, `docs/Logging.md`.
