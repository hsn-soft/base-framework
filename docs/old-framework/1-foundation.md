# Cluster A (foundation): what happened to the old foundation projects

Sources read: every file of the old projects on `dev-backup` (paths quoted as `dev-backup:src/...`), the new tree (`src/`, `docs/Overview.md`, ADR-0002..0006, 0036..0046, 0073, 0118, 0119, 0120, 0125), and a grep of `/Users/hasansahin/ws/org/tech-summus/hhs` and `samples/`.

Global fact that explains most "missing" projects: commit `0c82c90e` ("chore: start the rewrite from Base and Core only", 2026-10-22) deleted every `src` project except `HsnSoft.Base` and `HsnSoft.Base.Core`. Everything below that is "back" was re-created from the need of a package, not ported. So a missing old project is a decision by omission, and where an ADR names it, I cite it; otherwise it is marked "inferred".

## Summary

| Old project | Now | Verdict on old design | Gap / priority |
|---|---|---|---|
| `HsnSoft.Base` | same name, same role (name holder on Core) | sound | none |
| `HsnSoft.Base.Core` (98 .cs) | same name, about 10 % of it left; the rest split into Auditing, Data, Logging, Localization, Hosting, AspNetCore, Ddd.* or dropped | flawed (a kitchen sink with ~20 forced packages, static state, hidden DI conventions) | see Core section: PageBy/WhereIf covered, a few small utilities are open (Low) |
| `HsnSoft.Base.Data` (29) | same name, rewritten (`IDataFilter`, `IDataSeeder`, `IStartupLock`, startup steps) | sound idea, flawed code (shared mutable filter state, `IOptionsMonitor.CurrentValue` captured once) | optimistic concurrency stamp is gone and not replaced: Medium |
| `HsnSoft.Base.Timing` (36) | dropped; `TimeProvider` of .NET is used | over-coupled (Clock needed Localization + MultiTenancy + Security) | none; testability of `TimeProvider.System` in samples: Low |
| `HsnSoft.Base.Validation` (9) | dropped; replaced by `Guard`, `ValidationRule`, `ValidationErrors`, attributes in Core and `Localization` texts | flawed (a localized key list only, plus `IHasValidationErrors`) | already covered (ADR-0044) |
| `HsnSoft.Base.Security` (16) | split: `ICurrentUser` in Core, claim names and `ClaimsPrincipal` helpers in `Authentication`, tenant claims in `MultiTenancy.Authentication`; string encryption dropped | `StringEncryptionService` flawed (hard-coded key, IV and salt); the rest sound but over-wide | keep encryption dropped; document "use Data Protection": Low |
| `HsnSoft.Base.Json` | dropped | superseded by plain `System.Text.Json` | none |
| `HsnSoft.Base.Json.Abstractions` | dropped | over-engineered, pulled DeepCloner for nothing | none |
| `HsnSoft.Base.Json.Newtonsoft` | dropped | superseded (nobody calls it in hhs) | none |
| `HsnSoft.Base.Json.SystemTextJson` | dropped; a few converter ideas survive as web api JSON options | partly sound | document only (UTC date rule): Low |
| `HsnSoft.Base.ObjectMapping` | dropped | flawed (scope per call, reflection `Activator`, silent catch) | already covered (ADR-0046, `EntityProjection`) |
| `HsnSoft.Base.AutoMapper` | dropped | an empty project (csproj + AssemblyInfo, no provider class) | none |
| `HsnSoft.Base.VirtualFileSystem` | dropped (ADR-0038 says so) | sound but tiny and null-returning | already covered (Localization reads embedded JSON itself) |
| `HsnSoft.Base.ExceptionHandling` | dropped; `NotFoundException`, `ILocalizableError`, `KnownExceptions` in Core, `GlobalExceptionHandler` in WebApi | the project was only two exception classes and empty resource files | none |

Open items, ranked: (1) optimistic concurrency (`IHasConcurrencyStamp` gone, nothing in `Ddd.Domain`/EF/Mongo) - Medium; (2) `ReplaceInvalidChars` / `SlugKeyNormalize` are used by hhs and Edx-style keys but only `Normalize`/`Minimize` were ported - Low; (3) a documented rule for DateTime kind and time zones - Low; (4) `Calculator` in Core is a leftover from the first test, not a capability - cleanup (None priority, but it breaks "Core is small and meaningful").

---

## HsnSoft.Base

**What it was.** `dev-backup:src/HsnSoft.Base/HsnSoft.Base.csproj` and `README.md` ("This package is a name holder. It just references to the `HsnSoft.Base.Core` package."). One project reference: Core. No code. A `FodyWeavers.xml` with `<ConfigureAwait />` (every old project had it).

**Where it is now.** (a) Same name: `src/HsnSoft.Base/HsnSoft.Base.csproj`, still a name holder on Core (docs/Overview.md last row, ADR-0004).

**Why.** ADR-0004: "`HsnSoft.Base` (the bare name) only references Core, so a package id with the family name exists; it has no tests because there is nothing to run."

**Verdict.** Sound. The `ConfigureAwait` Fody weaver is replaced by an explicit rule in `.claude/rules/dotnet.md` (`ConfigureAwait(false)` on awaits), which removes a build-time IL weaving dependency.

**Gap.** None.

**Keep & avoid.** Nothing to port.

---

## HsnSoft.Base.Core

**What it was.** 98 .cs files (about 190 KB) in `dev-backup:src/HsnSoft.Base.Core`. A kitchen sink in four layers: (1) the ABP-style utility layer (`Check`, extension methods, reflection and type helpers, `TypeList`, `PredicateBuilder`), (2) DI conventions (`ITransientDependency`, `ISingletonDependency`, `IScopedDependency`, `IObjectAccessor`), (3) cross-cutting contracts (exceptions, exception notifier, logger abstractions, correlation id, localization context, `BaseResponse`), (4) configuration helpers.

Package references (`dev-backup:src/HsnSoft.Base.Core/HsnSoft.Base.Core.csproj`): Microsoft.Extensions.Localization, Logging.Abstractions, Logging, **Logging.Console**, DependencyInjection, Options, Options.ConfigurationExtensions, Configuration.EnvironmentVariables, **UserSecrets**, **CommandLine**, Hosting.Abstractions, System.Collections.Immutable, System.Text.Encodings.Web, System.Runtime.Loader 4.3.0, **System.Linq.Dynamic.Core 1.7.2**, System.Linq.Queryable 4.3.0 and System.Linq.Expressions 4.3.0 (obsolete netstandard shims), JetBrains.Annotations, **Nito.AsyncEx.Coordination and Context**, System.ComponentModel.Annotations 5.0.0. No project references. Every consumer of any package got all of these (the motto "never force a library the consumer does not use" is broken ~20 times). New Core references only `Microsoft.Extensions.DependencyInjection.Abstractions` and `Microsoft.Extensions.Logging.Abstractions`.

Hidden coupling and static state found in the code:
- `DefaultBaseLogger` (`Logging/DefaultBaseLogger.cs`) builds a whole `LoggerFactory` in its constructor inside a `using` block, so the factory is disposed before the logger is used (the console provider is torn down); the class also needs `IConfiguration` and parses `Logging:LogLevel:Default` with `Enum.Parse` and a catch-all.
- `EnumHelper.Configure(...)` stores a static `IStringLocalizer`; `ApplicationIdentifier.AppId/AppName` are public static setters; `BaseClaimTypes` (Security) is the same pattern.
- `ObjectHelper.TrySetProperty` caches by a string key built from `propertySelector.ToString()` plus type names (string allocation per call) and uses reflection `SetValue`.
- `RandomHelper` uses one shared `Random` with a `lock`, not usable for tokens (fine for jitter; must never be used for secrets). `GenerateRandomizedList` is O(n^2).
- `BaseStringExtensions.ToMd5` (MD5 for a "hash"), `StringHelper.ReplaceInvalidChars` casts `char` to `byte` (`(byte)upperText[i]`), so a letter whose low byte lands in `A-Z` passes as valid (for example U+0141 passes as `A`), and builds the result by string concatenation in a loop. `FirstCharCapitalize` uses culture-sensitive `ToUpper()`/`ToLower()` (Turkish `i` problem).
- `BaseQueryableExtensions.OrderByIf` passes a client string straight to Dynamic LINQ `OrderBy(sorting)`: a parse-injection surface on every list endpoint.
- `PredicateBuilder.And` (Reflection) builds `Expression.Invoke(expr2, ...)`, which EF Core cannot always translate; the second `PredicateBuilder` in `System/Linq/PredicateOperator.cs` (a LinqKit port) rebinds parameters correctly, so the project had two builders with different quality.
- `CheckSafe` implements each check as try/catch of the throwing `Check` (exceptions as control flow).
- `ExceptionNotifier` creates a DI scope per notification and swallows subscriber failures after logging only the type (the exception `e` is not logged).

### Feature groups of Core and where each went

| Group (old files) | Now | Notes |
|---|---|---|
| Exceptions: `BaseException`, `BaseHttpException`, `BusinessException`, `DomainException`, `IBusinessException`, `IHasErrorCode`, `IHasErrorDetails`, `IHasHttpStatusCode`, `ILocalizeErrorMessage`, `IHasLogLevel` | `BusinessException`, `DomainException`, `BaseHttpException` kept in `src/HsnSoft.Base.Core/HsnSoft/Base/` and made `ILocalizableError` (`WithKey(key, args)`, ADR-0038); new `NotFoundException`, `BusinessRuleException` (`Exceptions.cs`), `ValidationFailedException` (`ValidationErrors.cs`), `KnownExceptions.IsKnown` (expected vs unexpected, ADR-0020 style); `BaseException`, `IBusinessException`, the `IHas*` interfaces, `ILocalizeErrorMessage` (took a `LocalizationContext`, so the exception asked for texts) dropped; `BusinessException` keeps `ErrorCode` and `LogLevel`; no `[Serializable]`/`SerializationInfo` ctors any more | ADR-0038: "the domain stays free of localization: an exception names the key" |
| Exception notifier: `IExceptionNotifier`, `ExceptionNotifier`, `IExceptionSubscriber`, `ExceptionSubscriber`, `ExceptionNotificationContext`, `NullExceptionNotifier`, extensions | dropped; role taken by `GlobalExceptionHandler` + `ApiExceptionMapper` (WebApi) and the single "failure logged once" rule (ADR-0020); the broker side has `FailedEventNotifier` (RabbitMQ) | hhs: 0 uses of `IExceptionNotifier` |
| Guards: `Check`, `CheckSafe` | `Guard` (ADR-0039): `NotNull`, `NotDefault`, `NotNullOrEmpty`, `NotNullOrWhiteSpace` (returns the trimmed text), `Length`, `Range<T>` and `NotNegative<T>` on `INumber<T>` (replaces 6 copies per numeric type); throws `DomainException` with a translation key; `Check`'s public-argument checks are now `ArgumentNullException.ThrowIfNull` (rule in `.claude/rules/dotnet.md`); `CheckSafe` dropped | hhs uses `Check.NotNull(builder, ...)` in 3 DbContexts only |
| Response model: `Communication/BaseResponse`, `IBaseResponse` | same names, `Core/Communication/BaseResponse.cs` (same four fields, nullable annotations) | ADR-0022 owns the envelope |
| Logger abstractions: `IBaseLogger`, `IAppConsoleLogger`, `IFrameworkLogger`, `IFrameworkLog`, `IPersistentLog`, `IRefreshableLogger`, `DefaultBaseLogger`, `LogHelper`, `FrameworkLogModel`, `StackTraceLogDetail` | `IBaseLogger`/`IAppConsoleLogger` kept but moved to `HsnSoft.Base.Logging` (`Abstracts/IBaseLogger.cs`, `AppConsoleLogger.cs`); framework/persistent log model replaced by `LogFields`, `LogTypes`, `LogFlow`, `ExceptionLogFields` (they stay in Core because several packages need them, ADR-0004/0015-0019); `IHasLogLevel` dropped; `IRefreshableLogger` now lives with the Serilog Graylog sink | ADR-0015..0021 |
| Masking: `ILogMasker` | `Core/Logging/Masking/ILogMasker.cs`, plus `[SensitiveData]` and `MaskStrings` (ADR-0021); implementation in `Logging` | |
| Tracing: `ICorrelationIdProvider`, `DefaultCorrelationIdProvider`, `BaseCorrelationIdOptions`, `ITraceAccesor`, `ApplicationIdentifier` | correlation: `LogFlow.Current` (Core) and `IEventHeaderProvider` (EventBus) + `ResponseTrace` (WebApi); process identity: `AppInfo` (`AspNetCore/AppInfo.cs`, from `APP_ID`/`POD_NAME`/`HOSTNAME`, ADR-0121) instead of public statics | `ITraceAccesor` (user id, roles, client lat/long/channel/version) was a product concern; hhs implements it in `Hhs.Shared.Hosting` (`HttpContextTraceAccessor`), which is correct per ADR-0005 |
| DI conventions: `ITransientDependency`, `ISingletonDependency`, `IScopedDependency`, `IObjectAccessor`/`ObjectAccessor`, `IServiceProviderAccessor` | dropped; explicit `AddXxx` registrations with `TryAdd*` | hhs: 0 uses of the marker interfaces |
| Collections: `ITypeList`, `TypeList<T>` | dropped (only the old Json options used it) | |
| Content: `IRemoteStreamContent`, `RemoteStreamContent` | dropped; streams are handled by `IBlobStore` (ADR-0112) and ASP.NET `IFormFile` | hhs: 0 uses |
| IO: `DirectoryHelper`, `FileHelper` | dropped (BCL has `File.ReadAllBytesAsync` etc.; the old `ReadAllBytesAsync` ignored a partial read) | |
| Reflection: `ReflectionHelper`, `TypeHelper`, `EnumHelper`, `Enumeration`, `AssemblyHelper`, `PredicateBuilder`, `FilterBuilder` | only `FilterBuilder<T>` survives (`Core/Reflection/FilterBuilder.cs`), rewritten with a parameter-rebinding visitor (EF-safe, no `Expression.Invoke`); rest dropped | hhs: 7 uses of `FilterBuilder` (kept), 7 of `ReflectionHelper.GetPublicConstantsRecursively` (permission constant lists; the new Authorization package does not need it, so it was not ported) |
| Text: `StringHelper`, `Utf8Helper` | `StringHelper.Normalize` and `Minimize` kept (`Core/Text/StringHelper.cs`, now using `CharUnicodeInfo`); `ReplaceInvalidChars`, `SlugKeyNormalize`, `FirstCharCapitalize`, `Base64*`, `ConvertFromBytesWithoutBom`, `SplitFirstValue` dropped; `Utf8Helper` dropped | hhs: 33 uses of `StringHelper` (Normalize/Minimize/ReplaceInvalidChars); `ReplaceInvalidChars` is used for domain-name keys in hhs, see Gap |
| Extensions in namespace `System` (`BaseStringExtensions` 19 KB, `BaseTypeExtensions`, `BaseObjectExtensions`, collection/list/dictionary/enumerable, stream, datetime, dayofweek, comparable, memberinfo, assembly, event handler, exception) | dropped. `BaseExceptionExtensions.GetLogLevel` is now `BusinessException.LogLevel`/`KnownExceptions`; `GetMessages` is `ExceptionLogFields.ReasonOf`; `PageBy`/`WhereIf`/`OrderByIf` replaced by `ListQueryOptions`/`PagedQueryOptions` and `SortFields<T>` in `Ddd.Domain` | hhs: `WhereIf` only in commented code |
| Linq: `PredicateOperator.cs` (LinqKit-like `ExpressionStarter`, `Or`, `And`, `Extend`) | dropped | hhs: 0 uses |
| Threading: `OneTimeRunner`, `AsyncOneTimeRunner` (needed Nito.AsyncEx) | dropped | |
| Options: `BaseDynamicOptionsManager`, `OptionsBaseDynamicOptionsManagerExtensions` | dropped | |
| Localization: `CultureHelper`, `LocalizationContext`, `BaseCoreStringLocalizerFactoryExtensions` | replaced by `ILanguageAccessor`, `IMessageLocalizer`, `MessageLocalizerExtensions` (Core contracts) and the `Localization` package; language is a short code, not `CultureInfo` (ADR-0038) | |
| Configuration/hosting: `ConfigurationHelper`, `BaseConfigurationBuilderOptions`, `BaseHostingHostBuilderExtensions` (`appsettings.secrets.json`), `ServiceCollectionCommonExtensions`, `ServiceCollectionConfigurationExtensions` | replaced by `AddBaseHosting`, `EnvFile`, `RequireBaseSettings`, `RunBaseAsync` (`AspNetCore`, ADR-0120); the service-collection digging helpers dropped | hhs: `ConfigurationHelper` hits are its own Serilog/Swagger helpers, not this one |
| Misc primitives: `AsyncDisposeFunc`, `DisposeAction`, `NullDisposable`, `NullAsyncDisposable`, `NameValue`, `NamedTypeSelector(+ListExtensions)`, `ObjectHelper`, `RandomHelper`, `IBasicLoader`, `ISoftDelete`, `JsonSerializerOptionsHelper` | `ISoftDelete` kept (only `IsDeleted`, ADR-0073); local `Restore` classes replace `DisposeAction`; `IBasicLoader` replaced by `DatabaseStartupService`/startup steps (ADR-0118); rest dropped | hhs: `IBasicLoader` used in 2 files (its own `AppBasicLoader`, a `Task.Delay(100)` then seeder) |
| New in Core, no old counterpart | `Application/ApplicationService`, `Hosting/StartupSteps` (retry of a dependency at start) and `HealthCheckTags`, `Security/ICurrentUser` (moved from Security, now `Id`, `UserName`, `Roles`), `Security/ActingUser` (AsyncLocal user for background work), `ValidationRule`/`ValidationErrors`, `[NotDefault]`/`[NotWhiteSpace]`/`[NoBlankItems]`, `Calculator` | `Calculator` is a leftover (commit `7142ea1d`, used only by `CalculatorTests`) |

**Where it is now.** (a) Same name, but about 1,100 lines in 32 files instead of 98 files. Most groups are (c) intentionally dropped or (b) moved as the table shows.

**Why.** ADR-0004: "Core is small and has no framework-specific dependencies... A type goes to Core only when two or more packages need it." ADR-0002 (one package, one capability), ADR-0073 (Auditing and Data leave Core), ADR-0074 (no tenant in Core), ADR-0038/0039 (Guard and localization), ADR-0070 (a package brings what its API exposes). The deletion of unused helpers is inferred from commit `0c82c90e` plus ADR-0004; no ADR lists the dropped helpers one by one.

**Verdict.** Flawed as a foundation (forced packages, static state, hidden DI conventions, an injection-prone sort helper). Parts are sound ideas: `FilterBuilder`, `Normalize`/`Minimize`, `BusinessException` with log level, `DataFilter`-style scoped switches. The new split is clearly better and meets the motto.

**Gap.**
- `PageBy`/`WhereIf`/`OrderByIf`: already covered (`PagedQueryOptions`, `SortFields<T>` allow-list, ADR-0045/0046).
- DI marker interfaces and `TypeList`: keep dropped (reflection scanning contradicts explicit registration).
- `ReplaceInvalidChars`/`SlugKeyNormalize` (slug for domain names/keys): hhs uses it in `CustomerVpSetting` and a seeder; if Hsx needs a normalised key, add it to `StringHelper` with a correct (char-range, `StringBuilder`) implementation. Priority Low, "fold into an existing package" (Core `StringHelper`).
- `RandomHelper`: use `Random.Shared` / `RandomNumberGenerator`; keep dropped.
- `RemoteStreamContent`: already covered by BlobStoring streams and `IFormFile`; keep dropped.
- `EnumHelper` localized display values (`[Display(Name=...)]` with a static localizer): the new rule is "translate where the response body is made" (`IMessageLocalizer`); keep dropped.
- `CultureHelper.Use(...)` (scoped culture switch for a background job): not needed because language is a short code (`ILanguageAccessor`); if a worker must send a mail in a user's language, `ActingUser`-style scope for the language might be added later. Priority None now.

**Keep & avoid.**
- Port (already ported or worth citing): `dev-backup:.../Reflection/FilterBuilder.cs` idea, `StringHelper.Normalize/Minimize`, `Exception.GetMessages()` inner-exception walk (now `ExceptionLogFields`), `BusinessException.WithData`.
- Worth porting if needed: `dev-backup:src/HsnSoft.Base.Core/System/Linq/PredicateOperator.cs` `Or` with parameter rebinding (the new `FilterBuilder` has only `And`; an OR filter builder will be needed for "search in several fields" lists). Priority Low.
- Pitfalls never to repeat: a `LoggerFactory` created and disposed in a constructor; public static mutable configuration (`ApplicationIdentifier`, `BaseClaimTypes`, `EnumHelper._localizer`); try/catch as a boolean check (`CheckSafe`); client text passed to Dynamic LINQ; `(byte)char` casts; MD5 as a helper; netstandard-era shim packages (`System.Linq.Queryable 4.3.0`, `System.Runtime.Loader 4.3.0`); references to Console logging and UserSecrets from a base library.
- Note on the new Core (observation, not asked): `ActingUser` is a static `AsyncLocal`; it is scoped and restored, so acceptable, but it is the only static state left in Core and should stay the only one.

---

## HsnSoft.Base.Data

**What it was.** `dev-backup:src/HsnSoft.Base.Data` (29 files), reference: Core only. Contents: data filters (`IDataFilter`, `IDataFilter<T>`, `DataFilter`, `DataFilterState`, `BaseDataFilterOptions`); seeding (`IDataSeeder`, `DataSeeder`, `IDataSeedContributor`, `DataSeedContext`, `DataSeedContributorList`, `BaseDataSeedOptions`, `IBasicDataSeeder`, `DefaultBasicDataSeeder`); connection strings (`IConnectionStringResolver`, `DefaultConnectionStringResolver`, `ConnectionStringNameAttribute`, `BaseDbConnectionOptions`, `BaseDatabaseInfo(Dictionary)`, `ConnectionStrings`); `BaseDbConcurrencyException`, `IHasConcurrencyStamp` + `SetConcurrencyStampIfNotNull`; `BaseCommonDbProperties` (static `DbTablePrefix = "Base"`, `DbSchema`).

Defects:
- `DataFilter<T>` is a singleton holding `AsyncLocal<DataFilterState>` where `DataFilterState` is a mutable class: `EnsureInitialized` assigns once per async flow and `Enable/Disable` mutate that shared object. A child task spawned before a `Disable()` shares the same reference, so a disable inside a child (or a parent after the child started) leaks to the other flow.
- `DefaultConnectionStringResolver` takes `IOptionsMonitor<>.CurrentValue` once in the constructor (transient, so it re-reads per resolution but the monitor semantics are wasted) and `Resolve` is `[Obsolete]` beside `ResolveAsync` returning `Task.FromResult`.
- `DataSeeder` runs all contributors of all databases in one scope with no ordering, no lock for multiple pods and no switch to turn one off (ADR-0125 context says exactly this). `DefaultBasicDataSeeder` is a `Task.Delay(100)`.
- `BaseCommonDbProperties` is global mutable static configuration.
- `ConnectionStringNameAttribute` falls back to `type.FullName` as the name, so a missing attribute silently looks up a strange key.

**Where it is now.** (a) Same name, rewritten: `src/HsnSoft.Base.Data/HsnSoft/Base/Data/`: `IDataFilter` (non-generic only) + `DataFilter` (an `AsyncLocal<Dictionary<Type,bool>>`, a new dictionary on every `Enable/Disable` so scopes of other flows keep theirs), `IDataSeeder` + `AddDataSeeder<T>(connectionName)` (ADR-0125), `DatabaseStartupService`, `DatabaseStartupSteps`, `StartupDatabase`, `IStartupLock` (ADR-0118), `AddBaseDataFilter()`. References: Core + three Microsoft abstractions packages (Configuration, DI, Hosting).
Dropped: the connection-string resolver family (connection names are now parameters of `AddBaseEfCore/AddBasePostgres/AddBaseMongo` and the section `ConnectionStrings:<name>`, ADR-0119; the "database to tenants" mapping went with ABP's multi-database tenants, ADR-0074/0128/0129), `BaseCommonDbProperties`, `IBasicDataSeeder`/contributors/`DataSeedContext` (tenant-bound seed context), `IHasConcurrencyStamp`, `BaseDbConcurrencyException`.

**Why.** ADR-0073 (Data is a package of its own, "where what every data store shares will go (seeding, connection names)"), ADR-0118 (one owner host, one pod at a time under a database lock), ADR-0125 (seeders are steps), ADR-0119. The concurrency stamp is not mentioned in any ADR: inferred omission (grep of `ConcurrencyStamp|RowVersion|DbUpdateConcurrency|IsConcurrencyToken` in `src` and `samples` finds nothing; hhs has no use either).

**Verdict.** Sound but with flawed code; superseded by a better design (copy-on-write filter state, seeders as locked startup steps, `Seeding:Disabled`).

**Gap.** Optimistic concurrency: **Medium**, "add to an existing package". Neither hhs nor the samples use it today, but Hsx has transfer/sale flows where two writers can change one row (the hhs analysis itself replaces a distributed lock with "claims by compare-and-set", `samples/hsx/docs/hhs-analysis.md` line 71). Recommendation: do not port `IHasConcurrencyStamp` as a string set by callers; add a small `IHasVersion`-style marker to `Ddd.Domain` (EF: `xmin` on PostgreSQL / `rowversion` on SQL Server mapped by the provider package; Mongo: version field in `MongoEntityConcepts`) and map `DbUpdateConcurrencyException` to a 409 in `ApiExceptionMapper`. Needs a maintainer decision (open question 1).
Everything else: already covered or keep dropped.

**Keep & avoid.** Keep: the "switch that follows the flow of the code" semantics (already kept and fixed). Avoid: shared mutable state inside an `AsyncLocal`; static table prefix/schema; reading options once in a constructor; seeders without a lock and an off-switch.

---

## HsnSoft.Base.Timing

**What it was.** `dev-backup:src/HsnSoft.Base.Timing` (36 files, 24 of them translations of two display strings). References: **Localization, MultiTenancy, Security** (project) and `TimeZoneConverter 7.2.0`. Public types: `IClock` (`Now`, `Kind`, `SupportsMultipleTimezone`, `Normalize(DateTime)`), `Clock` (`Now` is `DateTime.UtcNow` or `DateTime.Now` by `BaseClockOptions.Kind`), `BaseClockOptions` (default `Unspecified`, but `AddBaseTimingServiceCollection` forces `Utc`), `DisableDateTimeNormalizationAttribute` (not read by any code), `ITimezoneProvider` + `TZConvertTimezoneProvider` (Windows/IANA lists and conversion), `TimingSettingNames`, `BaseTimingResource` + 24 `Localization/*.json` for "Timezone".

Motto check: a clock that references a tenant package, a security package and a localization package is the clearest sibling-coupling in the old tree (none of the three is used by the code). The 24 JSON files are dead text: no `ISettingProvider` exists in the framework.

**Where it is now.** (c) Intentionally dropped; .NET's `TimeProvider` (`TimeProvider.System`) replaces `IClock.Now`. `Auditing` registers it (`services.TryAddSingleton(TimeProvider.System)`, `AuditPropertySetter(TimeProvider clock, ICurrentUser?)`), the event bus and startup locks take a `TimeProvider`. Time zone conversion: `TimeZoneInfo.FindSystemTimeZoneById` accepts IANA ids on all platforms and .NET has `TimeZoneInfo.TryConvertIanaIdToWindowsId` / `TryConvertWindowsIdToIanaId` since .NET 8 (needs ICU; not available under `InvariantGlobalization`, which ADR-0038 says containers use), so `TimeZoneConverter` is not needed.

**Why.** Inferred (no ADR names Timing): commit `0c82c90e` removed it and `Auditing` was re-created on `TimeProvider`. ADR-0002/0004 forbid the old reference shape.

**Verdict.** Over-coupled; superseded by `TimeProvider` (testable with `FakeTimeProvider`, no package).

**Gap.** hhs: only `AddBaseTimingServiceCollection()` and `BaseClockOptions.Kind = Utc` in two EF registrations; no `IClock`, no `ITimezoneProvider` use anywhere. Nothing is missing. Two small notes: (1) Hsx calls `TimeProvider.System` directly in `EfCoreSubscriptionManager`/`EfCoreRefreshTokenStore` instead of injecting the registered provider, which makes those paths untestable with a fake clock: inject `TimeProvider` (Low). (2) The one real capability, "all DateTimes enter and leave the API as UTC", lived in `BaseDateTimeConverter`; the new WebApi JSON options (`WebApiServiceCollectionExtensions.ConfigureJson`) only add string enums. Recommendation: **document only** (use `DateTimeOffset`/UTC in contracts) or, if a service must accept local `DateTime`, a 20-line converter in `AspNetCore.WebApi`, not a package. Priority Low.

**Keep & avoid.** Keep: the idea that `Normalize` forces a `Kind` at the edge (as a documented rule). Avoid: a base library referencing three siblings for no code use; forcing a default via `Configure` inside an `Add` method that overrides what the host configured earlier (`AddBaseTimingServiceCollection` sets `Kind = Utc` unconditionally).

---

## HsnSoft.Base.Validation

**What it was.** `dev-backup:src/HsnSoft.Base.Validation` (9 files), reference: Localization. `IHasValidationErrors` (`IList<ValidationResult>`), `ValidationResource` + `ValidationResourceKeys` (`Validation:Required`, `IsNotEmpty`, `MinLength`, `MaxLength`, `Range`, `Email`, `Phone`, `Credit`, `ErrorCode`, `ErrorReference`) and three JSON files (en, ru, tr). No validator.

**Where it is now.** (b) Merged and re-designed: `Guard` (domain checks), `ValidationRule`, `ValidationErrors`/`ValidationFailedException`, `[NotDefault]`, `[NotWhiteSpace]`, `[NoBlankItems]` in Core; the texts `Validation:*` live in the framework's own `Localization/*.json` of the WebApi package; `ModelStateTranslator` (WebApi) translates attribute messages.

**Why.** ADR-0039 (Guard replaces `LocalizedModelValidator`) and ADR-0044 (requests are validated by attributes, rules and a collector; one 400 with every error).

**Verdict.** Flawed/incomplete in the old tree (keys only; the real validation was `LocalizedModelValidator` in product code, which called a static localizer, ADR-0039 context). Superseded by something better: no static state, attribute-first, all errors at once.

**Gap.** Already covered. hhs: 77 files use `ValidationResourceKeys` mostly as `ErrorReference` text for `WithData`; Hsx does not need it (ADR-0039: "the old `ErrorReference`... are not done").

**Keep & avoid.** Keep: the key naming `Validation:*` (kept verbatim). Avoid: a project whose only content is a resource key list; validators that need a static localizer.

---

## HsnSoft.Base.Security

**What it was.** `dev-backup:src/HsnSoft.Base.Security` (16 files), reference: Core only. Parts:
1. Claims: `BaseClaimTypes` (static settable strings: user id, role, email, phone, `tenant_id`, `tenant_normalized`, `is_system_tenant`, `allowed_tenant_id`, `allowed_customer_id`, `allowed_scope_key`, `client_id`, `user_lg`), `ICurrentPrincipalAccessor`, `CurrentPrincipalAccessorBase` (AsyncLocal override of the principal) and extensions, `BaseClaimsIdentityExtensions` (`FindUserId`, `FindTenantId`, `FindAllowedTenantIds`, `FindAllowedScopeKeys`, `FindClientId`).
2. Users: `ICurrentUser` (22 members including tenant fields, allowed tenants/scopes, phone/email verified, security stamp) + `CurrentUser`.
3. Encryption: `IStringEncryptionService`, `StringEncryptionService`, `BaseStringEncryptionOptions`.
4. `IScopeSubscription` (`ScopeKey`) and a commented-out `ICustomerSubscription`.

Problems: **security** - `BaseStringEncryptionOptions` hard-codes a default pass phrase (`"gsKnGZ041HLL4IM8"`), a fixed IV (`"jkE49230Tf093b42"`) and a fixed salt (`"hgt!16kl"`) in source: every deployment shares the key, AES-CBC with a constant IV is deterministic, there is no authentication (no MAC/GCM), `Rfc2898DeriveBytes` is used with the default SHA-1 and 1000 iterations, and the decrypt loop allocates a buffer per read. Also: tenant fields in a generic `ICurrentUser` (the user contract carried tenant concerns, against ADR-0074), `BaseClaimTypes` mutable statics, `AddBaseSecurityEncryptionCollection` registers it as a singleton with the insecure defaults, dead commented code (`AllowedCustomerIds`).

**Where it is now.** (b) Split by capability:
- `ICurrentUser` -> Core (`Security/ICurrentUser.cs`: `IsAuthenticated`, `Id`, `UserName`, `Roles`), implemented by `HttpContextCurrentUser` in `Authentication` (ADR-0036/0037).
- `BaseClaimTypes`, `ClaimsPrincipalExtensions` -> `HsnSoft.Base.Authentication` (`Security/BaseClaimTypes.cs`, `ClaimsPrincipalExtensions.cs`, JWT options, RSA keys, `BasePolicies`).
- Tenant claims (`tenant_id`, `is_system_tenant`, `allowed_tenant_id`) -> `MultiTenancy.Authentication` (ADR-0074/0128); `allowed_scope_key` and `IScopeSubscription` -> `MultiTenancy/IScopeSubscription.cs` (ADR-0129).
- `ICurrentPrincipalAccessor` + AsyncLocal override -> replaced by `Core/Security/ActingUser` (AsyncLocal `UserContext`, `ActingUser.Use(...)`) for non-request code.
- String encryption: (c) dropped.

**Why.** ADR-0036, 0037, 0073, 0074, 0122 (a service chooses authentication and authorization), 0129. Encryption: inferred; no ADR mentions it, nothing in the new tree references it, hhs has 0 uses.

**Verdict.** The split is "superseded by something better". Old project: sound shape for the claims part but over-wide; the encryption part is flawed and unsafe by default.

**Gap.** String encryption: keep dropped. If a system needs to store a secret at rest (an integration credential in a database, Edx's e-invoice provider account, Hsx third-party keys), the answer is ASP.NET Core Data Protection (`IDataProtector`, keys persisted to a shared store) or the platform secret store, not a framework AES helper with fixed keys. Priority Low, **document only** (a short paragraph in a security doc; say never to port `StringEncryptionService`). `ICurrentPrincipalAccessor`: hhs registers its own `HttpContextCurrentPrincipalAccessor` in 2 files; the new `ICurrentUser` plus `ActingUser` covers this; keep dropped.

**Keep & avoid.** Keep: GUID-safe claim parsing with dedupe (`FindAllowedTenantIds`), the AsyncLocal "change principal for a block" pattern (now `ActingUser`). Avoid: any crypto with constants in source, CBC without authentication, mutable static claim names, a user interface that grows by every claim.

---

## HsnSoft.Base.Json, Json.Abstractions, Json.Newtonsoft, Json.SystemTextJson

**What they were.** Four projects (`dev-backup:src/HsnSoft.Base.Json*`) forming an ABP-style serializer abstraction.
- `Json.Abstractions` (3 types): `IJsonSerializer`, `IJsonSerializerProvider` (`CanHandle`, `Serialize`, `Deserialize`), `BaseJsonOptions` (`Providers` as `TypeList`, `UseHybridSerializer`, `DefaultDateTimeFormat`). References: Core and **DeepCloner 0.10.4** (no code uses it).
- `Json.Newtonsoft`: provider with camel case except dictionary keys, `BaseJsonIsoDateTimeConverter` (normalises `DateTime.Kind` through `IClock`); references Abstractions, **Timing** (and via it Localization, MultiTenancy, Security), Newtonsoft.Json 13.0.4 and **Newtonsoft.Json.Schema 4.0.1** (commercial-licence package with limits, unused).
- `Json.SystemTextJson`: provider with options cache, converters `BaseDateTimeConverter`/`BaseNullableDateTimeConverter` (clock normalisation + optional format), `BaseStringToBooleanConverter` ("true" string to bool), `BaseStringToEnumConverter/Factory`, `ObjectToInferredTypesConverter` (object to long/double/DateTime), `BaseSystemTextJsonUnsupportedTypeMatcher`; references Abstractions, Timing, System.Text.Json.
- `Json`: `BaseHybridJsonSerializer` picks a provider per call (creates a DI scope on each `Serialize`/`Deserialize` call and resolves provider services inside it), `AddBaseJsonServiceCollection` registers both and **calls `AddBaseTimingServiceCollection`**; references both providers, so referencing `Json` pulls Newtonsoft + Json.Schema + STJ + Timing + Localization + MultiTenancy + Security.

Problems (motto): forced Newtonsoft and its schema library on every user; a per-call scope in a hot path; the "hybrid" choice made the format depend on the type (`CanHandle` in reverse order, Newtonsoft handled everything), so the same object could be written two ways; `BaseHybridJsonSerializer` dereferences `type.GetFullNameWithAssemblyName()` when `type` is null in the error branch; `BaseStringToBooleanConverter.Read` falls through to `reader.GetBoolean()` for non-bool tokens (exception); `ObjectToInferredTypesConverter.Write` throws NRE for `null`; the DateTime converter reads with `CultureInfo.CurrentUICulture` (culture-dependent parse).

**Where they are now.** (c) Dropped, all four. Replaced by direct `System.Text.Json`: `EventSerializer` (EventBus, case-insensitive options), `ApiResponseWriter`/`WebApiServiceCollectionExtensions.ConfigureJson` (`JsonSerializerDefaults.Web` + `JsonStringEnumConverter`), `LogMasker`, `AppInfo`. The `[SensitiveData]` masking replaces Newtonsoft-contract-based masking.

**Why.** Inferred (no ADR names Json). Commit `0c82c90e` removed them; ADR-0002/0003/0004 forbid a package that forces a library; the new web api standard is one response envelope in System.Text.Json (ADR-0022). hhs evidence: 0 uses of `IJsonSerializer` or `HsnSoft.Base.Json` namespaces; 3 csproj files reference `HsnSoft.Base.Json.Newtonsoft`, but the code uses `Newtonsoft.Json` and `System.Text.Json` directly (`Hhs.Shared.Hosting.Microservices/Cache/CachePermissionAssignmentRepository.cs`, `Extensions/ServiceCollectionExtensions.cs`), i.e. only for the transitive package.

**Verdict.** Over-engineered and over-coupled; superseded by plain `System.Text.Json` (a serializer abstraction over two libraries was never used through its interface).

**Gap.** None needed as packages. `Json.Abstractions`/`Json` abstractions: keep dropped. Newtonsoft: if a legacy contract requires it, the consumer adds the package (motto: never force a library). Converters worth keeping as ideas, **document only**, Low: tolerant bool-from-string for clients that send `"true"`; enum as string (already done); UTC normalisation of `DateTime` (see Timing). If those become real needs, they belong to `AspNetCore.WebApi` options, not a Json package. Do not add a "Json" package.

**Keep & avoid.** Keep: the camel-case-except-dictionary-keys rule (STJ does that by default for dictionaries only with `DictionaryKeyPolicy = null`, which is the Web default); options cache keyed by flags (`BaseSystemTextJsonSerializerProvider`). Avoid: a DI scope per call on a hot path; a library chosen per type at runtime; unused package references (DeepCloner, Json.Schema); parsing with the current culture.

---

## HsnSoft.Base.ObjectMapping

**What it was.** `dev-backup:src/HsnSoft.Base.ObjectMapping` (11 files), reference: Core. `IObjectMapper` (+ `IObjectMapper<TContext>` and `IObjectMapper<TSource,TDestination>`), `DefaultObjectMapper` (tries a registered specific mapper, then `IMapTo<T>`, then `IMapFrom<T>` via `Activator.CreateInstance(typeof(TDestination), source)` inside a bare `catch`, then the "auto" provider), `IAutoObjectMappingProvider`, `NotImplementedAutoObjectMappingProvider`, `ObjectMapperExtensions` (MakeGenericMethod reflection), registration `AddTransient(typeof(IObjectMapper<>), typeof(DefaultObjectMapper<>))`.

Problems: `Map` creates a new DI scope on **every call** to look up a specific mapper (the `//TODO: It can be slow` comment says so); reflection with `Activator` and an empty `catch {}` hides real construction failures; `ObjectMapperExtensions` is a reflection call per use; the default provider throws `NotImplementedException` at runtime instead of failing at startup; a leaked ABP concept (`IObjectMapper<TContext>`) with a commented-out registration hook.

**Where it is now.** (c) Dropped, replaced by DTO projection built from the DTO: `EntityProjection.For<TEntity,TDto>()` in `Ddd.Domain/HsnSoft/Base/Domain/Repositories/EntityProjection.cs`, exposed as `FindAsync<TDto>`/`GetListAsync<TDto>`/`GetPagedListAsync<TDto>` (ADR-0046).

**Why.** ADR-0046: "A mapper is a package with a history of surprises... and the old system needed it only for this"; writing is by hand through the entity's own methods (ADR-0042).

**Verdict.** Flawed (per-call scope, silent catch, runtime NotImplemented). Superseded by something better (expression projection executes in the database, errors found at first use with all names listed).

**Gap.** Already covered. hhs: `IObjectMapper` 0 uses; hhs code uses `AutoMapper` directly (27 files, 63 `IMapper`/`CreateMap` hits) because the old `HsnSoft.Base.AutoMapper` had no implementation. In Hsx, the mapper is replaced by projection and entity methods; no gap.

**Keep & avoid.** Keep: the idea of a conversion contract on the DTO (`IMapFrom`/`IMapTo`) only as optional; the new design needs none. Avoid: scope-per-call service location, `Activator` in a hot path, `catch {}`.

---

## HsnSoft.Base.AutoMapper

**What it was.** `dev-backup:src/HsnSoft.Base.AutoMapper`: only `HsnSoft.Base.AutoMapper.csproj`, `Properties/AssemblyInfo.cs` (InternalsVisibleTo) and Fody files. **No class.** The project referenced `AutoMapper 14.0.0`, **Auditing** and ObjectMapping. In other words a package that ships an assembly with no code and forces AutoMapper 14 (a version line with a commercial licence key requirement) plus Auditing on users. (Other old projects referencing it: `AspNetCore.Mvc.UI`, `Ddd.Domain`.)

**Where it is now.** (c) Dropped.

**Why.** ADR-0046 (no mapper); inferred for the empty shell.

**Verdict.** Flawed (empty package with a forced licensed dependency, and an unrelated sibling reference to Auditing).

**Gap.** None. hhs services reference `AutoMapper` themselves; if Hsx ever wants a mapper, a service references it directly. Not worth a package ("keep dropped", None).

**Keep & avoid.** Nothing to port. Avoid shipping a package with no code, and referencing a licensed library from a base package.

---

## HsnSoft.Base.VirtualFileSystem

**What it was.** `dev-backup:src/HsnSoft.Base.VirtualFileSystem` (7 files): `IEmbeddedResourceQuery`/`EmbeddedResourceQuery` (read a manifest resource stream by type, assembly or assembly name) and `IEmbeddedFileProviderQuery`/`EmbeddedFileProviderQuery` (via `EmbeddedFileProvider`). References: `Microsoft.Extensions.FileProviders.Composite`, `.Physical`, `.Embedded` (Physical and Composite are unused by the code) and Core. Used by old `Localization` (`ServiceResourceDictionary`), `AspNetCore`, `Swashbuckle`.

Problems: returns `null` streams when a resource is missing (and `GetFileInfo(...).CreateReadStream()` throws `FileNotFoundException` on a missing one, so the two classes disagree); `Assembly.Load(assemblyName)` by string; `EmbeddedFileProvider` is created per call; Physical/Composite packages are dead weight.

**Where it is now.** (c) Dropped by decision. ADR-0038: "It does not bring back `HsnSoft.Base.VirtualFileSystem`: reading a text embedded in an assembly is a few lines inside the package." Implemented in `Localization/LocalizationStore.cs` (manifest resources of `Localization/*.json`, checked at start: duplicate keys, wrong culture, unreadable files stop the application).

**Why.** ADR-0038 (explicit).

**Verdict.** Sound but tiny; not worth a package. Superseded by a private reader in Localization.

**Gap.** Already covered. Another embedded-resource consumer (Swagger UI assets, e-mail templates, SQL scripts for the startup steps) would copy the same few lines; if a second package ever needs it, ADR-0005's rule ("needs a second product that wants it unchanged") says wait. None.

**Keep & avoid.** Keep: read by `Assembly.GetManifestResourceStream` with the resource name derived from the assembly. Avoid: nulls for missing resources, `FileProviders.Physical/Composite` references with no use.

---

## HsnSoft.Base.ExceptionHandling

**What it was.** `dev-backup:src/HsnSoft.Base.ExceptionHandling` (10 files), reference: Localization and `FileProviders.Embedded`. Contents: `EntityNotFoundException` (type, id), `EntityDuplicateException` (type) in namespace `HsnSoft.Base.Domain.Entities`; `BaseAuthorizationResource` (a resource class named "Authorization" in an exception package, with `en/de/ru/tr.json` containing empty `texts`). The actual exception interfaces and notifier were in Core.

Problems: a package whose JSON files are empty and whose resource class has an unrelated name; the exceptions subclass `BaseException` and put the entity's full type name in the message, which leaks internal type names to the client; a reference to Localization for no code use.

**Where it is now.** (b) Moved/merged: `NotFoundException` and `BusinessRuleException` (Core `Exceptions.cs`), mapped to 404/400 by `ApiExceptionMapper`; `KnownExceptions.IsKnown` decides log level; the exception handling pipeline is `GlobalExceptionHandler` + `ApiExceptionMapper` + `ApiErrorOptions` in `AspNetCore.WebApi` (production hides details, ADR-0022/0020). No duplicate exception: duplicates are `BusinessRuleException`/`DomainException` with a key.

**Why.** ADR-0038/0039 (exceptions name a key, domain free of localization), ADR-0022, ADR-0020; inferred for dropping the two entity exceptions (hhs: 0 uses of `EntityNotFoundException`).

**Verdict.** Flawed (empty resources, internal names in messages); superseded by the Core exception set plus the mapper.

**Gap.** Already covered. Check one detail: `NotFoundException` takes a message only; if the repositories should say which entity and id (for logs, not for clients), add an `(Type, id)` factory next to it: Low, "fold into Core". Nothing else.

**Keep & avoid.** Keep: `EntityNotFoundException(type, id)` data as log fields (not in the client message). Avoid: type names in client messages; empty resource packages.

---

## Open questions for the maintainer

1. **Optimistic concurrency**: should `Ddd.Domain` get a version marker (EF `xmin`/`rowversion`, Mongo version field) with a 409 answer, now that Hsx has concurrent writers (transfers, subscriptions)? The old `IHasConcurrencyStamp` was never used by hhs, so this is a new feature, not a port.
2. **Slug/key normalisation**: should `StringHelper.ReplaceInvalidChars`/`SlugKeyNormalize` come back (hhs and domain-name keys use them), and if yes in Core `Text` (corrected) or in the product shared project?
3. **DateTime rule**: write a short documented rule (UTC `DateTimeOffset` in contracts) and add a UTC-normalising converter to `AspNetCore.WebApi` only if a client sends local `DateTime`?
4. **Secrets at rest**: confirm "no framework encryption helper; use Data Protection / platform secret store" so the old `StringEncryptionService` is explicitly closed (an ADR line or a security doc paragraph).
5. **Core cleanup**: remove `Calculator` (and its test) from Core, or keep it as a deliberate smoke test? It contradicts "Core is small and meaningful".
6. **OR filters**: add `Or` to `FilterBuilder` (old `PredicateOperator.cs` has a correct rebinding `Or`) when list search over several fields is built?
7. **Time in samples**: should Hsx inject the registered `TimeProvider` instead of calling `TimeProvider.System` (testability), as a sample rule?
