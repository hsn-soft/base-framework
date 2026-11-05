# ADR-0060: The hhs rewrite in Hsx: the order, and what is decided

- **Status:** Accepted
- **Date:** 2026-11-01 (extends [ADR-0058](0058-hsx-imitates-hhs-to-test-the-new-framework.md), [ADR-0059](0059-hsx-has-two-mongodb-services-and-the-four-environments.md); the study is in [hsx/docs/hhs-analysis.md](../../hsx/docs/hhs-analysis.md))

## Decision

- **Hsx becomes the whole of hhs, in the new shape**: the business of hhs stays, its technical shape is chosen again on the framework, and what hhs got wrong (the study lists it) is not carried over. Where the framework lacks what hhs does, the gap is written down and closed in the framework.
- **The core first**: identity with the auth server, administration, content, text-normalizer, video-generator, the scheduler. Event-manager and feedr-admanager after.
- **The database schema is evolved by migrations** (EF Core), run by a startup step of the framework (`AddMigrationStartupStep`, [framework ADR-0131](../../../docs/adr/0131-a-schema-that-evolves-is-migrated-by-a-startup-step.md)), as hhs migrates; `EnsureCreated` stays for a service that never changes its schema.
- **The services talk through events on RabbitMQ**, as hhs does; one RabbitMQ (the one of this machine), **a virtual host for each environment** (`hsx` for development and docker, `hsx-uat`, `hsx-prod`), the exchange `HSX`, the queues of each service named by its `ConsumerClientName`. The events that cross a service are in `Hsx.Shared.Contracts`
  (`Events/Content`, `Events/TextNormalizer`, `Events/VideoGenerator`, `Events/Common`: the hhs events, one for each step the content service keeps, and one `MilestoneFailedEto` for a failure); each carries its scope key and what it is about (`RefContentId`, `RefContentType`: the tenant travels in the envelope). The steps a service takes by sending an event to itself are events too, as in hhs, in the Application project of that service: **the number of events is not reduced** (a work item, a row with the next step handled by a worker, was proposed and the maintainer does not want it).
  Polling and retry running inside the service (a hosted worker) instead of a scheduler that calls an endpoint every few seconds stays a proposal, to be confirmed when text-normalizer is written.
- **The customer settings belong to each service**: hhs has one `CustomerVpSetting` in three services because three services need settings of a customer; they are three different things (the content settings, the normalizer settings, the video settings), each owned by its service, and not a copy of one. In Hsx they are
  `CustomerContentVpSetting`, `CustomerNormalizerVpSetting` and `CustomerVideoVpSetting` (the content one was renamed from `CustomerVpSetting` by the second migration of the content service).
- **The web of the customers' sites** (the SDK, the player, the loader of each customer) is **not** part of Hsx: its technology will change (Node.js), so it is another project; Hsx only knows what it calls (`contents/get-or-create`, `create-ad-result`) and keeps that endpoint on a public gateway.
- `ProductTypes` has distinct values (hhs has three equal to 12) and `ContentTypes` replaces `ContentType` (no `None`).

## Consequences

- The PostgreSQL services (identity, content) use EF migrations from now on: `Migrations/` of their persistence project, `Initial` first. The databases that `EnsureCreated` had made (`Hsx_Identity_*`, `Hsx_Content_*` of development, uat and production) were dropped once, since their first migration would fail on tables that exist. The MongoDB services create collections and indexes on their own and need no migrations.
- The inbox of the event bus is added to a service together with its first handler, not before.
