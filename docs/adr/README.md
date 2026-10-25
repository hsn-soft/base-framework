# Architecture decision records

The decisions behind this repository, one short file each: **why** it is the way it is, what was decided, and what it costs. They are the memory of the project: a new session, or a new maintainer, starts here.
The reference documents in [`docs/`](../README.md) say *how to use* a feature; these say *why*, and the reference documents link here.

**Read this index first**, then the ADRs of the area you are about to touch. **Every new architecture decision is added here when it is taken** (see [ADR-0001](0001-record-architecture-decisions.md) for what deserves one and
how to supersede one), in the same pull request as the change, and the table below is updated.

New ADR: copy [0000-template.md](0000-template.md) to `NNNN-short-title.md` (the next number), fill it in, add a row.

## Process

| ADR | Decision | Status | Date |
|---|---|---|---|
| [0001](0001-record-architecture-decisions.md) | Architecture decisions are recorded as ADRs in `docs/adr` | Accepted | 2026-10-25 |

## Packages and where code lives

| ADR | Decision | Status | Date |
|---|---|---|---|
| [0002](0002-one-package-one-capability.md) | One package, one capability | Accepted | 2026-10-22 |
| [0003](0003-abstractions-separate-from-providers.md) | Abstractions are separate from their providers | Accepted | 2026-10-22 |
| [0004](0004-core-once-and-the-dependency-graph.md) | Packages reference `HsnSoft.Base.Core` once, and form this dependency graph | Accepted | 2026-10-22 |
| [0005](0005-where-code-lives.md) | Where a piece of code lives: the framework, a product's shared project, or a service | Accepted | 2026-10-22 |
| [0006](0006-repository-and-solution-layout.md) | Repository folders, the solution file and the sample systems | Accepted | 2026-10-25 |

## Services

| ADR | Decision | Status | Date |
|---|---|---|---|
| [0065](0065-the-consumers-outlive-their-own-failures.md) | The consumers outlive their own failures | Accepted | 2026-10-26 |
| [0064](0064-the-stock-is-changed-in-one-step.md) | The stock is changed in one step of the store (an atomic update on MongoDB too) | Accepted | 2026-10-26 |
| [0063](0063-kafka-topics-are-named-after-the-exchange.md) | Kafka topics are named after the exchange; the topic prefix setting is gone | Accepted | 2026-10-25 |
| [0062](0062-a-kafka-consumer-for-every-service-and-event.md) | A Kafka consumer, in a group of its own, for every service and event | Accepted | 2026-10-25 |
| [0061](0061-the-xyz-sample-beside-abc.md) | The Xyz sample beside Abc | Accepted | 2026-10-25 |
| [0060](0060-customer-payment-and-an-internal-event-in-the-shipment-service.md) | Customer, payment and an internal event in the shipment service | Accepted | 2026-10-25 |
| [0059](0059-where-events-and-their-handlers-live.md) | Where the events and their handlers live | Accepted | 2026-10-25 |
| [0058](0058-names-on-the-broker-in-the-abc-sample.md) | The names on the broker in the Abc sample | Accepted | 2026-10-25 |
| [0057](0057-the-caller-of-a-message-and-publish-or-send.md) | The caller of a message is in the envelope; publish an event, send a command | Accepted | 2026-10-25 |
| [0056](0056-tables-and-collections-are-named-like-the-entity.md) | Tables and collections are named like the entity | Accepted | 2026-10-25 |
| [0055](0055-environments-of-the-abc-sample.md) | The environments of the Abc sample, and the names of its databases | Accepted | 2026-10-25 |
| [0054](0054-the-order-flow-through-events.md) | The order flow runs through events: order, product, shipment, order | Accepted | 2026-10-25 |
| [0053](0053-more-than-one-store-in-a-service.md) | More than one store in a service: a second MongoDB under a key, a relational project beside a MongoDB one | Accepted | 2026-10-25 |
| [0052](0052-the-history-of-the-users.md) | The history of the users: a CRUD of its own, written by the identity service and the auth server | Accepted | 2026-10-25 |
| [0051](0051-every-context-is-named-by-its-properties-class.md) | Every context of a sample is named by its properties class; the provider setting is gone | Accepted | 2026-10-25 |
| [0050](0050-a-context-has-a-properties-class-for-its-database.md) | A context has a properties class that says where and on what its data is | Accepted | 2026-10-25 |
| [0049](0049-abc-databases-and-provider-references.md) | The Abc services' databases, and a project references only the provider it uses | Accepted | 2026-10-25 |
| [0048](0048-bulk-and-set-based-changes.md) | Many entities at once, and changes made without loading | Accepted | 2026-10-25 |
| [0047](0047-relational-provider-chosen-by-a-setting.md) | The relational provider is chosen by a setting | Accepted | 2026-10-25 |
| [0046](0046-dto-projection-without-a-mapper.md) | A DTO is read by a projection made from the DTO, without a mapper | Accepted | 2026-10-25 |
| [0045](0045-repository-levels-relational-and-document.md) | The repository has levels: a common contract, one interface and one implementation for each kind of store | Accepted | 2026-10-25 |
| [0044](0044-validation-of-requests.md) | Validation of requests: attributes, rules and the messages for the client | Accepted | 2026-10-25 |
| [0043](0043-project-file-reference-order.md) | The order of the references in a project file | Accepted | 2026-10-25 |
| [0042](0042-ddd-domain-and-application-packages.md) | `Ddd.Domain` and `Ddd.Application`: entity bases, DTO bases, and the generic repository | Accepted | 2026-10-25 |
| [0041](0041-application-service-base-in-the-framework.md) | `ApplicationService` is the framework's; the application services of a service derive from its base | Accepted | 2026-10-25 |
| [0040](0040-api-controller-base-in-the-framework.md) | `ApiControllerBase` is the framework's; a service's base controller derives from it | Accepted | 2026-10-25 |
| [0039](0039-guard-and-translating-a-response-body.md) | `Guard` for the checks of the domain, and translating a text in a response body | Accepted | 2026-10-25 |
| [0038](0038-localization.md) | Localization: embedded JSON texts, merged from the innermost layer out, in the language of the request | Accepted | 2026-10-25 |
| [0037](0037-audit-soft-delete-and-tenants.md) | Audit, soft delete and tenants of an entity | Accepted | 2026-10-25 |
| [0036](0036-icurrentuser-in-the-framework.md) | `ICurrentUser` is the framework's; the JWT authentication registers it | Accepted | 2026-10-25 |
| [0035](0035-the-persistence-project-registers-its-database.md) | The persistence project registers its database provider | Accepted | 2026-10-25 |
| [0034](0034-redis-and-rabbitmq-in-the-abc-hosts.md) | Redis and RabbitMQ in the Abc hosts | Accepted | 2026-10-25 |
| [0033](0033-the-abc-services-crud-pattern.md) | The CRUD pattern of the Abc services: order, product and shipment follow the identity service | Accepted | 2026-10-25 |
| [0032](0032-the-auth-server-reads-the-users-of-the-identity-service.md) | The auth server reads the users of the identity service; the author of a row is the `sub` of the token | Accepted | 2026-10-25 |
| [0031](0031-jwt-signed-with-an-rsa-private-key.md) | The JWT is signed with an RSA private key; the other hosts only have the public key | Accepted | 2026-10-25 |
| [0030](0030-microservice-project-pattern.md) | Every microservice has the same four projects; only the persistence project varies | Accepted | 2026-10-25 |
| [0029](0029-one-auth-server-issues-the-token.md) | One auth server issues the JWT; every other host only validates it | Accepted | 2026-10-25 |
| [0028](0028-shared-hosting-projects.md) | A product's hosts share their composition in `{Product}.Shared.Hosting`, with one specialization per kind of host | Accepted | 2026-10-25 |
| [0027](0027-gateways-per-audience.md) | One gateway per audience, built from a route registry; the gateway forwards, the services decide who may call | Accepted | 2026-10-25 |
| [0026](0026-route-pattern-and-jwt-by-default.md) | Routes are `api/{area}/v1/{commercial|public}/{controller}`, and every endpoint needs a JWT unless it says otherwise | Accepted | 2026-10-25 |

## Branching

| ADR | Decision | Status | Date |
|---|---|---|---|
| [0025](0025-the-rewrite-lives-on-feat-new-framework.md) | The rewrite lives on `feat/new-framework` and is not merged into `dev` until the whole framework exists | Accepted | 2026-10-25 |

## Build, tests, CI and the developer workflow

| ADR | Decision | Status | Date |
|---|---|---|---|
| [0007](0007-build-settings-and-versioning.md) | Shared build settings, one version, and how a package is published | Accepted | 2026-10-22 |
| [0008](0008-code-style-and-analyzers.md) | Code style: the `.editorconfig` is the only source, analyzers run in the build | Accepted | 2026-10-22 |
| [0009](0009-unit-tests-every-package.md) | Every package has a unit test project, found by name | Accepted | 2026-10-22 |
| [0010](0010-integration-tests-with-testcontainers.md) | Integration tests start the real services with Testcontainers | Accepted | 2026-10-22 |
| [0011](0011-coverage-gate.md) | The coverage gate merges unit and integration coverage and fails below 99 % of `src` | Accepted | 2026-10-22 |
| [0012](0012-ci-workflow-and-what-breaks-a-pull-request.md) | The CI workflow, and what breaks a pull request | Accepted | 2026-10-22 |
| [0013](0013-git-hooks.md) | Git hooks: format on commit, build and unit tests on push | Accepted | 2026-10-22 |
| [0014](0014-detector-before-a-pull-request.md) | The detector runs the CI's checks on the developer's machine before a pull request | Accepted | 2026-10-23 |

## Logging

| ADR | Decision | Status | Date |
|---|---|---|---|
| [0015](0015-logging-architecture.md) | Everything logs through `ILogger`; a provider decides what a log is and where it goes | Accepted | 2026-10-24 |
| [0016](0016-log-types-by-namespace.md) | The log type is told by the namespace of the class that wrote it | Accepted | 2026-10-25 |
| [0017](0017-two-places-two-levels.md) | The console and what is kept have separate levels | Accepted | 2026-10-25 |
| [0018](0018-chain-logs.md) | The chain logs: a request and a message are each a pair of logs, with ids that tie a whole chain together | Accepted | 2026-10-24 |
| [0019](0019-log-fields-pipeline.md) | Which fields a log has: a pipeline, a noise filter by type, and a map measured from a run | Accepted | 2026-10-25 |
| [0020](0020-failure-logged-once.md) | A failure is logged once, with where it was thrown and why; a known error is a Warning | Accepted | 2026-10-24 |
| [0021](0021-sensitive-data-by-attributes.md) | What is sensitive is said by the classes, never by settings | Accepted | 2026-10-23 |

## Web api, events, Swagger

| ADR | Decision | Status | Date |
|---|---|---|---|
| [0022](0022-web-api-answers.md) | Every answer is a `BaseResponse`; what a client learns of a 500 is a key and a trace id | Accepted | 2026-10-24 |
| [0023](0023-event-bus.md) | The event bus is a port of the production code: same wire, same topology, same failure handling | Accepted | 2026-10-23 |
| [0024](0024-swagger.md) | Swagger is its own package, off where a service runs for real, and a gateway shows its services | Accepted | 2026-10-25 |

## The decisions in a few lines

- **Packages:** one capability each, no forced dependencies; abstractions and providers apart; everything references Core once; the framework knows no product. Code goes to the framework, the product's shared project or a service by the test in ADR-0005.
- **Build and workflow:** `.editorconfig` is the only style, warnings are errors in CI, pre-commit formats, pre-push builds and runs unit tests, the detector runs the whole CI locally, the CI merges unit and integration coverage and fails below 99 %.
- **Logging:** everything uses `ILogger`; the type of a log is the namespace of its writer (`HsnSoft.Base` framework, `{App}.Shared` shared, `{App}` app, the rest platform); the console is told by `Logging:LogLevel`, Graylog/file by `PersistentLogger`; Debug and Trace are never kept;
  a failure is logged once; sensitive data is said by `[SensitiveData]`; the field map is measured.

## Open items (not decided or not done)

- Port the Kafka consumer and add an Azure Service Bus project the same way as RabbitMQ ([ADR-0023](0023-event-bus.md)); bridge the Kafka client's logs to `ILogger`.
- Show the `BaseResponse` envelope in the Swagger document ([ADR-0024](0024-swagger.md)).
- A test that fixes the allowed fields per log type, and configurable per-type `RemoveFields`/`AddFields` ([ADR-0019](0019-log-fields-pipeline.md)).
- A `SharedLog` in a measured run (the Xyz shared library logs only when Redis is down), so its column of `docs/GraylogFields.md` has data.
- Revoke and rotate the SonarQube token that was once pasted in a conversation; keep tokens in `scripts/sonar/.env` only.
- A **draft** pull request from `feat/new-framework` into `dev` (never merged until the framework is complete, [ADR-0025](0025-the-rewrite-lives-on-feat-new-framework.md)), to see the CI run on Linux for the first time.
