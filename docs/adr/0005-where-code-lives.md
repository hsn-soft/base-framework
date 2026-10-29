# ADR-0005: Where a piece of code lives: the framework, a product's shared project, or a service

- **Status:** Accepted
- **Date:** 2026-10-22

## Context

Code that several services repeat tempts everyone to move it into the framework. But the framework is published to NuGet, versioned, tested to 99 % and used by every product; a rule that
only one product has does not belong there. Conversely, copy-pasting infrastructure into every microservice is what the framework exists to stop. We needed a test that decides
quickly.

## Decision

Ask, in this order:

1. **Is it infrastructure that any product would use unchanged** (an event bus, a cache, a health endpoint, a web api answer format, a logging pipeline)? It goes in the **framework**, as its own
   capability package (ADR-0002), with a unit test project and, for a provider, integration tests.
2. **Does it know the product** (its roles, its rate-limit policy, how its events carry the user, its handler base class, its request DTOs, its error texts)? It goes in the product's
   **shared project** (`{Product}.Shared`, `Hhs.Shared`), which composes the framework for all of that product's services: what is registered and in which order
   (`Add{Product}Api`, `Use{Product}Api`), and the product's own policies.
3. **Does only one service use it?** It stays in that **service**, in its layers (Domain, Application, EntityFrameworkCore, Http.Host; see [ADR-0006](0006-repository-folders-and-the-solution-file.md)).

The framework **knows no product**: no product names, no product settings, no product default; every default is neutral and overridable by configuration.

The three places are also three kinds of logs: the framework writes `FrameworkLog`s (`HsnSoft.Base.*`), the shared project `SharedLog`s (`{AppNamespace}.Shared.*`), the services `AppLog`s
(`{AppNamespace}.*`) (see [ADR-0016](0016-log-types-by-namespace.md)). **A product's shared library is not the framework.**

Examples decided so far: a gateway's Redis rate limit is the product's shared project (a product policy); `HttpContextEventHeaderProvider` (where a message's user and client come from) is
the product's shared project; the handler base class with inbox and idempotency is the product's; `ReQueuedEtoHandler` is the product's; `AddBaseWebApi` is the framework (every web api answers the same way).

## Consequences

- A change in a product's policy never needs a framework release.
- Moving something from a shared project into the framework needs a second product that wants it unchanged.
- Reviews ask "which of the three is it?" before "is it well written?".

## Related

[ADR-0002](0002-one-package-one-capability.md), [ADR-0006](0006-repository-folders-and-the-solution-file.md), [ADR-0016](0016-log-types-by-namespace.md), root `README.md` (The product decides the composition).
