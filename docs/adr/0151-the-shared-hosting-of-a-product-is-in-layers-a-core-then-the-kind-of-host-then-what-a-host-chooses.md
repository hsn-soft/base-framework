# ADR-0151: The shared hosting of a product is in layers: a core, then the kind of host, then what a host chooses

- **Status:** Accepted
- **Date:** 2026-11-05

## Context

A product built on the framework has hosts of several kinds (a gateway, a microservice, a gRPC or WCF service, an agent that runs jobs) and each repeats the same composition of framework calls, so the product keeps it in shared hosting projects. Put in one `{Product}.Shared.Hosting` that every host takes, it drifts to what the web hosts need (the web api shell, Swagger, the bearer token, the ASP.NET Core reference), and an agent or a host without a message broker, a cache or a database takes it for nothing.

## Decision

The shared hosting of a product is in three layers, each only what all the hosts below it need:

1. **`{Product}.Shared.Hosting`** (every host): the logging. No ASP.NET, no token, no tenant, no message broker, no cache, no database.
2. **The kind of host**: `{Product}.Shared.Hosting.Http` (every web host, all of them ASP.NET Core: the web api shell, Swagger, the exception handling, the bearer token), and on it `.Gateways`, `.Microservices` (and `.Grpc`); an agent is not a web host and has `.Agents` on the core (its scheduling plumbing written once for every agent).
3. **What a host chooses**, each its own package or framework package referenced only by the host that uses it: the message broker (`.EventBus`), Redis, a database, the texts of several languages. A shared `{Product}.Shared.Hosting.<Name>` package exists only where the product adds something of its own to the framework package; otherwise the host references the framework package.

A new concern goes to the lowest layer that all its users share, never to a layer above where it is not needed.

## Consequences

A host restores only what its kind and its choices need. A product that has one `Shared.Hosting` splits it when a host of another kind appears, and the namespace of the web calls moves to `.Http`. The framework itself keeps one package per capability (ADR-0002); this is how a product composes them.

## Related

[ADR-0002](0002-one-package-one-capability.md), [ADR-0005](0005-where-code-lives.md), [docs/Hosting.md](../Hosting.md), the rule [infrastructure-hosting](../../.claude/rules/infrastructure-hosting.md).
