# ADR-0133: What is not brought back from the old framework, and the risks we accept

- **Status:** Accepted
- **Date:** 2026-11-03

## Context

The old framework (the branch `dev-backup`) was read project by project against the new tree ([the analysis](../old-framework/README.md)). Most projects are covered or were dropped for a reason that an ADR already records. What the analysis left open is a short list of things the new tree does not have, where the maintainer decided on 2026-11-03 that the absence is deliberate, and a few risks that are accepted rather than removed. Without a record, the next reader of the old code would ask the same questions again.

## Decision

**Not brought back, and not planned until a product needs them** (each one gets its own ADR if it comes back):

- **Azure Service Bus** (`EventBus.Azure`). No service uses it; the old project was a half-working prototype (it completed a message whose handler failed, and resolved handlers from the root provider). The open item in the catalog that said to add it is closed. A new broker is added the way the RabbitMQ and Kafka ones are ([ADR-0023](0023-event-bus.md), [ADR-0003](0003-abstractions-separate-from-providers.md)) when a product asks.
- **An embedded database provider** (`EntityFrameworkCore.Sqlite`). It never had code in the old framework. Repository behaviour is tested against the real databases with Testcontainers ([ADR-0010](0010-integration-tests-with-testcontainers.md)).
- **The old Redis lock as it was** (`RedisLockService` was a get-then-set, not atomic, and one lock only). What is needed, a limit on how many run at once in every pod (a limit of 1 is a lock), is [ADR-0143](0143-a-limit-on-how-many-run-at-once-holds-across-every-pod.md); a job may also claim its row with a compare-and-set in its own database, and the startup lock of a database exists ([ADR-0118](0118-a-database-is-prepared-by-the-host-that-owns-it-one-pod-at-a-time.md)).
- **MongoDB sessions and transactions.** Added when a service has an invariant that spans documents (it needs a replica set).
- **A feed of development packages** (the old repository published `-dev` packages to GitHub Packages on every commit). `scripts/pack-local.sh` does this for a consumer on one machine; the release path is [ADR-0115](0115-versions-and-releases-two-branches-and-a-version-made-from-nuget.md), [ADR-0130](0130-nuget-packages-are-pushed-with-trusted-publishing-not-an-api-key.md).
- **A string encryption helper.** The old one had a hard-coded passphrase, IV and salt and unauthenticated AES-CBC. A secret at rest is protected with ASP.NET Data Protection or the platform's secret store.
- **The rest of the old kitchen sink** (the `Json`, `Timing`, `ObjectMapping`, `AutoMapper`, `VirtualFileSystem`, `UI`, `UI.Navigation`, `Mvc.UI` projects, the DI marker interfaces, the exception notifier): see the analysis for each.

**Risks accepted:**

- **No outbox.** An event published after the data was saved can be lost if the process dies between the commit and the publish. The publisher retries a failed publish ([ADR-0023](0023-event-bus.md)) and a consumer is protected from a message that comes twice by the inbox ([ADR-0067](0067-the-event-inbox-is-opt-in-and-a-decorator.md)), but a lost event is not recovered by the framework. A product whose events must not be lost reconciles by a job that compares its data with what was published. This is the position of the Hsx analysis ("outbox: not needed", `samples/hsx/docs/hhs-analysis.md`); the old hhs code names it a known gap, and the decision is that it stays one. An outbox decorator, built like the inbox, can be added without changing the bus if a product needs it.
- **The wire is not compatible with the old services.** The `UserRoles` header is a JSON array (it was a comma-joined text) and `Producer` is the name of the client (it was `Exchange_Client`). Old and new services are not run together; the new framework does not keep compatibility with the old one.


## Consequences

The new tree stays small and the next reader finds the reason here instead of in the old code. A product that needs one of these has a place to start (the analysis says what the old code did wrong and what the shape should be). The cost is that the accepted risks are the product's to manage: a service with events that must not be lost builds the reconciliation or asks for the outbox.

## Related

[The old framework analysis](../old-framework/README.md), [ADR-0002](0002-one-package-one-capability.md), [ADR-0003](0003-abstractions-separate-from-providers.md), [ADR-0005](0005-where-code-lives.md), [ADR-0023](0023-event-bus.md), [ADR-0067](0067-the-event-inbox-is-opt-in-and-a-decorator.md).
