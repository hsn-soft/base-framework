# hhs, what it does, and what Hsx does about it

Written 2026-11-01 from a read-only study of `tech-summus/hhs` (identity, administration, content, text-normalizer, video-generator, the scheduler, the commercial gateway, the commercial web, `shared/`) by four reviewers, each reading the code. Facts here are what they reported with file
paths; where they could not tell, it says so. The framework column was checked against `docs/` and the ADR index of this repository. Secrets in hhs (passwords, client secrets, API keys, a committed private key) are named, never copied.

The rule for Hsx is **not** to port hhs: the business stays, the technical shape is chosen again on the new framework, and what hhs got wrong is not carried over.

## 1. What hhs is

Seven services on **RabbitMQ** (one direct exchange `HHS`, one vhost `/`, queue `HHS_<Service>_<Event>`), each with its own database: PostgreSQL (identity, administration, content, feedr), MongoDB (text-normalizer, video-generator, event-manager, feedr). The system makes a short **video of a
page of a customer's website** and, once a day, a video of the day's most read pages.

| Part | What it does |
|---|---|
| **identity** | Tenant tree (System / Reseller / Account, `NormalizedAccessPath`), Company, Customer (a website domain), Subscription (reseller + company + customer + product), users, roles, role-subscriptions (the source of `allowed_scope_key`), refresh tokens, login audit, password policy. `tenant-management`, `my/subscriptions`. |
| **auth-server** | Issues the RS256 JWT (`password` and `client_credentials`), refresh token in an HttpOnly cookie, lockout, login audit. Reads **identity's database** through the NuGet package of identity. Clients are hard-coded. |
| **administration** | Permissions, role-permissions, the menu tree, session permissions; writes the grants to Redis and tells every service (`CachePermissionGrantsChangedEto`). |
| **content** | `CustomerContent` (a page), `AnalysisContent` (+ items), `CustomerContentVisit`, `CustomerVpSetting` (per-customer config), `ContentVideoGenerationLimit` (quota ledger). The **public** endpoints `contents/get-or-create` and `create-ad-result`. Owns the approve rules and the daily jobs. |
| **text-normalizer** | Scrapes the page (Puppeteer, a scraper per site), makes the script ("outline") with an LLM provider, for one page or for the items of an analysis. |
| **video-generator** | Audio (TTS) per item, then the video (Creatomate or internal queue providers), downloads and uploads to a CDN, publishes the result. Also "video from a link". |
| **scheduler** | Quartz in one pod, **14 cron jobs that call `JobsController` endpoints** with a static `SchedulerApiKey`: most are polling or retry loops every 5 or 10 seconds. |
| **commercial gateway** | YARP, three routes per service, `/<svc>-service/v1/...` to `/api/<svc>-service/v1/commercial/...`. Forwards everything; the services check the token. |
| **commercial web** | **Not a dashboard**: a Razor host of the static JS the customers embed (the SDK, the player, a loader file per customer) and test pages. The real signed-in UI is the Angular **back-office**, which was not studied. |
| **event-manager, feedr-admanager** | Dead-letter store with manual requeue; Google Ad Manager revenue and reports. Not studied here. |

### The pipeline (one page to one video)

1. A browser on the customer's site calls `POST contents/get-or-create` (anonymous). Content checks the Origin against the domain of `CustomerVpSetting`, the path filters, creates `CustomerContent`, writes a visit row and publishes `CustomerContentCreatedEto`.
2. text-normalizer creates a request, scrapes, (skips if the page is older than 3 days), calls the outline provider (immediate or polled) and publishes `NormalizerResultPublishedEto`.
3. content applies the approve rules (release time is today, start hour, daily quota) and publishes `VideoGenerationApprovedEto`, or marks `REJECTED`.
4. text-normalizer forwards the Base64 audio items (`VideoGenerationDataForwardedEto`); video-generator creates `VideoRequest`, one `AudioRequest` per item, calls the audio provider, uploads the audio, then (a fan-in on a tick) calls the video provider, polls, downloads, uploads to the CDN and publishes
   `VideoGenerationResultPublishedEto`; content stores `VideoCdnUrl`.
5. **Analysis video**: the scheduler asks content (per customer) to pick today's most visited normalized pages, an analysis with items is made and goes the same way, with an intro and an outro sentence and a snapshot of the sources.

About **25 Etos** carry this, most of them a service talking to itself (`...StartedEto`, `...CompletedEto`), each with an inbox row, a compare-and-set on the previous status and a retry that re-publishes the Eto of the failed milestone string.

## 2. The technical inventory

| Topic | hhs |
|---|---|
| **Databases** | One per service, created and seeded by the service itself (`MigrateAsync` + seeders). Only the identity database is shared (with the auth server). No schema, no table prefix. Seeds hold **real customers and domains**, fixed GUIDs shared between services (`IdMap`), a hard-coded default password. |
| **Redis** | (1) the permission grants (`hhs-permission-store`, one JSON blob of all grants, written by administration, read by every service on a 300 s timer); (2) a **job concurrency counter** (`req_limit:<endpoint>`, TTL 120 s) behind `[EndpointConcurrencyLimit]`; (3) a lock service nobody uses; (4) auth-server refresh tokens, all commented out. No business cache. |
| **Broker** | RabbitMQ only. Event name = type name without `Eto`. Envelope: hop level, requeue count, message and parent ids, correlation id, user id and roles, client info, producer. **No tenant and no scope key in the envelope** (`ScopeKey` is a field of some Etos). |
| **Handlers** | `EventDispatcher` makes a scope per handler and **turns the tenant and scope filters off for every handler**; `ApplicationEventHandlerBase` is the inbox (table `EventInboxMessages`, STARTED/COMPLETED/FAILED, a lease, a stale reset by a worker); on an exception the framework sends a `FailedEto` to event-manager (stored in Mongo, requeued by hand with `ReQueuedEto`). Per-event prefetch (`fetchCount`). |
| **Logging** | Serilog; Console + Graylog (HTTP) + a file fallback; four "persistent loggers" gated by a level; masking by `[SensitiveData]` and by key names; `X-Correlation-Id` made at the edges (gateway, auth server, web) and carried in the envelope; request/response logging at the edges only. |
| **Tenant and scope** | `IMultiTenant` and `IScopeSubscription` global filters (`IsSystemTenant || allowed.Contains(...)`), toggled by `IDataFilter`. `ScopeKey = {customerId:N}:{product prompt}`; set on creation by the services, put into the token from the subscriptions of the user's roles. `allowed_tenant_id` = every tenant whose path starts with the user's. Most services have **only the scope filter**. |
| **Authentication** | RS256, public key file in every service, one audience per service (`audience-service-x`), 2 h access token, refresh token cookie (7 d, rotated, hash in the database), issuer validated only in stage/uat/production. |
| **Authorization** | `[PermissionAuthorize]` over a Redis-synced `IPermissionStore`, permissions `service.*`, `operation.*`, `constraint.*`. **In practice thin**: only identity uses it (8 attributes); content's are commented out; administration's `[Authorize(permission)]` probably points at a policy that does not exist. Two permission models (`PermissionGrant` and `AppRolePermission`) that are not connected: what the UI writes may never reach Redis. |
| **Hosting** | The same 100-line `Program.cs` in every service over `Hhs.Shared.Hosting` (settings, Serilog, thread pool, Kestrel, JWT, permissions, event bus, health, Swagger, response wrapper `BaseResponse`, exception mapping, localization en/tr/ru). |
| **Environments** | development, docker, stage, uat, production; `appsettings.<env>.json` committed **with the secrets in them**; k8s manifests per repo plus a gitops repo (kustomize + ArgoCD); uat and production share one RabbitMQ and one exchange. |

## 3. The framework against it

"Have" means there is a package or a documented behaviour; the verdict says what Hsx does.

| hhs needs | New framework | Verdict |
|---|---|---|
| Host start, limits, proxies, thread pool, health, Swagger, response wrapper, exception mapping, localization | `Hosting`, `AspNetCore.WebApi`, `Localization`, health endpoints (ADR-0117), ADR-0120, 0121 | **Have.** The 100-line `Program.cs` is the five-line composition of the other samples. |
| Serilog, Graylog, masking, correlation id, request log | `Logging.Serilog`, `[SensitiveData]`, `RequestLoggingOptions`, `Logging.md`, `GraylogFields.md` | **Have.** |
| Tenant tree, allowed tenants in the token, scope key, two filters, system tenant | `MultiTenancy*`, `TenantAccessPath`, `IScopeSubscription`, ADR-0128, 0129 | **Have**, and exclusive by entity (ADR-0129). Handlers: the tenant travels in headers; a handler of a scope entity enters the scope of the event's `ScopeKey` with `ICurrentTenant.Change(...)` (hhs turned the filters off instead). |
| JWT, audiences, issuing | `Authentication` (ADR-0122); the Hsx auth server | **Have**; refresh token, lockout, login audit, client credentials are **product features of the auth server** to build in Hsx. |
| Permissions | `Authorization`: page, endpoint, operation, constraint; `IPermissionStore` over `IDistributedCache`, one entry per subject, `CacheSeconds` | **Have, and better**: no sync worker, no sync event, no blob. Administration writes with `SetAsync`; the menu is data of Hsx. |
| RabbitMQ events, failed events, requeue, per-event concurrency | `EventBus.RabbitMQ`/`Kafka`, `FailedEto`/`ReQueuedEto`, `FetchCount`, `MaxConcurrentHandlers` | **Have.** The event-manager service (the store of failed events, the requeue by hand) is a **product service** to build. |
| Inbox (idempotency) | `EventBus.EntityFrameworkCore`/`MongoDB`, opt-in decorator (ADR-0067), cleanup (0102) | **Have**, as a decorator instead of a handler base class. The lease and stale-reset of hhs are replaced by the redelivery semantic of the broker; check it with a test before relying on it. |
| Outbox | none | **Not needed**: hhs has none either. |
| Seeds at start | `IDataSeeder` (ADR-0125), schema steps (ADR-0118) | **Have.** |
| Scheduler calling endpoints with a key | `[SchedulerAuth]` (ADR-0126), `Scheduler.md`, `Abc.Scheduler.Host` | **Have.** |
| Blob storage and CDN | `BlobStoring` (Aws, Azure, Minio) | **Have** for storage. Bunny pull zone and Bunny storage are **providers of the product** (an HTTP PUT). |
| **Polling and retry of a work item that waits on an external system** (status, `NextAttemptAt`, attempt count, claim by compare-and-set, backoff, classify retryable) | **none** (`retry worker` is the open item) | **GAP. The one real one.** hhs does it with a scheduler that curls an endpoint every 5 s, a Redis counter and a milestone string. See section 5. |
| Retry classification (`ExceptionClassifier`, `RetrySettingsBase`, `RetryDelayCalculator`: 18, 15 and 9 references in hhs) | none | Goes with the gap above. |
| Distributed lock, endpoint concurrency limit | none | The limit exists only to stop two pods running the same job; with claims by compare-and-set, or an in-process worker that claims rows, **it is not needed**. A lock stays a question for the daily jobs. |
| Rate limiting at a gateway, a public gateway | `Abc.Gateway.Public.Host` is a sample; no rate limiter in the framework | **Gap in the gateway composition** (hhs has the settings and the code commented out). Hsx gets a public gateway with rate limiting and a per-customer key. |
| `PasswordHasher` | none (copied into `Edx` and `Hsx` identity) | **Move into the framework** (`Authentication`): three copies is one too many; hhs has it in `shared` with 8 references. |
| Scraping (Puppeteer, a scraper per site by a `switch` on the domain) | none (the old framework had a `PuppeTeer` package) | **Product.** In Hsx the per-site rules (five selectors) are **data of the customer settings**, not a class and a deploy per customer. |
| Providers (outline, audio, video, CDN) with capabilities (`ImmediateResult` / `AsyncPolling`, audio-input mode, URL / stream result), chosen by a key per customer | none | **Product**, kept as a pattern inside the services that use it, not in `shared`. |

**Answer to "can our services do what the hhs services do?"** Yes for everything cross-cutting except the polling and retry of waiting work (gap), the public gateway hardening (a composition to write) and the `PasswordHasher` (to move). Everything else is business that Hsx writes.

## 4. What stays in shared, what goes to the framework, what is dropped

**To the framework** (nothing of the product in it, used by every service): `PasswordHasher`; the retry policy and exception classification together with the work-item building block of section 5; (later, if a second product needs it) a `ScopeKey` format helper is **not** a framework matter,
only the interface is.

**Stays in `Hsx.Shared.*`** (two or more Hsx services use it): the integration events that cross a service (`Hsx.Shared.Contracts`), `ProductTypes` (with the duplicate values removed), `ContentType`, `ScopeKeyHelper`, `HsxTenants` and the demo ids, the permission constants of each service **only if the
client needs them**, `Hsx.Shared.Hosting.*` (the thin composition).

**Moves into the one service that uses it** (hhs put it in `shared` because it had nowhere else): provider keys, provider settings and capabilities (`ProviderKeys` 23 references, in two services), the Puppeteer wrapper (text-normalizer only), retry settings of one service.

**Not carried** (dead or wrong in hhs, with the evidence the reviewers gave):

- `MailSender`, `MailKit`, `MimeKit`, `PasswordResetLinkGenerator` (0 references), `Signer` (0), `EnumDisplayCache`, `ProviderModels`, `PollingSettingsBase`, `HashServiceProviderEnum`, `RedisLockService` (registered, never used), the Razor render service, `ICustomerSubscription`, `BackgroundServiceFlags`, a half-built **constraint pipeline** with no
  consumer.
- Auth: the unused `RedisService`, `AuthTokenRevocation`, `AuthEmailConfirmationToken`, a password policy that is stored and never read, the commented security-stamp check; administration's `old/` folder, `OldMenuMap`, `OldMenuPermissionMap`, the seeded `AppMenu` tables nobody queries (the menu is a 328-line static class).
- Content: the dashboard job and `ResponseStatistic` (the job calls an endpoint that does not exist), trend video generation ("not implemented"), `create-ad-result` (validates and writes nothing), `TestQuery*`, the **anonymous** `tests/analysis-contents` write endpoint, the `CustomerContentOperationStates` enum and the DTO fields that no longer exist on the entity.
- video-generator: `VideoRequestStates`, `VideoFormatTypes`, `ClientColossyanAiSettings`, the per-customer bucket fields.
- Bugs to **fix, not copy**: `ProductTypes` has three members equal to 12; `get-or-create` never returns the customer's own finished video (a status mapping that reads fields that are gone); two permission models that are not connected; the same default password in three places; `IdentityModelEventSource.ShowPII = true`;
  a committed private key and committed passwords; a failed analysis that never tells content; `[Authorize(permission)]` on a policy name that does not exist; role update and delete without a tenant check; `IsStatic` not enforced.

## 5. What Hsx does differently (to decide, then an ADR each)

1. **One work item per step, not 25 events.** The intra-service steps (`...StartedEto`, `...CompletedEto`) become a **durable work item** in the service's own database (status, current step, attempt count, `NextAttemptAt`, claimed by compare-and-set) handled by an in-process worker; the broker carries only what
   **crosses a service** (about ten events). Polling, retry and the fan-ins are the same worker, not a scheduler calling an endpoint every five seconds. The scheduler stays for what is a time of day (the daily analysis, the hourly trend). This is the one building block the framework lacks; I propose it
   as a framework package after one service has used the idea (text-normalizer first), not before.
2. **One state machine per work item** (hhs has `Status`, `ScrapingStatus` and `OutlineStatus` for one record, plus content's mirror of the strings of another service). Content keeps **its own** states only (not started, normalizing, approved, rejected, generating, completed, failed) and a few progress events.
3. **The settings of a customer have one owner** (content's `CustomerVpSetting`; hhs keeps a copy in three services, synchronized by hand). The others read a snapshot that content publishes when it changes. Or the provider choice lives in the subscription's `SettingsJson`, which identity has already.
4. **A public gateway** (the `get-or-create` of hhs sits in the commercial controller with a TODO): anonymous routes only, **rate limited**, with a **public key per customer** and the allowed origins in the customer's settings (the `Origin` header alone is spoofable), and the visit written without a database insert per page view.
5. **Permissions**: one model, the framework's; endpoints are guarded, not commented out; the menu is data.
6. **The scraper rules are data**, **providers are resolved by key** with their capabilities, as in hhs (a good idea to keep), inside the service.
7. **Errors reach content**: a failed analysis publishes a failure like any other step.
8. **Secrets** only from the environment (`deploy/.env`, ADR of the samples), the demo data only in samples, never real customers.

## 6. The business to carry

Domain check against the customer's registered domain; include and exclude path filters (anchored at the first segment); one content per (customer, normalized path); the response states (CREATED, PROCESSING, READY, NO_ANALYSIS_VIDEO, SKIPPED_PATH, FAILED); the direct-video approve rules (release
time is today, the start hour **as a time, not an hour of the day**, the daily quota per type from a ledger); the analysis video (a minimum of visits and of stories, today's most visited, an intro and an outro sentence, a snapshot of the sources, the sources shown only when the customer enables it);
per-customer provider selection; the tenant tree, the subscriptions and the scope keys of identity; the role-subscription link; the tenant-management rules (a reseller creates its companies, customers and subscriptions; a company has an account tenant).

## 7. Order of work

0. **Decisions** (ADRs of the samples): the broker and a vhost for each environment (as Abc), the work item (1 above), the public gateway (4), the owner of the customer settings (3), the scope of "all of hhs" (which of event-manager, feedr, back-office, mobile).
1. **Contracts and the broker**: `Hsx.Shared.Contracts` with the events that cross a service, `ProductTypes` fixed; RabbitMQ in the four environments.
2. **Identity and the auth server complete**: tenant management, my subscriptions, refresh token, lockout, login audit, client credentials, a password change.
3. **Administration** on the framework's permissions.
4. **Content** in full, with the public gateway.
5. **Text-normalizer**, then **video-generator** (the work item, the providers, the scraper rules as data).
6. **Scheduler** for the daily jobs; **event-manager**; the web.
7. After each step: the smoke test of the four environments, and the report of what the new framework could and could not do.
