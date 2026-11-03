# The old framework: what happened to every project

The old HsnSoft framework is on the branch `dev-backup` (46 `src` projects, about 530 `.cs` files, 8 test projects). This folder answers, **project by project**, "it was there, why is it not here, and do we miss it?", so that nobody has to read the old code again unless something extreme happens.

- Read this page for the answer and the open decisions. The five reports next to it hold the evidence per project (what it was, where it is now, why, verdict, gap, what to keep and what to avoid, with `dev-backup:` paths).
- Every judgement is measured against the motto: **independent infrastructure libraries, references cleanly separated, no library forced on a consumer that does not use it, performant and secure** ([Overview](../Overview.md), ADR-0002 to 0006, 0043, 0070, 0075).
- The old code is a source of ideas, not a constraint: nothing here has to stay compatible with it.
- How it was done: five parallel readers, each read every old `.cs` file of its cluster with `git show dev-backup:<path>`, read the new code (not only the names), and grepped the hhs system (`tech-summus/hhs`) and the samples for real usage. The findings were then spot-checked against the repository (the deleting commit, the Swagger text, the Authorization project file, the missing CA2007 rule, the secret in the old script). Items marked "inferred" in the reports have no recorded reason in an ADR or a commit message.

| Report | Old projects |
|---|---|
| [1-foundation](1-foundation.md) | Base, Core (feature group by feature group), Data, Timing, Validation, Security, Json (4), ObjectMapping, AutoMapper, VirtualFileSystem, ExceptionHandling |
| [2-domain-data](2-domain-data.md) | Ddd.Domain, Ddd.Application, EntityFrameworkCore (+ MySQL, PostgreSql, SqlServer, Sqlite), MongoDB, MultiTenancy, Auditing, Authorization (+ Abstractions), Localization |
| [3-messaging-storage](3-messaging-storage.md) | EventBus (+ RabbitMQ, Kafka, Azure), BlobStoring (+ Aws, Azure, FileSystem, Minio), Caching.StackExchangeRedis |
| [4-web-tools](4-web-tools.md) | AspNetCore, Mvc, Mvc.UI, Swashbuckle, Http.Client, UI, UI.Navigation, PuppeTeer, Serilog, the old health checks and hhs shared hosting |
| [5-tests-repo-graph](5-tests-repo-graph.md) | The old tests, repository tooling and CI, the cross-cutting rules, and the project-reference graph of the old framework against the new one |

## Why so many projects are not here

One commit explains almost all of it: `0c82c90e` (2026-10-22), "start the rewrite from Base and Core only", deleted every old project except `HsnSoft.Base` and `HsnSoft.Base.Core`. What is back was **written again** against the motto, package by package, with an ADR for the decisions. So a project that is missing is mostly **not a recorded decision but a project nobody needed to bring back yet**; only VirtualFileSystem (ADR-0038), ObjectMapping and AutoMapper (ADR-0046) are named in an ADR. The reports mark which absences are decisions and which are omissions.

Two facts that change the picture:

- **Several "projects" never had code.** On `dev-backup`, `EntityFrameworkCore.MySQL/PostgreSql/SqlServer/Sqlite`, `BlobStoring` and its `Aws/Azure/FileSystem/Minio` variants, `AutoMapper` and the `Json` meta package were a project file (and Fody files) with no C#. Their new versions are not ports.
- **The old graph broke the motto** (details in report 5): `Core` carried 21 packages (console logging, user secrets, Dynamic LINQ, async helpers, netstandard shims); `EventBus` forced Serilog and Newtonsoft; the `Json` meta package pulled Newtonsoft; `Ddd.Domain` forced AutoMapper; the Redis cache referenced `Ddd.Domain`; `Http.Client` referenced `AspNetCore.Mvc`. A leaf package carried 21 to 48 transitive packages; the new maximum is 10. There were no cycles, but 21 direct references were redundant; the new graph has none and CI enforces it (ADR-0075).

## The nine projects that were asked about

| Old project | Now | Why, and the verdict on the old design | Gap |
|---|---|---|---|
| `Validation` | Gone. `Guard`, `ValidationRule`, `ValidationErrors` in Core, the validation attributes, localized texts in `Localization` (ADR-0044) | Omission turned decision: the old project was a list of localized keys plus `IHasValidationErrors`. Flawed | Covered |
| `VirtualFileSystem` | Gone (ADR-0038); `Localization` reads the embedded JSON itself | Recorded decision. Sound idea, tiny, returned null | Covered |
| `Timing` | Gone; .NET `TimeProvider` | Inferred. Over-coupled: a clock needed Localization, MultiTenancy and Security | None |
| `Swashbuckle` | `AspNetCore.Swashbuckle` and `AspNetCore.SwaggerGateway` | Rewritten. Sound idea, flawed code | Covered. One wrong sentence in `Swagger.md` |
| `Serilog` | `Logging` (abstractions, masking, settings), `Logging.Serilog`, `Logging.NLog` | Rewritten and split by provider. Over-coupled and static-heavy before | Covered |
| `Security` | Split: `ICurrentUser` in Core, claims in `Authentication`, tenant claims in `MultiTenancy.Authentication` | String encryption dropped on purpose: hard-coded passphrase, IV and salt, unauthenticated AES-CBC, zero use in hhs | Document "use Data Protection" (Low) |
| `PuppeTeer` | **Missing** | Omission. Used by exactly one hhs service (scraping in the text normalizer); the PDF renderer had no consumer. The lease, limiter and drain design is good; the defaults are unsafe (`--no-sandbox`, `--ignore-certificate-errors`, runtime Chromium download, a global 30-minute static timeout) | **Medium**: an opt-in, hardened package, or keep it inside the Hsx text normalizer (the maintainer's call) |
| `Json` (+ Abstractions, Newtonsoft, SystemTextJson) | Gone; plain `System.Text.Json` | Superseded. Forced Newtonsoft and JSON Schema; hhs never called `IJsonSerializer` | Document the UTC date rule (Low) |
| `Http.Client` | **Missing** | Omission, no ADR. The old package was a thin and partly harmful wrapper (it forwarded the caller's `Authorization` header to any host); hhs used plain `IHttpClientFactory`. The need is real: Abc and Hsx each hand-write the same `GatewayRequestHandler` and `GatewayClient`, without a timeout setting or resilience | **Medium-High**: a new, small package (below) |

## The whole map (46 old projects)

Verdict key: sound, over-coupled, flawed, superseded. Gap: priority, or None.

| Old project | Now | Verdict | Gap |
|---|---|---|---|
| Base | same | sound | None |
| Core (98 files) | same name, about a tenth left; the rest in Auditing, Data, Logging, Localization, Hosting, AspNetCore, Ddd.\* or dropped | flawed (kitchen sink, ~20 forced packages, static state) | see report 1 |
| Data | same, rewritten | sound idea, flawed code | optimistic concurrency (Low-Medium) |
| Timing, Validation, Json (4), ObjectMapping, AutoMapper, VirtualFileSystem, ExceptionHandling | dropped | see the nine above; ObjectMapping and AutoMapper flawed (reflection, silent catch, empty shell) | None |
| Security | split | see above | Low |
| Ddd.Domain | same, smaller | superseded; aggregate roots, domain events, value objects and `IUnitOfWork` were dead code (publishing was commented out) | optimistic concurrency |
| Ddd.Application | same, DTOs and paging only | superseded | None |
| EntityFrameworkCore | same, rewritten | flawed in parts (ThreadSafe wrappers, licensed bulk library that skipped audit and soft delete, string order-by into Dynamic LINQ) | None |
| EntityFrameworkCore.MySQL / PostgreSql / SqlServer | real code now | old was project file only | None |
| EntityFrameworkCore.Sqlite | dropped, no reason recorded | old was project file only | Low |
| MongoDB | same, rewritten | flawed (a client per context, commands logged at Information) | sessions/transactions: Low |
| MultiTenancy, Auditing | same + satellites (ADR-0074) | sound, over-coupled | None |
| Authorization (+ Abstractions) | redesigned (ADR-0081/0083) | flawed (global snapshot, no tenant in the key, polling thread) | the new package forces `Microsoft.AspNetCore.App` (Low) |
| Localization | same, rewritten (ADR-0038) | flawed (cache key bug) | None |
| EventBus | same, rewritten | over-coupled, static-ish | **outbox: Medium** |
| EventBus.RabbitMQ / Kafka | same, rewritten | flawed (no publish confirms, channel per publish; Kafka committed before handling, a producer per publish) | None; wire changes to check |
| EventBus.Azure | **missing**, never ported | half-working prototype | decision (ADR-0025 blocks the merge) |
| BlobStoring Aws / Azure / Minio | written from scratch | old = skeletons | None |
| BlobStoring.FileSystem | missing, never had code | skeleton | Low |
| Caching.StackExchangeRedis | same, much smaller | cache part sound; the lock was a non-atomic get-then-set, the repository swallowed every exception | Low |
| AspNetCore | split into AspNetCore, WebApi, Hosting, Authentication | flawed (heavy coupling, sync-over-async workers, thread-culture middleware) | Low leftovers |
| AspNetCore.Mvc, Mvc.UI, UI, UI.Navigation | dropped (menu is data in Hsx) | dead code or superseded | None |
| Swashbuckle, Serilog | see above | | |
| Http.Client, PuppeTeer | missing | see above | Medium-High, Medium |
| Old tests, Dockerfile, six workflows, scripts, props | see report 5 | | None |

## What was decided and done (2026-11-03)

The list below was gone through one item at a time with the maintainer; each done item is one commit and, where it is a lasting rule, an ADR.

| Item | Decision | Where |
|---|---|---|
| `Http.Client` | Written: correlation id, retries that do not repeat a write, token only to its hosts, SSRF guard, `BaseResponse` read; the three gateway clients of the samples use it | [ADR-0138](../adr/0138-a-call-to-another-service-has-a-correlation-id-safe-retries-and-a-token-that-goes-only-where-it-is-for.md), [HttpClient](../HttpClient.md) |
| `PuppeTeer` | Written as a framework package: scraping, PDF, screenshot, with the policy and the safe defaults | [ADR-0141](../adr/0141-a-rendered-page-is-an-untrusted-caller-and-the-browser-is-a-pool-with-limits.md), [Browser](../Browser.md) |
| Outbox | **Not built**; the risk is accepted and recorded | [ADR-0133](../adr/0133-what-is-not-brought-back-from-the-old-framework-and-the-risks-we-accept.md) |
| `EventBus.Azure` | **Not planned**; the open item is closed (ADR-0025 was already superseded by ADR-0132, so it blocks nothing) | ADR-0133 |
| Optimistic concurrency | Written: `IHasConcurrencyStamp` in EF Core and MongoDB, a conflict is a 409 | [ADR-0136](../adr/0136-an-entity-is-protected-from-a-lost-update-by-an-opt-in-stamp.md) |
| Wire changes (`UserRoles`, `Producer`) | No mix of old and new services; recorded | ADR-0133 |
| `BlobStoring.FileSystem` | Written: plain names only, atomic streamed writes | [ADR-0140](../adr/0140-the-blob-store-on-the-local-disk-takes-only-plain-names-and-writes-atomically.md) |
| Sqlite, Mongo sessions, dev feed | **Not done**, with the reasons | ADR-0133 |
| Distributed lock | First recorded as not wanted, then **corrected**: what is needed is a limit on how many run at once in every pod, and it is built (a limit of 1 is a lock) | [ADR-0143](../adr/0143-a-limit-on-how-many-run-at-once-holds-across-every-pod.md), [Concurrency](../Concurrency.md) |
| Helpers of the old `Core` | Only what a consumer used came back: text keys in `Core`, `FilterBuilder.Or/AnyOf`, and the package `Extensions` (Base64, collections, `PublicAddress`, `HostPattern`) | [ADR-0137](../adr/0137-the-helpers-of-the-old-core-come-back-only-when-a-consumer-used-them.md) |
| Authorization and ASP.NET | Split: the checker needs no web framework, `Authorization.AspNetCore` has the endpoint layer | [ADR-0135](../adr/0135-permissions-are-asked-without-a-web-framework-and-the-endpoint-layer-is-its-own-package.md) |
| Security headers and CORS | `UseBaseSecurityHeaders()` and `Hosting:Cors` | [ADR-0139](../adr/0139-the-host-writes-security-headers-and-names-the-origins-that-may-call-it.md), [Hosting](../Hosting.md) |
| Kafka message size | `MessageMaxBytes`, `FetchMaxBytes`, `MaxPartitionFetchBytes` | [EventBus](../EventBus.md) |
| CA2007, central package versions | `ConfigureAwait(false)` is an error in `src/`; versions are in `Directory.Packages.props` | [ADR-0134](../adr/0134-package-versions-are-managed-centrally-and-a-library-awaits-with-configureawait-false.md) |
| Benchmarks | `benchmarks/HsnSoft.Base.Benchmarks` | [ADR-0142](../adr/0142-the-hot-paths-are-measured-by-benchmarks-that-are-not-tests.md) |
| The 69 repository tests of the old framework | Read scenario by scenario against the new tests ([2026-11-03 follow-up](#the-69-repository-tests-of-the-old-framework)): the new suite had run on SQLite only (PostgreSQL, MySQL and SQL Server had one scenario each); it now runs on all three real databases and on MongoDB, and it found two defects, both fixed | `RepositoryScenarioTests.cs`, `MongoRepositoryTests.cs` |
| `LogMasker` fallback | Checked: an object that cannot be masked is replaced by a text that says so (never logged raw); a text that is not JSON has no structure to look into and is kept as it is | `LogMasker.cs` |
| `Swagger.md` and the catalog | The wrong dependency sentence and the stale Kafka item are corrected | commit of ADR-0133 |

Still the maintainer's: revoke the old Sonar token that is in the history of `dev-backup`.

## What the new framework is missing or should check, in order

**Medium-High**

1. **`HsnSoft.Base.Http.Client`.** Shape (from report 4): references Core and `Microsoft.Extensions.Http.Resilience` only; correlation from the `LogFlow` in Core; the bearer-token relay is a separate opt-in piece that may only send to hosts in an `AllowedHosts` list (the old package forwarded the token anywhere); failures become a typed `BaseHttpException`; an optional guard against server-side request forgery, because hhs's remote file downloader and live client test fetch user-supplied URLs unguarded. It would replace three copies in the samples. Open: decide first how the in-call resilience fits the "work item with retry" idea of the Hsx analysis.

**Medium**

2. **Outbox for "save, then publish".** The new tree has an inbox (ADR-0067) and no outbox. hhs's own code calls the loss a known gap; `samples/hsx/docs/hhs-analysis.md` says "not needed". The two disagree; decide before Hsx writes its first chain. Shape: an opt-in decorator like the inbox, with EF Core and MongoDB providers.
3. **PuppeTeer**: framework package versus a class in the Hsx text normalizer; scraping only, or PDF and screenshot too. Any package must change the defaults listed above.
5. **`EventBus.Azure`**: implement it (native dead letters, topic as the exchange), or supersede ADR-0023 and ADR-0025, which make it a condition for merging `feat/new-framework` into `dev`.

**Low-Medium**

6. **Optimistic concurrency.** `IHasConcurrencyStamp` and `BaseDbConcurrencyException` have no replacement in `Ddd.Domain`, EF or Mongo. hhs never used them; Hsx has concurrent writers (transfers, subscriptions). An opt-in stamp in the interceptor, or document EF's own concurrency token.

**Low**: `BlobStoring.FileSystem` (path-traversal rules would be the contract); a security-headers middleware; a settings-driven CORS policy (hhs allowed any origin with credentials, a real hole; Edx shows the right shape); `StringHelper.ReplaceInvalidChars` (hhs still uses it); an `Or` for `FilterBuilder`; the `Calculator` leftover in Core; splitting the Authorization contracts from the `Microsoft.AspNetCore.App` reference; Sqlite; Mongo sessions; a distributed lock (compare-and-set rows may suffice); DataProtection on Redis (document); the CA2007 analyzer rule (ConfigureAwait is a convention now; the old Fody weaver enforced it); benchmarks (none exist); central package versions; a dev feed for packages.

## Defects found in the current repository

- `docs/Swagger.md` says `AspNetCore.Swashbuckle` depends only on Core; its project file and ADR-0071 reference `AspNetCore`.
- `docs/adr/INDEX.md` still lists "port the Kafka consumer" as open; Kafka is ported (ADR-0062 to 0066).
- The `UserRoles` header changed on the wire from a comma-joined string to an array, and `Producer` from `Exchange_Client` to the client name. Neither was tested for a rolling mix of old and new services. Decide whether such a mix can still exist; if so add a tolerant reader.
- Not audited: whether the new `LogMasker` still returns the unmasked value when masking fails, as the old one did.

## Security

The old script `analysis_sonar.ps1` has a **hard-coded Sonar token, twice, in the history of `dev-backup`**. It is not copied here. Revoke it, and decide whether `dev-backup` should be rewritten or kept private.

## Decisions for the maintainer

The reports end with their own lists (about 35 questions). The ones that decide the next work: build `Http.Client` and with what limits; outbox yes or no; where `PuppeTeer` lives; `EventBus.Azure` or superseding ADR-0025; optimistic concurrency; whether a mixed rollout of old and new services exists.

## The 69 repository tests of the old framework

The old `test/HsnSoft.Base.Test.Unit` held 35 scenarios for each of the EF Core and the MongoDB repository (all `[Trait("category", "integration")]`, so the old CI never ran them; they ran on PostgreSQL and MongoDB). Read one by one against the new tests, with the new contract in mind (`FindAsync` returns `null` where `GetByIdAsync` threw, `ListQueryOptions` and `PagedQueryOptions` replace the order-by lambdas, no `GetSingle`):

| Old scenario | New |
|---|---|
| Get by id, found and not found | `FindAsync(id)`: the entity, or `null` (by design, ADR-0045) |
| Get single / single or default / first or default, found, not found, **duplicates** | `FindAsync(predicate)` takes one and never throws on duplicates; the first of an order is a sorted list with `MaxResultCount = 1`. **Not tested before; now tested** (every database) |
| Get list, get page list, **empty and not empty** | Not empty was tested on SQLite only; **empty store, filter, sort, page two and the total are now tested on PostgreSQL, MySQL, SQL Server and MongoDB** |
| Count and exists, empty and not empty | The same: now on every real store, also with a filter put together by `FilterBuilder` (`And`, `Or`, `AnyOf`) |
| Insert, insert many | `AddAsync`, `AddManyAsync`: on every real store |
| Update by id / update / update many, found | `UpdateAsync` (tracked and detached), `UpdateManyAsync`: on every real store |
| **Update, delete, update many and delete many of an entity that is not found: throws** | **A 409 in every store.** EF Core always did it (through the concurrency exception); **MongoDB silently did nothing, and now answers 409 too** (defect 1) |
| Delete by id, delete by id list | `DeleteAsync`, `DeleteManyAsync`, `DeleteWhereAsync` (which says how many it marked): on every real store |
| **A whole entity read back equals the one saved, to the millisecond** (`ShouldBeEquivalentToWithMilliseconds`) | Was not tested on a real store. **Now tested on all four, and MySQL failed it: the audit times lost everything after the second** (defect 2, fixed by keeping every time as a `datetime(6)` on MySQL) |

Two scenarios of the old suite have no equivalent on purpose: `GetSingle` (an entity that is more than one is the caller's problem, not an exception of the repository) and the exceptions of a missing entity on a read (`null` is the answer).
