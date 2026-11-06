# Cluster B: domain, persistence, tenancy, security model

Evidence base: `git show dev-backup:<path>` for the old code, the working tree of `feat/new-framework` for the new code, `/Users/hasansahin/ws/org/tech-summus/hhs` for real usage. ADR numbers are the ones in `docs/adr/INDEX.md`.

## Summary

| Old project | Now | Verdict | Gap / priority |
|---|---|---|---|
| Ddd.Domain | (a) same name, rebuilt smaller (`Entity`, 3 audited bases, `IRepository`, sorting whitelist) | superseded by something better | AggregateRoot / domain events / ValueObject / ConcurrencyStamp / `IUnitOfWork` dropped; optimistic concurrency is the only thing worth a decision: Low-Medium |
| Ddd.Application | (a) same name, DTOs + paging only | superseded (sound DTO core, rest was unused) | none; `ICrudAppService` family kept dropped: None |
| EntityFrameworkCore | (a) same name, rewritten (interceptor instead of tracker events, no ThreadSafe wrappers, no BulkExtensions) | flawed in parts (ThreadSafe*, Bulk licence, Dynamic LINQ sort); rest superseded | none; `IUnitOfWork` replaced by `ExecuteInTransactionAsync`: None |
| EntityFrameworkCore.MySQL | (a) renamed `EntityFrameworkCore.MySql`, now real code | old was csproj only | covered: None |
| EntityFrameworkCore.PostgreSql | (a) same name, now real code | old was csproj only | covered: None |
| EntityFrameworkCore.SqlServer | (a) same name, now real code | old was csproj only | covered: None |
| EntityFrameworkCore.Sqlite | (c) dropped, nothing recorded | old was csproj only (never had code) | Low: only if a test/embedded host needs it |
| MongoDB | (a) same name, rewritten (no context class, no tracking collection, no Old folder) | sound idea, flawed implementation; superseded | session/transaction API not ported: Low |
| MultiTenancy | (a) same name + 4 satellite packages (ADR-0074) | sound but over-coupled; superseded | covered: None |
| Auditing | (a) same name, redone as `Mark...` methods and no reflection | sound but over-coupled; superseded | covered: None |
| Authorization | (a) same name, redesigned (distributed-cache store, catalogue from code, 4 types) | flawed (global static snapshot, no scope); superseded | one motto note: the new package forces `Microsoft.AspNetCore.App`: Low |
| Authorization.Abstractions | (b) merged into `Authorization` already on dev-backup (commit 513d8411), same in the new tree | sound, but a needless project | see Authorization: Low |
| Localization | (a) same name, rewritten (ADR-0038) | flawed (cache key bug, VirtualFileSystem dependency); superseded | covered: None |

No HIGH gap in this cluster. The two open items with real weight are (1) optimistic concurrency for edited records, (2) transactional publish of an event with the data change (outbox; belongs to the EventBus cluster, noted here because the old domain-event code was the place that tried and never finished it).

Facts that matter for "what really contained code": on dev-backup `EntityFrameworkCore.MySQL`, `.PostgreSql`, `.SqlServer`, `.Sqlite` are 3 files each (csproj + `FodyWeavers.xml/.xsd`): no `.cs` at all. hhs used `HsnSoft.Base.EntityFrameworkCore.PostgreSql` only as a package reference (the `UseNpgsql` calls are in hhs itself, 4 + 4 places). `MultiTenancy` on dev-backup had 6 `.cs` files, and no satellite projects (`MultiTenancy.Authentication/.EventBus/.EntityFrameworkCore/.MongoDB` did not exist).

---

## HsnSoft.Base.Ddd.Domain

**What it was.** 32 files (`dev-backup:src/HsnSoft.Base.Ddd.Domain/...`).
- Entities: `Entity<TKey>` (Id with protected setter, no equality), `BasicAggregateRoot` (list of `DomainEventRecord`, `AddDomainEvent`, `ClearDomainEvents`), `AggregateRoot` (+ `ConcurrencyStamp` from `Guid.CreateVersion7`), `AuditedEntity/CreationAuditedEntity/FullAuditedEntity`, `EntityHelper.TrySetId` (reflection), `EventOrderGenerator` (static `long` counter with `Interlocked`), `EntityEventReport/DomainEventEntry`, `DisableIdGenerationAttribute`, `ValueObject`.
- Repositories: `IGenericRepository<TEntity,TKey>` (Insert/InsertMany, UpdateById(action), Update/UpdateMany, DeleteById/DeleteByIdList/Delete/DeleteMany(predicate|entities)) and `GenericRepositoryBase` (about 300 lines: GetById/GetSingle/GetFirst/List/Page/Count/Exists with `includeEntity` lambdas, every read in two flavours: `selector` and AutoMapper `IConfigurationProvider`; and `ApplyDataFilters` doing soft delete, tenant and `IScopeSubscription` filters by `IQueryable` rewriting).
- `Models/*QueryOptions` (filter, include, `OrderByDynamic` string + `OrderByEntity` func, page number/size).
- `DomainService` (service-locator base: `IClock`, `IDataFilter`, `ICurrentUser`, `ICurrentTenant`, `IStringLocalizerFactory`, `ILoggerFactory` pulled with `provider?.GetService`, plus `IScopedDependency` marker), `IUnitOfWork` (SaveChangesAsync, ExecuteAsync).
- References: AutoMapper, Data, EventBus, ExceptionHandling, Timing, MultiTenancy (project references; 6 siblings). Motto: poor. A domain project pulled a mapper, the event bus, tenants and the clock; the repository contract knew `IConfigurationProvider` (AutoMapper) and `IQueryable`, so every store leaked.

**Where it is now.** (a) `src/HsnSoft.Base.Ddd.Domain`, references only Auditing + Data (+ DI abstractions). Files: `Entities/Entity.cs, CreationAuditedEntity, AuditedEntity, FullAuditedEntity, IEntity`, `Repositories/IRepository.cs` (+ `IReadOnlyRepository`, `QueryOptions`, `EntityProjection`), `Sorting/SortFields, SortSpec, SortClause`, `EntityConceptsServiceCollectionExtensions` (`AddBaseEntityConcepts`). Moved elsewhere: `FilterBuilder` and `Guard` to Core; `DomainService` has no successor (hhs had 0 users); the tenant/scope filters moved to `MultiTenancy.EntityFrameworkCore`/`.MongoDB`.
Dropped: `AggregateRoot`, `BasicAggregateRoot`, `DomainEventRecord`, `EventOrderGenerator`, `EntityEventReport`, `ValueObject`, `ConcurrencyStamp`, `IUnitOfWork`, `DisableIdGenerationAttribute`, `EntityHelper`.

**Why.** ADR-0042 (taken back "smaller and with the faults left out"), ADR-0045 (repository levels: "no `IQueryable`, no `Include`, no context or session" in the common contract; the old split "let DbSet, collections and sessions leak"), ADR-0046 (no mapper: DTO projection from the DTO), ADR-0037 (entity marks instead of reflection). The unused pieces are inferred drops: hhs has 0 references to `AggregateRoot`, `ValueObject`, `AddDomainEvent`, `ConcurrencyStamp`; `IUnitOfWork` is only registered (3 `AddScoped` lines) and never injected anywhere in hhs.

**Verdict.** Superseded by something better. The domain-events machinery was dead in the old code too: `PublishEntityEvents` in `BaseEfCoreDbContext` is entirely commented out, events were collected and cleared and never delivered. `EventOrderGenerator` is process-global static state.

**Gap.**
- Domain events / aggregate roots: keep dropped. Events leave a service through the EventBus after the save; a domain-event dispatcher that nothing consumed is dead weight. (Related, not this cluster: the dual write "save then publish" has no outbox in the new tree; ADR-0067 only has the inbox. Medium for the EventBus cluster; hhs publishes after save with the same risk.)
- `ValueObject`: keep dropped (C# `record` gives value equality; hhs 0 users). None.
- Optimistic concurrency (`IHasConcurrencyStamp`): not present in the new tree, hhs never used it. A back-office where two operators edit the same record would want it. Recommendation: document only (EF `[ConcurrencyCheck]`/`xmin`, Mongo `UpdateWhereAsync` with a version in the filter) or, if wanted, fold a small opt-in `IHasConcurrencyStamp` into `Ddd.Domain` + the EF interceptor. Priority Low-Medium, maintainer decision.
- `IUnitOfWork`: covered by `DbContextExtensions.ExecuteInTransactionAsync` (EfCore; ADR-0119: it runs through the execution strategy, which the old `UnitOfWork` also did via `CreateExecutionStrategy`). None.

**Keep & avoid.**
- Keep: the page numbering idea `PageNumber` from 1 / `MaxResultCount` (already kept), `Guid.CreateVersion7` as the id and stamp (kept in the docs sample). `ValueObject.ValueEquals` is a hand-rolled enumerator; do not port it.
- Avoid: service-locator constructors (`IServiceProvider provider = null` then `GetService`), nullable "optional" dependencies that silently turn a filter off when DI is missing (`DataFilter?.IsEnabled<...>() ?? false`: a missing `IDataFilter` meant **no tenant filter**, a security fail-open), `GetQueryable()` on the interface, `Equals(e.Id, id)` boxing in expression trees, static mutable counters.

---

## HsnSoft.Base.Ddd.Application

**What it was.** 30 files. DTOs: `EntityDto`, `AuditedEntityDto`, `FullAuditedEntityDto`, `ListDataResultDto`, `PagedDataResultDto` (total, page count, start/end number; `PageTotalCount`, `ResultDataStartNumber`, `ResultDataEndNumber` are get-only so they do not deserialize), `LimitedDataRequestDto` -> `SortedAnd...` -> `SearchDataRequestDto` -> `PagedDataRequestDto` with `static` mutable `DefaultMaxResultCount = 10` and `MaxMaxResultCount = 100`. Services: `ApplicationService` (service-locator base like `DomainService`), `IApplicationService`, and the ABP-style `ICrudAppService` / `IReadOnlyAppService` / `ICreate...` / `IUpdate...` / `IDelete...` interface families. `BaseDddResources` (localization marker).
References (7): Auditing, Authorization, Ddd.Domain, Localization, ObjectMapping, Security, Validation. Motto: poor, an application-layer DTO package that forced the whole authorization and localization stack and a mapper.

**Where it is now.** (a) `src/HsnSoft.Base.Ddd.Application` references only Ddd.Domain (+ options/DI abstractions): `Dtos/EntityDtos.cs, ListRequests.cs, ListResults.cs`, `QueryOptionsExtensions.cs` (`ToPagedOptions`, `ToListOptions`, `ToPagedDto`). `ApplicationService` moved to Core (ADR-0041, logger + current user only).

**Why.** ADR-0042 (the paging model is "hhs's"; the maximum is the `Paging:MaxResultCountLimit` setting, "not a static field"), ADR-0041. The CRUD interface family is an inferred drop: hhs has 0 references to `ICrudAppService`, `IReadOnlyAppService`, `ICreateAppService`; hhs uses `PagedDataResultDto` (25 files) and `PagedDataRequestDto` (8 files).

**Verdict.** Superseded: the DTO core was sound and is kept; the CRUD service interfaces were unused ABP imitation; the static mutable limit was a global that any code could change.

**Gap.** None. Keep dropped (CRUD interfaces force a method shape and, with `IApplicationService`, DI conventions). `AuditedEntityDto` exists in new.

**Keep & avoid.** Keep: the paged-result arithmetic (`dataTotalCount <= skip` returns empty counters; page/size clamped to 1). Avoid: static mutable limits, get-only computed properties on a response DTO that a client SDK must deserialize, validation messages as bare keys (`"MaxResultCountExceededExceptionMessage"`).

---

## HsnSoft.Base.EntityFrameworkCore

**What it was.** 17 files.
- `BaseEfCoreDbContext<T>`: sets audit/tenant/id/concurrency stamp on `ChangeTracker.Tracked` and `StateChanged` events; soft delete by `entry.Reload()` + setting `IsDeleted` by reflection (`ObjectHelper.TrySetProperty`); global filters (soft delete, tenant, scope) built with expression trees in a context that holds `ICurrentTenant`/`IDataFilter` taken from `IServiceProvider`; `OnModelCreating` calls generic methods through `MakeGenericMethod(...).Invoke` for every entity; domain event report (published nowhere); `DbUpdateConcurrencyException` wrapped in `BaseException`; command timeout fixed at 30 s.
- `Modeling/EntityTypeBuilderExtensions` (`ConfigureByConvention`, with large commented ABP leftovers), `DbContextHelper` (reflection over `DbSet<>` properties).
- `ThreadSafeDbContext`, `ThreadSafeDbSet`, `ThreadSafeQueryProvider/Queryable/Enumerator/AsyncEnumerator`: a `SemaphoreSlim(1,1)` around every EF call with `EnableThreadSafetyChecks(false)`.
- `EfCoreGenericRepository` (438 lines), `OldEfCoreGenericRepository` (288 lines, dead), `IEfCoreBulkRepository` (BulkInsert/Update/Delete/Merge/Sync/Read/Truncate on `EFCore.BulkExtensions`), `UnitOfWork<TDbContext>` (execution strategy + transaction + SaveChanges).
- References: Ddd.Domain, Json, `EFCore.BulkExtensions 10.0.1`, EF Core + Relational. Motto: poor. Json is unrelated; BulkExtensions is a licensed package forced on every consumer.

**Where it is now.** (a) `src/HsnSoft.Base.EntityFrameworkCore`: `BaseDbContext`, `BaseEntityInterceptor` (audit on Added/Modified; soft-delete turns Deleted into Modified and keeps owned entities), `EfCoreRepository` + `IEfCoreRepository` (reads with `include`, `UpdateWhereAsync` = `ExecuteUpdate`), `EfCoreProviders`/`EfCoreOptions` (retries, command timeout, `LogCommands`), `DbContextHealthCheck`, `DbContextExtensions` (`ExecuteInTransactionAsync`, schema startup step). Tenants moved to `MultiTenancy.EntityFrameworkCore` (`MultiTenantDbContext`, `TenantEntityInterceptor`). Dropped: ThreadSafe* family, bulk repository, `UnitOfWork`, `ConfigureByConvention` as a reflection sweep, `DbContextHelper`, domain event report, concurrency stamp.

**Why.** ADR-0037 (interceptor and `Mark...` methods), ADR-0045, ADR-0048 (bulk: "the old framework had bulk methods on top of a commercially licensed package, only on the relational side, and its set based changes skipped the audit fields and the soft delete"; "no bulk copy package"), ADR-0050, ADR-0074 (tenant filter out of the base), ADR-0119 (bounded retries and timeout; a transaction started by code is refused by a retrying strategy so `ExecuteInTransactionAsync` runs the whole unit again), ADR-0124 (command logging only by a setting), ADR-0118/0125/0131 (startup steps). ThreadSafe*: no ADR names it; inferred: it hid a scoped-context-shared-across-threads bug instead of fixing it (hhs has 0 references to `ThreadSafeDbContext`), serialised every query of a context behind one semaphore, and `ExecuteAsync` in `ThreadSafeQueryProvider` takes `Wait(...)` synchronously (blocking the thread pool).

**Verdict.** Flawed in three places, superseded overall.
1. `OrderByDynamic` was a string handed to System.Linq.Dynamic.Core (`query.OrderBy(options.OrderByDynamic)`): an injection and column-exposure surface; replaced by the `SortFields` whitelist (ADR-0042).
2. ThreadSafe* wrappers (above).
3. `EFCore.BulkExtensions` licence and the bulk methods that skipped audit/soft delete.
Also: `Reload()` on every soft-deleted entry costs a query per delete; reflection per save.

**Gap.** None.
- Bulk copy (millions of rows, temp table, `COPY`): kept dropped by ADR-0048 on purpose; hhs used only `BulkDeleteAsync(predicate)` in content service (`EfCoreCustomerContentVisitRepository`, with `UseTempDB = false`), which `DeleteWhereAsync` covers. None.
- A shared context across threads: the right answer is a context per scope/`IDbContextFactory`; document only if a worker needs it. Low.

**Keep & avoid.**
- Keep: `UnitOfWork.ExecuteAsync` shape (strategy + transaction + rollback) is what `ExecuteInTransactionAsync` now does. The soft-delete handling of owned entities is better in the new interceptor than the old `Reload()`.
- Avoid: semaphore-wrapping a `DbContext`; string-based order-by; a bulk package whose licence a consumer inherits; `DbUpdateConcurrencyException` swallowed into a generic `BaseException` (the caller loses the ability to tell a conflict from a bug); the tenant filter closed over the context instance (fine in EF if done as a query filter on the context member, but written here with `Contains` on a captured list); fail-open when `IDataFilter` is not registered.

---

## HsnSoft.Base.EntityFrameworkCore.MySQL / .PostgreSql / .SqlServer / .Sqlite

**What they were.** csproj only (no `.cs`). MySQL: refs EntityFrameworkCore + Relational + `Pomelo.EntityFrameworkCore.MySql 9.0.0` (a 9.x provider on an EF Core 10 project). PostgreSql: refs EntityFrameworkCore + `Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3`. SqlServer: refs EntityFrameworkCore + `Microsoft.EntityFrameworkCore.SqlServer`. Sqlite: refs EntityFrameworkCore + `Microsoft.EntityFrameworkCore.Sqlite`. Each only existed so the consumer got the provider package transitively (hhs services reference `HsnSoft.Base.EntityFrameworkCore.PostgreSql`, then call `UseNpgsql` themselves). The abstraction/provider split was right (ADR-0003 style); the packages had no behaviour. The Sqlite git history is only "first", "add localization", "9.0", "net core v10" version bumps.

**Where they are now.**
- PostgreSql: (a) `src/HsnSoft.Base.EntityFrameworkCore.PostgreSql`, real code (`AddBasePostgres`, `UsePostgres`, retries, startup lock).
- SqlServer: (a) real code (`AddBaseSqlServer`, `UseSqlServer`).
- MySql: (a) `src/HsnSoft.Base.EntityFrameworkCore.MySql` (note the spelling change from `MySQL`), files `MySqlServiceCollectionExtensions.cs`, `MySqlStartupLock.cs`, on Oracle's `MySql.EntityFrameworkCore 10.0.12`.
- Sqlite: (c) dropped. No ADR or commit says why (inferred: no hhs, Abc, Edx or Hsx service uses Sqlite; the new stack runs real databases in Testcontainers, ADR-0010).

**Why.** ADR-0047 (provider by setting, partly superseded by ADR-0050 properties class), ADR-0076 (MySQL "on Oracle's `MySql.EntityFrameworkCore`: Pomelo has no release for EF Core 10"), ADR-0119/0124/0118.

**Verdict.** Sound but empty; superseded by real provider packages that own retries, health check naming, startup lock.

**Gap.** Sqlite: keep dropped. Low priority: add `EntityFrameworkCore.Sqlite` (capability: Sqlite `DbContext` registration with the same options; references: EntityFrameworkCore only) only if the maintainer wants an embedded/file store for an offline back-office tool, a CLI, or fast provider-free repository tests. hhs does not need it. PostgreSql is what hhs and Hsx use.

**Keep & avoid.** Avoid: a version of a third-party provider that does not match the EF Core major (Pomelo 9.0.0 on EF 10); empty "marker" provider projects.

---

## HsnSoft.Base.MongoDB

**What it was.** 26 files.
- `MongoDbContext` / `BaseMongoDbContext`: a class that builds its own `MongoClient` per context instance (`new MongoClient(settings)` in the constructor, `Client.StartSessionAsync()` called and ignored, finalizer `~MongoDbContext`), `MaxConnectionPoolSize = 1000`, `MinConnectionPoolSize = 5`, `WaitQueueTimeout` 60 s, `ClusterConfigurator` logging every command **at Information with the full command JSON when no `ILoggerFactory` is present** (and Debug otherwise), `ApplicationIdentifier.AppName` (static, from the Tracing package) appended to the application name.
- `TrackingMongoCollection` (about 80 methods of `IMongoCollection<T>` forwarded, setting `LastModificationTime` to `DateTime.UtcNow` and `LastModifierId`/`DeleterId` to `BsonNull` on updates by reflection `GetMember`), `MongoEntityEventArgs/State`.
- `MongoGenericRepository` / `IMongoGenericRepository` (exposes `GetCollection()`, `GetQueryable()` via LinqKit `AsExpandable`, sessions, `UpdateByExpressionAsync`), soft-delete/tenant filter built as `FilterDefinition`, AutoMapper `ProjectTo`; `OldMongoGenericRepository` (dead); the whole `MongoDBOld/` folder (`Document`, `IDocument`, `BaseRepository`, `MongoRepository`, `MongoDbSettings`, `QueryOptions`): dead.
- Misc: `BsonCollectionAttribute` (collection name), `ObjectIdConverter` (Newtonsoft), `DynamicSerializer` (Mongo -> Newtonsoft dynamic), `ReadOption`, `FilterOptions`, `MongoExpressionHelper`, `BsonMemberMapExtensions`.
- References: LinqKit, MongoDB.Driver 3.10.0, Ddd.Domain, Json. Motto: poor (LinqKit, Newtonsoft through Json, dead folders shipped in the package).

**Where it is now.** (a) `src/HsnSoft.Base.MongoDB` (driver + Ddd.Domain only): `AddBaseMongo` (one `IMongoDatabase`, health check, startup lock, `MongoOptions` timeouts and pool bounds, `MongoCommandLog` only when `LogCommands`), `MongoRepository` + `IMongoRepository` (`UpdateWhereAsync` with a `UpdateDefinition`), `MongoEntityConcepts` + `IMongoEntityConcept` (audit, soft delete, tenant as concepts asked on query and write), `MongoEntityMaps.RegisterBases<TKey>()`. Collection name is now an argument of `AddMongoRepository<TEntity,TKey>("collection")`. Tenant concept lives in `MultiTenancy.MongoDB`.

**Why.** ADR-0037 (Mongo has no change tracking, so concepts are called by the repository), ADR-0042 (map the base classes once), ADR-0045 (levels; document store keeps its own interface), ADR-0046, ADR-0048 (set-based change writes audit and soft delete), ADR-0119 (shared client, bounded waits), ADR-0124 (command log by setting). The `Old` classes and `ObjectIdConverter`/`DynamicSerializer`: inferred drops, hhs has 0 references to `IMongoRepository`, `BaseRepository`, `IDocument`, `BsonCollection`, `DynamicSerializer`, `ObjectIdConverter`, `ReadOption`.

**Verdict.** Sound idea (one repository per kind of store), flawed implementation: a client per context; full command text (can include user data) logged at Information by default; a 1000-connection pool default; reflection-based audit in a collection decorator that wrote `null` modifier ids (setting `LastModifierId` and `DeleterId` to `BsonNull` on every update, so an update erased the deleter/modifier id instead of setting the user); `IQueryable` exposed. Superseded.

**Gap.** Sessions/transactions (`StartSession`, the `IClientSessionHandle` overloads): not ported; hhs has 0 uses of `StartSession`; ADR-0045 says the Mongo interface will add "session" later. Low. Add to `IMongoRepository` only when a service needs multi-document atomicity (needs a replica set). `UpdateByExpressionAsync` (10 files in hhs) is covered by `UpdateWhereAsync`. Read preference (`ReadOption`): not used; express it in the connection string. None.

**Keep & avoid.** Keep: the per-update `WaitQueueTimeout`/`MaxTime` idea (now `MongoOptions`). Avoid: a `MongoClient` per context/scope (connection storm), logging commands without a switch, finalizers on a context, forwarding the entire `IMongoCollection` surface just to add two fields, a `Document` base class (`MongoDBOld`) that forces `ObjectId`.

---

## HsnSoft.Base.MultiTenancy

**What it was.** 6 files: `IMultiTenant` (`Guid TenantId { get; }`), `ICurrentTenant` (`Id`, `Normalized`, `IsSystemTenant`, `List<Guid> AllowedTenantIds`, `List<string> AllowedScopeKeys`, `Change(...)` with five arguments), `CurrentTenant` (`ITransientDependency`), `ICurrentTenantAccessor`, `BasicCurrentTenantAccessor` (an `AsyncLocal<BasicTenantInfo>`, **initialised in the constructor from `ICurrentUser` via `provider.GetRequiredService<ICurrentUser>()`**), `BasicTenantInfo`. References: Data, Security (the project that held `ICurrentUser`, claims, `IScopeSubscription`, `ICustomerSubscription`). Motto: over-coupled; tenants were in every consumer (`Auditing`, `Authorization.Abstractions`, `Ddd.Domain` all referenced MultiTenancy), the tenant was read only from the user token, so a message handler or worker had to fake a user. Mutable `List<>` in the public API. The `ICustomerSubscription` filter was already commented out (code left in `GenericRepositoryBase`, `BaseEfCoreDbContext`, `MongoGenericRepository`).

**Where it is now.** (a)+(b) `src/HsnSoft.Base.MultiTenancy` (Core only): `IMultiTenant`, `ICurrentTenant`/`CurrentTenant`, `ITenantSource`, `TenantContext`, `TenantAccessPath` (a tenant sees the tenants under it by a path of ids), `IScopeSubscription`, `TenantAssigner`, `AddBaseMultiTenancy`. Satellites: `.Authentication` (claims `tenant_id`, `is_system_tenant`, `allowed_tenant_id`), `.EventBus` (tenant as headers; handler runs in the tenant of its message), `.EntityFrameworkCore` (`MultiTenantDbContext`, `TenantEntityInterceptor`), `.MongoDB`. `Normalized` (tenant normalized name) was dropped; hhs's auth server still writes the claim `TenantNormalized` ("//TODO : ENCRYPT") but nothing in `shared`/services reads it as a tenant property I could find (not exhaustively verified).

**Why.** ADR-0037 (tenant rules: the system tenant sees all), ADR-0074 (a service that does not want tenants does not get them; sources instead of the user), ADR-0128 (tenant tree, path of ids), ADR-0129 (an entity is isolated by tenant or by scope, never both), memory notes (hhs Hsx is the rewrite).

**Verdict.** Sound model (system tenant, allowed list, `Change(...)` as an `AsyncLocal` disposable), over-coupled packaging; superseded and improved.

**Gap.** None.

**Keep & avoid.** Keep: `Change(...)` returning a disposable that restores the parent (kept). Avoid: tenant state built from `ICurrentUser` in a constructor; public mutable lists; per-feature flags added as parameters of `Change` (five args and growing); commented dead code in three places.

---

## HsnSoft.Base.Auditing

**What it was.** 14 files: `Contracts/IHasCreationTime` (`CreationTime`), `IMayHaveCreator` (`CreatorId`), `IHasModificationTime`, `IModificationAuditedObject` (`LastModifierId`), `IHasDeletionTime`, `IDeletionAuditedObject` (`DeleterId`), `ICreation/IModification/IFullAuditedObject`; `IAuditPropertySetter` + `AuditPropertySetter` writing everything through `ObjectHelper.TrySetProperty` (reflection setting properties, including private setters) using `IClock` and `ICurrentUser`. About 40 lines of the setter are commented tenant checks. References: Data, Json, MultiTenancy, Security, Timing: Json and MultiTenancy were not needed. DateTime based, `UtcNow` through a custom `IClock`.

**Where it is now.** (a) `src/HsnSoft.Base.Auditing` (Core only): the same read shapes (`IAuditedObject`, `IFullAuditedObject`, `ICreationAuditedObject`, ...), plus `IMarkCreated/IMarkModified/IMarkDeleted` that an entity implements; `AuditPropertySetter(TimeProvider, ICurrentUser?)` with `SetCreation/SetModification/SetDeletion/Stamp`, `AuditStamp`. `DateTimeOffset`; `CreatedAt/CreatedBy` names (not `CreationTime/CreatorId`).

**Why.** ADR-0037, ADR-0073 ("audit contracts in `HsnSoft.Base.Auditing`"); no `IClock`, uses `TimeProvider`.

**Verdict.** Sound but over-coupled; superseded (no reflection, the entity decides what a mark does, user id is the guid not the user name).

**Gap.** None. Names differ from hhs's database columns, but compatibility is not a goal (memory).

**Keep & avoid.** Keep: deleting without a user writes null (new does the same); creation also stamps modification. Avoid: reflection setters for audit; coupling the audit package to tenants; commented-out rules left in code.

---

## HsnSoft.Base.Authorization.Abstractions

**What it was.** 20 files: `IPermissionChecker` (`IsGrantedAsync(name)` / with a `ClaimsPrincipal`), `IPermissionConstraintChecker` (`GetValueAsync/GetDecimalAsync/GetIntAsync/GetBooleanAsync`), `IPermissionStore` (`IsGrantedAsync(permission, providerName, providerKey)`, `SetAllPermissions`, `GetAllPermissions`), `IPermissionConstraintStore`, `PermissionAssignment`, `PermissionConstraintAssignment`, `PermissionAuthorizeAttribute` (`: AuthorizeAttribute`, policy `permission:{x}`), `PermissionRequirement`, `PermissionGrantResult` (Undefined/Granted/Prohibited), value-provider contracts, `PermissionProviders` (`"U"`, `"R"`, `"C"`). References: `Microsoft.AspNetCore.Authorization` package, Localization, MultiTenancy (neither used by the contracts). Motto: the interfaces dragged ASP.NET authorization, localization and tenants into any layer that only wanted `IPermissionChecker`.

**Where it is now.** (b) Merged into `Authorization` already on dev-backup, commit `513d8411` "fold single-purpose abstraction projects into their host projects" ("Authorization.Abstractions -> Authorization (namespaces unchanged); it cannot live in Core because it needs Localization, MultiTenancy and ASP.NET Core authorization"). In the new tree the same contracts are in `src/HsnSoft.Base.Authorization`: `IPermissionChecker`, `IPermissionStore`, `PermissionAuthorizeAttribute`, `PermissionRequirement`.

**Why.** Commit message above; then ADR-0081/0083 redesigned the contracts. The folding contradicts ADR-0003 (abstractions separate from providers) in spirit; the commit reasoned that a separate abstraction was single-purpose.

**Verdict.** Sound contracts, a needless project that did not keep the motto.

**Gap.** One real motto issue in the *new* package: `HsnSoft.Base.Authorization` has `<FrameworkReference Include="Microsoft.AspNetCore.App" />`, so a class library (application/domain layer, a worker, a gRPC-only host) that only wants to ask `IPermissionChecker.IsGrantedAsync`/`RequireDecimalAsync` takes the whole ASP.NET shared framework. Recommendation: **fold into an existing package / split** (ADR-0003, ADR-0070): put `IPermissionChecker`, `PermissionType`, `PermissionCatalog` in Core (or a tiny `Authorization.Abstractions`) and keep the policy provider, handler, attribute in `Authorization`. Priority Low (hhs keeps permission checks in controllers: only `AppUsersController`/`AppRoleController` use `[PermissionAuthorize]`; the application layer never asks the checker). Maintainer decision.

**Keep & avoid.** Keep: `PermissionConsts.Prefix` policy-per-permission built on demand (kept as `PermissionPolicyProvider`). Avoid: provider key letters `"U"`/`"R"`/`"C"` as magic strings, `Task<bool>` APIs without a `CancellationToken`.

---

## HsnSoft.Base.Authorization

**What it was.** 19 files. `PermissionChecker` (loops over `IPermissionValueProvider`, first `Prohibited` wins, first `Granted` wins), `PermissionConstraintChecker` (first provider with a value, parsed with `TryParse`, `NumberStyles.Any` for decimal), `PermissionPolicyProvider` (builds `permission:{x}` policies on demand), `PermissionRequirementHandler`, role/user providers over `BasePermissionStore` / `BasePermissionConstraintStore` which are **singletons holding an `ImmutableHashSet` snapshot**: key `"{provider}|{providerKey}|{permission}"` lowercased and accent-stripped, **no tenant in the key**, filled by hhs's `SynchServicePermissionStoreBackgroundService` every `CachePermissionsUpdateSeconds` (default 3600) with the tenant and scope filters turned off, plus a `Thread` polling a static flag `BackgroundServiceFlags.SkipWaitPeriodForSynchPermissionServiceStore` every second. Also `BaseAuthorizationResource` (localization). References: Abstractions, Localization, Security.

**Where it is now.** (a) `src/HsnSoft.Base.Authorization` (Core + `Microsoft.AspNetCore.App`): `PermissionChecker`, `PermissionPolicyProvider`, `PermissionRequirementHandler`, `PermissionAuthorizeAttribute`, `IPermissionStore` + `DistributedPermissionStore` (over `IDistributedCache`, memory cache `Permissions:CacheSeconds` = 30), `IPermissionScopeProvider`, `PermissionCatalog`, `PermissionGrants`, `PermissionSubject`, `PermissionOptions`, `AddBasePermissions`.

**Why.** ADR-0081 (states the old design's costs: "the same grant was kept twice, a change took up to an hour to be seen, and the type of a permission was a string prefix checked by hand"), ADR-0083 (four types; a constraint must be given; old "no value is no limit" removed), ADR-0122 (authentication/authorization are choices).

**Verdict.** Flawed (global, one-hour stale, not tenant-aware, fail-open for constraints), superseded by something better: scope in the key, 30 s staleness, the catalogue is the code, constraints required.

**Gap.** Already covered. Roles-and-users providers: new covers role by name and user by id; the old "client" provider (`C`: a machine client id when there is no role) is not in the new docs I read; hhs has client credentials tokens for the scheduler (ADR-0126 uses a key instead). Verify only if a machine client must hold grants: Low.

**Keep & avoid.** Keep: first-`Prohibited`-wins semantics (new has "no denial" by decision). Avoid: process-global static flags polled by a thread, a tenant-less global grant set, hourly refresh, constraint "absent means unlimited", `NumberStyles.Any` parsing of money from a string.

---

## HsnSoft.Base.Localization

**What it was.** 25 files. JSON `{culture, texts}` files embedded in assemblies and read through `EmbeddedResourceQuery` (from the `VirtualFileSystem` package); `ServiceResourceDictionary.ImportJsonResourceToDictionary`; `CacheStringLocalizer` (in-memory cache key `locale_{culture}_{firstResourceType.Name}`, 30 s sliding expiry, culture taken from `Thread.CurrentThread.CurrentCulture`, `tr-TR` cut to `tr`); `DefaultStringLocalizer(Factory)`, `LocalizableString`, `FixedLocalizableString`, `LocalizationResourceNameAttribute`, `IStringLocalizerFactoryWithMultiple`, `JsonLocalizationDictionaryBuilder` (duplicate-key check), `CultureHelper` (in Core). References: `Microsoft.Extensions.Caching.Memory`, VirtualFileSystem.

**Where it is now.** (a) `src/HsnSoft.Base.Localization` (Core + Localization.Abstractions + Options/Hosting abstractions): `LocalizationStore` (merged layers, `FrozenDictionary`, built once, validated at start), `LanguageAccessor` (claim `locale`, `Accept-Language` with quality, default), `MessageLocalizer`, `StoreStringLocalizer`, `StoreStringLocalizerFactory` (so `IStringLocalizer`, `IStringLocalizer<T>` and `IStringLocalizerFactory` still work: hhs uses `IStringLocalizerFactory` in 95 files), `LocalizationOptions`. Contracts `IMessageLocalizer`, `ILocalizableError`, `ILanguageAccessor` in Core. Dropped: `CacheStringLocalizer*`, `LocalizableString`/`FixedLocalizableString`/`IHasNameWithLocalizableDisplayName` (hhs 0 references), `CultureHelper`, `IStringLocalizerFactoryWithMultiple`.

**Why.** ADR-0038 (lists the old faults): the multi-resource cache key used only the first resource, JSON re-read after 30 s, no fallback chain, language claim `user_lg` written and never read, domain exceptions took an `IStringLocalizer`, no translation completeness check, `CultureInfo` fails under `InvariantGlobalization`; and "does not bring back VirtualFileSystem". ADR-0039 (translating a response body), ADR-0119 (remembered missing keys are bounded).

**Verdict.** Flawed (cache-key bug, `Thread.CurrentThread.CurrentCulture` rather than the request), superseded.

**Gap.** Covered (the standard `IStringLocalizer` surface remains). Texts of DataAnnotations attributes are not translated yet (ADR-0038 consequence); that is a WebApi/validation topic (ADR-0044), not a new package. None.

**Keep & avoid.** Keep: the `{culture, texts}` JSON format (kept, so hhs files port as is) and the duplicate-key / empty-culture checks of `JsonLocalizationDictionaryBuilder`. Avoid: a cache keyed by only part of its inputs, re-reading assemblies on a timer, translating inside the domain, `CultureInfo` for choosing texts.

---

## Open questions for the maintainer

1. **Optimistic concurrency.** Do back-office edits need protection against lost updates? If yes: an opt-in `IHasConcurrencyStamp` in `Ddd.Domain` handled by `BaseEntityInterceptor`/`MongoEntityConcepts`, or document EF's own concurrency token and the `UpdateWhereAsync` pattern only. (hhs never used the old stamp.)
2. **Outbox.** The old domain-event code never delivered events and the new tree has an inbox but no outbox; is "save, then publish, may lose the event" acceptable, or should an opt-in `EventBus.EntityFrameworkCore` outbox be planned (EventBus cluster)?
3. **Authorization package weight.** Split the contracts (`IPermissionChecker`, `PermissionCatalog`, `PermissionType`) out of the package that carries `Microsoft.AspNetCore.App`, so application/domain/worker layers can ask permissions without ASP.NET? (Revisits commit 513d8411.)
4. **Client (machine) grants.** The old `C` provider granted permissions to a client id when a token had no role; does Hsx need machine clients with grants, or is ADR-0126's key enough?
5. **Sqlite.** Is an embedded/file provider wanted at all (offline tools, fast repository tests), or does the Testcontainers approach (ADR-0010) stay the only way?
6. **MongoDB sessions.** Add `IMongoRepository` session/transaction support now or only when a service has a multi-document invariant (needs a replica set)?
