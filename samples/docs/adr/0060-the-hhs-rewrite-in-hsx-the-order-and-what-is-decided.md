# ADR-0060: The hhs rewrite in Hsx: the order, and what is decided

- **Status:** Accepted
- **Date:** 2026-11-01 (extends [ADR-0058](0058-hsx-imitates-hhs-to-test-the-new-framework.md), [ADR-0059](0059-hsx-has-two-mongodb-services-and-the-four-environments.md); the study is in [hsx/docs/hhs-analysis.md](../../hsx/docs/hhs-analysis.md))

## Decision

- **Hsx becomes the whole of hhs, in the new shape**: the business of hhs stays, its technical shape is chosen again on the framework, and what hhs got wrong (the study lists it) is not carried over. Where the framework lacks what hhs does, the gap is written down and closed in the framework.
- **The core first**: identity with the auth server, administration, content, text-normalizer, video-generator, the scheduler. Event-manager and feedr-admanager after.
- **The services talk through events on RabbitMQ**, as hhs does; one RabbitMQ (the one of this machine), **a virtual host for each environment** (`hsx` for development and docker, `hsx-uat`, `hsx-prod`), the exchange `HSX`, the queues of each service named by its `ConsumerClientName`. The events that cross a service are in `Hsx.Shared.Contracts`
  (`Events/Content`, `Events/TextNormalizer`, `Events/VideoGenerator`, `Events/Common`); each carries its scope key and what it is about (`RefContentId`, `RefContentType`: the tenant travels in the envelope). The steps of a service that only talk to itself are **not** events yet: whether they become a work item with a worker
  of its own is decided with text-normalizer, with code in hand. What changes at once is that polling and retry run **inside the service** (a hosted worker), not by a scheduler that calls an endpoint every few seconds.
- **The customer settings belong to each service**: hhs has one `CustomerVpSetting` in three services because three services need settings of a customer; they are three different things (the content settings, the normalizer settings, the video settings), each owned by its service, and not a copy of one. In Hsx they are
  `CustomerContentVpSetting`, `CustomerNormalizerVpSetting` and `CustomerVideoVpSetting` (the content one is still called `CustomerVpSetting` and is renamed with the next change of its schema).
- **The web of the customers' sites** (the SDK, the player, the loader of each customer) is **not** part of Hsx: its technology will change (Node.js), so it is another project; Hsx only knows what it calls (`contents/get-or-create`, `create-ad-result`) and keeps that endpoint on a public gateway.
- `ProductTypes` has distinct values (hhs has three equal to 12) and `ContentTypes` replaces `ContentType` (no `None`).

## Consequences

- The databases of the PostgreSQL services are made with `EnsureCreated`, which does **not** add a table to a database that exists: a new aggregate, the inbox or a rename needs the database to be made again, or migrations (hhs migrates). This is a gap of the framework (it has no migration step) to decide before the schemas start to change.
- The inbox of the event bus is added to a service together with its first handler, not before.
