# Architecture decision records: full catalog

The decisions behind this repository, one short file each: **why** it is the way it is, what was decided, and what it costs. They are the memory of the project: a new session, or a new maintainer, starts here.
The reference documents in [`docs/`](../README.md) say *how to use* a feature; these say *why*, and the reference documents link here.

Start with the short [ADR guide](README.md), then use this catalog to locate and read only decisions relevant to your task. **Every new architecture decision is added here when it is taken** (see [ADR-0001](0001-record-architecture-decisions.md) for what deserves one and
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
| [0006](0006-repository-folders-and-the-solution-file.md) | Repository folders and the solution file | Accepted | 2026-10-25 |
| [0133](0133-what-is-not-brought-back-from-the-old-framework-and-the-risks-we-accept.md) | What is not brought back from the old framework, and the risks we accept (no outbox, no Azure Service Bus, no Sqlite, no distributed lock, no old-wire compatibility) | Accepted | 2026-11-03 |
| [0134](0134-package-versions-are-managed-centrally-and-a-library-awaits-with-configureawait-false.md) | Package versions are managed centrally in `Directory.Packages.props`, and a library awaits with `ConfigureAwait(false)` by rule (CA2007 is an error in `src/`) | Accepted | 2026-11-03 |
| [0135](0135-permissions-are-asked-without-a-web-framework-and-the-endpoint-layer-is-its-own-package.md) | The permission checker needs no web framework, and the endpoint layer is its own package (`Authorization.AspNetCore`) | Accepted | 2026-11-03 |
| [0136](0136-an-entity-is-protected-from-a-lost-update-by-an-opt-in-stamp.md) | An entity is protected from a lost update by an opt-in stamp (`IHasConcurrencyStamp`), in EF Core and in MongoDB; a conflict is a 409 | Accepted | 2026-11-03 |
| [0137](0137-the-helpers-of-the-old-core-come-back-only-when-a-consumer-used-them.md) | The helpers of the old `Core` come back only when a consumer used them: text keys in `Core`, the rest in `Extensions` | Accepted | 2026-11-03 |
| [0138](0138-a-call-to-another-service-has-a-correlation-id-safe-retries-and-a-token-that-goes-only-where-it-is-for.md) | A call to another service has the correlation id, safe retries and a token that goes only where it is for (`Http.Client`) | Accepted | 2026-11-03 |
| [0139](0139-the-host-writes-security-headers-and-names-the-origins-that-may-call-it.md) | The host writes security headers and names the origins that may call it (`UseBaseSecurityHeaders`, `Hosting:Cors`) | Accepted | 2026-11-03 |
| [0140](0140-the-blob-store-on-the-local-disk-takes-only-plain-names-and-writes-atomically.md) | The blob store on the local disk takes only plain names and writes atomically (`BlobStoring.FileSystem`) | Accepted | 2026-11-03 |
| [0141](0141-a-rendered-page-is-an-untrusted-caller-and-the-browser-is-a-pool-with-limits.md) | A rendered page is an untrusted caller, and the browser is a pool with limits (`Browser`, `Browser.Puppeteer`) | Accepted | 2026-11-03 |
| [0142](0142-the-hot-paths-are-measured-by-benchmarks-that-are-not-tests.md) | The hot paths are measured by benchmarks that are not tests (`benchmarks/`) | Accepted | 2026-11-03 |
| [0143](0143-a-limit-on-how-many-run-at-once-holds-across-every-pod.md) | A limit on how many run at once holds across every pod (`Concurrency`, `Concurrency.StackExchangeRedis`, `Concurrency.AspNetCore`) | Accepted | 2026-11-03 |
| [0144](0144-the-source-of-every-pull-request-has-a-known-branch-name.md) | Which branch may be merged into which: the rules of the source branch of a pull request (general, then those of each release branch); supersedes the source rules of ADR-0115 | Accepted | 2026-11-04 |
| [0145](0145-an-enum-has-a-key-and-a-name-through-the-display-attribute.md) | An enum has a key and a name through the Display attribute: `ToPrompt`, `ToDisplayName`, `ToEnumFromPrompt` in `Extensions` | Accepted | 2026-11-04 |
| [0146](0146-several-repository-calls-run-as-one-unit-of-work-through-iunitofwork.md) | Several repository calls run as one unit of work through `IUnitOfWork` (`EfCoreUnitOfWork`), never through a context | Accepted | 2026-11-04 |
| [0147](0147-a-response-is-an-ibasedto-so-a-response-is-found-by-its-type-not-by-a-name.md) | A response is an `IBaseDto`, so a response is found by its type and not by a name | Accepted | 2026-11-04 |
| [0148](0148-an-enum-is-stored-by-its-number-in-a-relational-database-never-by-its-name.md) | An enum is stored by its number in a relational database, never by its name | Accepted | 2026-11-05 |
| [0149](0149-normalize-changes-the-turkish-letters-first-and-keeps-only-capitals-digits-and-a-few-marks.md) | `StringHelper.Normalize` changes the Turkish letters first and keeps only capitals, digits and `@ . - _ +` | Accepted | 2026-11-05 |

## Building blocks, products and hosts

| ADR | Decision | Status | Date |
|---|---|---|---|
| [0132](0132-claude-works-on-its-own-branch-from-dev-and-never-merges-into-dev.md) | Claude works on its own branch cut from `dev` and never opens a pull request into `dev` or merges into it | Accepted | 2026-11-03 |
| [0131](0131-a-schema-that-evolves-is-migrated-by-a-startup-step.md) | A schema that evolves is migrated by a startup step (`AddMigrationStartupStep`, one pod at a time under the lock), instead of the schema step | Accepted | 2026-11-01 |
| [0130](0130-nuget-packages-are-pushed-with-trusted-publishing-not-an-api-key.md) | The packages are pushed to NuGet with trusted publishing (`NuGet/login`, a key that lives for the run), not with a long-lived API key | Accepted | 2026-11-01 |
| [0129](0129-an-entity-is-isolated-by-its-tenant-or-by-its-scope-never-both.md) | An entity is isolated by its tenant or by its scope (subscription), never both | Accepted | 2026-11-01 |
| [0128](0128-the-tenants-that-a-tenant-sees-are-the-ones-under-it-by-a-path-of-ids.md) | The tenants that a tenant sees are the ones under it, by a path of ids | Accepted | 2026-11-01 |
| [0127](0127-the-brokers-clients-do-not-take-the-pool-nor-open-a-connection-per-message-or-probe.md) | The brokers' clients do not take the thread pool, nor open a channel per message or a client per probe | Accepted | 2026-11-01 |
| [0126](0126-the-endpoints-a-scheduler-calls-are-authenticated-by-a-key-the-host-requires.md) | The endpoints a scheduler calls are authenticated by a key the host requires | Accepted | 2026-11-01 |
| [0125](0125-the-data-a-database-starts-with-is-written-by-seeders-that-are-steps-of-the-database.md) | The data a database starts with is written by seeders, which are steps of the database | Accepted | 2026-11-01 |
| [0124](0124-the-commands-sent-to-a-database-are-logged-only-when-a-setting-says-so.md) | The commands sent to a database are logged only when a setting says so | Accepted | 2026-11-01 |
| [0123](0123-the-pre-commit-style-format-builds-the-solution-first.md) | The pre-commit style format builds the solution first, so a using that is needed is never removed | Accepted | 2026-11-01 |
| [0122](0122-a-service-has-its-own-audiences-and-chooses-authentication-and-authorization.md) | A service has its own audience, the token has a list, the issuer is checked outside Development, and authentication and authorization are choices | Accepted | 2026-10-31 |
| [0121](0121-a-process-knows-who-it-is-and-the-thread-pool-minimum-is-a-setting.md) | A process knows who it is (`AppInfo`, `GET /`), the minimum of the thread pool is a setting, and the handlers of the event bus have a limit | Accepted | 2026-10-31 |
| [0120](0120-the-host-takes-its-settings-from-one-section-and-starts-safely.md) | The host takes its limits and proxies from one section, reads a `.env` file, and starts safely | Accepted | 2026-10-31 |
| [0119](0119-the-infrastructure-clients-share-their-connections-and-bound-their-waits.md) | The infrastructure clients share their connections, bound their waits and retry only what is safe to retry | Accepted | 2026-10-31 |
| [0118](0118-a-database-is-prepared-by-the-host-that-owns-it-one-pod-at-a-time.md) | A database is prepared by the host that owns it, one pod at a time, under a lock of the database | Accepted | 2026-10-31 |
| [0117](0117-the-health-endpoints-are-live-startup-and-ready-and-answer-with-the-checks.md) | The health endpoints are live, startup and ready, and answer with the checks | Accepted | 2026-10-31 |
| [0116](0116-the-release-branches-run-the-ci-on-a-push-and-have-their-own-badges.md) | The release branches run the CI on a push, and have badges of their own | Superseded by [ADR-0153](0153-the-ci-does-not-run-on-the-release-branches.md) | 2026-10-31 |
| [0115](0115-versions-and-releases-two-branches-and-a-version-made-from-nuget.md) | Versions and releases: a candidate branch and a regular branch, and a version made from what NuGet has | Accepted (the run on their pull requests is superseded by [ADR-0116](0116-the-release-branches-run-the-ci-on-a-push-and-have-their-own-badges.md), and the key of the push by [ADR-0130](0130-nuget-packages-are-pushed-with-trusted-publishing-not-an-api-key.md): none is kept) | 2026-10-31 |
| [0114](0114-the-readme-badges-are-made-by-the-ci-and-published-to-their-own-branch.md) | The badges of the README are made by the CI and published to a branch of their own | Accepted | 2026-10-31 |
| [0113](0113-the-ci-runs-on-pull-requests-weekly-on-dev-and-by-hand.md) | The CI runs on pull requests, weekly on `dev` and by hand, not on every push to `dev` | Accepted (the weekly run is removed by [ADR-0152](0152-the-ci-has-no-weekly-run.md)) | 2026-10-31 |
| [0112](0112-blob-reads-stream-and-uploads-that-cannot-seek-spill-to-disk.md) | Reading a blob streams it; a body that cannot seek goes to memory up to a megabyte and to a temporary file beyond | Accepted | 2026-10-30 |
| [0111](0111-the-actions-of-the-workflows-run-on-node-24.md) | The actions of the workflows are on the first major version that runs on Node.js 24 | Accepted | 2026-10-30 |
| [0110](0110-integration-tests-share-a-container-per-service-and-own-their-data.md) | The integration tests share a container per service and own their data; a test that breaks a container owns the container | Accepted | 2026-10-30 |
| [0109](0109-the-ci-jobs-are-a-pipeline-built-once.md) | The CI jobs are a pipeline: the rules and the build first, the tests on what was built, the gate and the summary last | Accepted | 2026-10-30 |
| [0108](0108-the-minio-image-of-the-tests-is-built-from-source-by-chainguard.md) | The MinIO image of the integration tests is the one Chainguard builds | Accepted | 2026-10-30 |
| [0107](0107-the-scripts-of-the-ci-and-the-detector-live-in-one-folder.md) | What the CI and the detector run lives in one folder; the cleaning script is `cleaner` | Accepted | 2026-10-29 |
| [0106](0106-the-ci-reports-the-time-of-every-step-the-test-counts-and-the-coverage.md) | The CI reports the time of every step, the test counts and the coverage, and shows every test | Accepted | 2026-10-29 |
| [0105](0105-shared-package-versions-in-one-file-and-a-common-test-host.md) | The versions of the shared dependencies are in one file that libraries and tests both import, and tests share a small test host | Accepted (where the versions live is superseded by [ADR-0134](0134-package-versions-are-managed-centrally-and-a-library-awaits-with-configureawait-false.md)) | 2026-10-29 |
| [0104](0104-kafka-consumers-back-off-and-state-their-group-timeouts.md) | Kafka consumers back off when they fail and state their group timeouts | Accepted | 2026-10-29 |
| [0103](0103-brokers-are-reached-with-tls-and-sasl-when-the-settings-say-so.md) | The brokers are reached with TLS and SASL when the settings say so, through one place per broker | Accepted | 2026-10-28 |
| [0102](0102-the-inbox-cleanup-is-opt-in-and-needs-no-coordination-between-instances.md) | The inbox cleanup is opt-in and needs no coordination between instances | Accepted | 2026-10-28 |
| [0101](0101-a-message-whose-handler-keeps-timing-out-counts-as-failed.md) | A RabbitMQ message whose handler keeps timing out counts as failed after a limit | Accepted | 2026-10-28 |
| [0100](0100-a-connection-the-client-is-recovering-is-not-replaced.md) | A connection the RabbitMQ client is recovering is never replaced from outside | Accepted | 2026-10-28 |
| [0099](0099-rabbitmq-publishes-with-confirms-and-a-timeout.md) | RabbitMQ publishes with confirms and a timeout; what is worth repeating is one list | Accepted | 2026-10-28 |
| [0098](0098-a-message-another-handling-holds-is-put-back-not-acknowledged.md) | A message another handling holds is put back, never acknowledged | Accepted | 2026-10-28 |
| [0083](0083-four-permission-types-and-constraints-that-must-be-given.md) | Four permission types and constraints that must be given | Accepted | 2026-10-28 |
| [0081](0081-permissions-one-framework-package-and-the-grants-in-the-distributed-cache.md) | Permissions: one framework package and the grants kept in the distributed cache | Accepted | 2026-10-27 |
| [0076](0076-more-providers-and-the-rules-every-provider-shares.md) | More providers (NLog, Azure, AWS, MySQL), and the rules every logging provider shares | Accepted | 2026-10-27 |
| [0075](0075-rules-of-the-framework-project-files.md) | The rules of the project files of the framework (a living list) | Accepted | 2026-10-27 |
| [0074](0074-multitenancy-is-a-package-of-its-own.md) | Multitenancy is a package of its own, and a service that does not want tenants does not get them | Accepted | 2026-10-27 |
| [0073](0073-auditing-and-data-are-packages-of-their-own.md) | Auditing and Data are packages of their own | Accepted | 2026-10-27 |
| [0071](0071-the-aspnetcore-layer-packages-build-on-aspnetcore.md) | The `AspNetCore` layer packages (`WebApi`, `Swashbuckle`) build on `AspNetCore` | Accepted | 2026-10-27 |
| [0070](0070-a-framework-package-brings-what-its-api-exposes.md) | A framework package brings what its API exposes | Accepted | 2026-10-26 |
| [0067](0067-the-event-inbox-is-opt-in-and-a-decorator.md) | The event inbox is opt-in and a decorator of the dispatcher | Accepted | 2026-10-26 |
| [0066](0066-kafka-handles-fetchcount-messages-at-the-same-time.md) | Kafka handles `fetchCount` messages at the same time | Accepted | 2026-10-26 |
| [0065](0065-the-consumers-outlive-their-own-failures.md) | The consumers outlive their own failures | Accepted | 2026-10-26 |
| [0063](0063-kafka-topics-are-named-after-the-exchange.md) | Kafka topics are named after the exchange; the topic prefix setting is gone | Accepted | 2026-10-25 |
| [0062](0062-a-kafka-consumer-for-every-service-and-event.md) | A Kafka consumer, in a group of its own, for every service and event | Accepted | 2026-10-25 |
| [0057](0057-the-caller-of-a-message-and-publish-or-send.md) | The caller of a message is in the envelope; publish an event, send a command | Accepted | 2026-10-25 |
| [0050](0050-a-context-has-a-properties-class-for-its-database.md) | A context has a properties class that says where and on what its data is | Accepted | 2026-10-25 |
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
| [0026](0026-route-pattern-and-jwt-by-default.md) | Routes are `api/{area}/v1/{commercial|public}/{controller}`, and every endpoint needs a JWT unless it says otherwise | Accepted | 2026-10-25 |

## Branching

| ADR | Decision | Status | Date |
|---|---|---|---|
| [0025](0025-the-rewrite-lives-on-feat-new-framework.md) | The rewrite lives on `feat/new-framework` and is not merged into `dev` until the whole framework exists | Superseded by [ADR-0132](0132-claude-works-on-its-own-branch-from-dev-and-never-merges-into-dev.md) | 2026-10-25 |

## Build, tests, CI and the developer workflow

| ADR | Decision | Status | Date |
|---|---|---|---|
| [0007](0007-build-settings-and-versioning.md) | Shared build settings, one version, and how a package is published | Accepted (where the versions of the shared dependencies live is superseded by [ADR-0105](0105-shared-package-versions-in-one-file-and-a-common-test-host.md); the version and the release by [ADR-0115](0115-versions-and-releases-two-branches-and-a-version-made-from-nuget.md)) | 2026-10-22 |
| [0008](0008-code-style-and-analyzers.md) | Code style: the `.editorconfig` is the only source, analyzers run in the build | Accepted | 2026-10-22 |
| [0009](0009-unit-tests-every-package.md) | Every package has a unit test project, found by name | Accepted | 2026-10-22 |
| [0010](0010-integration-tests-with-testcontainers.md) | Integration tests start the real services with Testcontainers | Accepted | 2026-10-22 |
| [0011](0011-coverage-gate.md) | The coverage gate merges unit and integration coverage and fails below 99 % of `src` | Accepted | 2026-10-22 |
| [0012](0012-ci-workflow-and-what-breaks-a-pull-request.md) | The CI workflow, and what breaks a pull request | Accepted (the layout of the jobs is changed by [ADR-0109](0109-the-ci-jobs-are-a-pipeline-built-once.md); when it runs by [ADR-0113](0113-the-ci-runs-on-pull-requests-weekly-on-dev-and-by-hand.md)) | 2026-10-22 |
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

- Bridge the Kafka client's logs to `ILogger` (the Kafka consumer is ported; Azure Service Bus is not planned, [ADR-0133](0133-what-is-not-brought-back-from-the-old-framework-and-the-risks-we-accept.md)).
- Show the `BaseResponse` envelope in the Swagger document ([ADR-0024](0024-swagger.md)).
- A test that fixes the allowed fields per log type, and configurable per-type `RemoveFields`/`AddFields` ([ADR-0019](0019-log-fields-pipeline.md)).
- A `SharedLog` in a measured run (a product's shared library that logs only when Redis is down), so its column of `docs/GraylogFields.md` has data.
- Revoke and rotate the SonarQube token that was once pasted in a conversation; keep tokens in `scripts/sonar/.env` only.
| [0150](0150-a-normalized-field-is-never-empty-is-unique-in-its-set-and-is-checked-before-the-database.md) | A normalized field is never empty, is unique in its set, and is checked before the database | Accepted | 2026-11-05 |
| [0151](0151-the-shared-hosting-of-a-product-is-in-layers-a-core-then-the-kind-of-host-then-what-a-host-chooses.md) | The shared hosting of a product is in layers: a core, then the kind of host, then what a host chooses | Accepted | 2026-11-05 |
| [0152](0152-the-ci-has-no-weekly-run.md) | The CI has no weekly run | Accepted | 2026-11-05 |
| [0153](0153-the-ci-does-not-run-on-the-release-branches.md) | The CI does not run on the release branches | Accepted | 2026-11-05 |
