# Cluster E: old tests, repo tooling, cross-cutting rules, old dependency graph

Evidence: old tree extracted from `dev-backup` to a scratch copy (46 `src` projects, 471 .cs files, 8 test projects); graph computed by a scratch copy from the csproj files (old and new). New tree = working tree of `feat/new-framework` (37 `src` projects, 38 `test` folders, ~1218 `[Fact]/[Theory]`, of which 99 in `test/HsnSoft.Base.IntegrationTests`).

## Cluster summary

| Old item | Now | Verdict | Gap / priority |
|---|---|---|---|
| `Test.Unit` (69 tests: EF Postgres + Mongo generic repository CRUD) | `test/HsnSoft.Base.EntityFrameworkCore.Tests`, `.MongoDB.Tests`, `IntegrationTests/Data/{PostgreSql,MySql,Mongo}RepositoryTests` | superseded (better) | Already covered; None |
| `Test.Api` (web host, controllers, 4 workers, fake user, event handler) | `samples/abc`, `samples/edx`, `samples/hsx` | superseded (demo role only) | Already covered; None |
| `Test.Api.Application` / `.Domain` / `.EfCore` / `.MongoDb` | the layered shape lives in the samples | superseded | None |
| `Test.Api.Helper`, `Test.Api.Hosting` | dropped (were empty `Class1`; Hosting only held Swashbuckle+Serilog refs) | flawed (dead scaffolding) | None |
| `common.props`, `common.test.props`, `configureawait.props` | `common.props`, `common.test.props`, `common.versions.props` (no Fody) | improved | Low: ConfigureAwait is a convention, not enforced |
| Dockerfile (build+test+pack+push in one image) | `.github/workflows/ci-control-workflow.yml`, `deploy-global-nuget.yml`, `scripts/pack-local.sh` | superseded | None |
| 6 workflows (build-test, codecov, codeql, sonarqube, deploy-global, publish-github) | 3 workflows (`ci-branch-rule`, `ci-control-workflow`, `deploy-global-nuget`) | improved, with two lost items | Medium: CodeQL; Low: GitHub Packages dev feed |
| `analysis_sonar.ps1`, `clear_bin_obj.ps1` | `scripts/sonar/*`, `scripts/cleaner/*` | improved (token no longer in the script) | None |
| CONTRIBUTING (a single word) / README (badges+1 paragraph) | CONTRIBUTING, `docs/` (18 references), 130+ ADRs, `.claude/rules` | improved | None |
| Old dependency graph | new graph | old graph clearly violated the motto in 9 places (section 4) | n/a |

---

## 1. The old test projects

### 1.1 `HsnSoft.Base.Test.Unit` (`dev-backup:test/HsnSoft.Base.Test.Unit`)

**What it was.** The only project that contained real assertions in the whole old repository. xUnit 2.9.3 + FluentAssertions 8.10 + coverlet; also declared Moq 4.20.72 and `EntityFrameworkCore.InMemory` (never used: `grep` finds no `using Moq`, no InMemory use). References: `EntityFrameworkCore.PostgreSql`, `MongoDB`.
- `Repositories/EfCoreGenericRepositoryUnitTests.cs`: 36 tests; `MongoGenericRepositoryUnitTests.cs`: 33 tests. Identical case list for both stores: `GetById` (found / `EntityNotFoundException`), `GetByIdOrDefault`, `GetSingle` (found / not found / `EntityDuplicateException`), `GetSingleOrDefault`, `GetFirstOrDefault` (order), `GetList`, `GetPageList`, `GetCount`, `Exists`, `Insert`, `InsertMany`, `UpdateById`, `Update`, `UpdateMany`, `DeleteById`, `DeleteByIdList`, `Delete`, `DeleteMany` (each with the "not found throws" case), plus `FilterBuilder<T>` filters.
- `Fixtures/PostgresFixture.cs`: hard-coded `Host=localhost;Port=5432;...Password=<local dev default>`, migrates and `TRUNCATE`s a table; `Fixtures/MongoFixture.cs`: Mongo2Go (an embedded mongod binary). `Extensions.cs`: `ShouldBeEquivalentToWithMilliseconds` (DB vs CLR DateTime precision).
- `Repositories/IntegrationTests.cs`: two placeholder tests asserting `true.Should().BeTrue()`.
- Checked-in EF `Migrations/` and `migrations_macos.txt`.

**Key defect: nothing ran in CI.** Every test class is marked `[Trait("category","integration")]`, and every workflow runs `dotnet test --filter "category!=integration"` (`net-core-build-and-test.yml`, `deploy-global-nuget.yml`, `analyze-codecov.yml`, Dockerfile). So CI executed zero tests, and the codecov job measured zero coverage of real code. The "unit" tests needed a local Postgres on 5432 and had a hard-coded password. The name `Unit` was wrong: they were integration tests.

**Where it is now.** (b) merged into: `test/HsnSoft.Base.EntityFrameworkCore.Tests/EfCoreRepositoryTests.cs` and `EfCoreProjectionTests.cs` (unit, no container), `test/HsnSoft.Base.MongoDB.Tests`, and the Testcontainers-backed `test/HsnSoft.Base.IntegrationTests/Data/PostgreSqlRepositoryTests.cs`, `MySqlRepositoryTests.cs`, `MongoRepositoryTests.cs` (plus `TransactionTests`, `StartupTests`, inbox tests, `RelationalTests`, `MongoTests`). Provider list is wider (MySql, SqlServer, Kafka, RabbitMQ, MinIO, Azurite, Redis). Not line-by-line verified that each of the 69 old cases has a twin; the old case list is a good checklist (see Keep).

**Why.** ADR-0009 (unit project per package), ADR-0010 (Testcontainers), ADR-0011 (99 % merged coverage gate), ADR-0110 (shared container per service, test owns its data). Commit history: `8d545610 test: restructure test projects, add integration project and CI coverage gate`; `0c82c90e chore: start the rewrite from Base and Core only`; `14c05314 chore: remove test and samples folders before the rewrite`.

**Verdict.** Sound test content (a good provider-parity matrix), flawed infrastructure: wrong naming, hard-coded local database and credentials, a trait filter that silently disabled the suite, Mongo2Go (an unmaintained-binary approach, differs from the real server), unused packages. Superseded by a better strategy.

**Gap.** Already covered. None.

**Keep and avoid.** Keep: the same-case-list-per-provider idea (parity matrix between EF and Mongo) and the `ShouldBeEquivalentToWithMilliseconds` helper concept (DB precision truncation; check `test/` for an equivalent when adding a provider). Avoid: category traits as the only selector (the new project-name selector in `scripts/run-tests.sh` cannot be forgotten); hard-coded connection strings; placeholder assertions; testing a repository only against InMemory.

### 1.2 `HsnSoft.Base.Test.Api` (web host, `Microsoft.NET.Sdk.Web`)

**What it was.** A manual playground API (not an automated test). `Startup.cs`: `AddBaseAspNetCoreContextCollection`, controllers with `JsonStringEnumConverter`, Swashbuckle, `AddServiceApplicationConfiguration`, EF and Mongo registrations, `FakeUserMiddleware` (fake authenticated user), Swagger UI in Development. `Controllers/EfCoreUsersController`, `MongoUsersController`; `EventHandlers/ForgotPasswordMailSendEtoHandler` (an event-bus handler); four `Workers` (`EfCoreWriterWorker`, `EfCoreCleanerWorker`, `MongoWriterWorker`, `MongoCleanerWorker`: load generators, hosted-service registration commented out in `Startup`); `SeedData`, `.http` file. References only `Test.Api.Hosting/Application/EfCore/MongoDb`.

**Where it is now.** (b) the role of "prove the framework works end to end" moved to `samples/abc`, `samples/edx` and `samples/hsx` (own solution, own ADRs, `samples/check.sh`). Not a framework test any more.

**Why.** ADR (commit `517373bb chore: the samples are self-contained`); the old project was removed with the rewrite (`14c05314`). Reason inferred: it was never wired into CI.

**Verdict.** Sound as a developer playground, but had no assertions; performance workers were the only load tooling, and they were off.

**Gap.** Load generation (writer/cleaner workers) is not present as a tool. Low priority: if repository performance needs measuring, a `samples/` worker or BenchmarkDotNet project is a better place than a test project. Otherwise already covered.

**Keep and avoid.** Keep: the writer/cleaner worker idea for soak tests of EF vs Mongo repositories. Avoid: a fake-user middleware living in a project that looks like production code; Swashbuckle forced via `Test.Api.Hosting`.

### 1.3 `Test.Api.Application`, `.Domain`, `.EfCore`, `.MongoDb`

**What they were.** A four-layer slice of a demo service built on the old framework: `Domain` (`User` entity, `IEfCoreUserRepository`, `IMongoUserRepository`, `NormalizedRequestStates`, `DomainAssemblyMarker`) referencing `Ddd.Domain`, `AutoMapper` (forced) and `Test.Api.Helper`; `Application` (`EfCoreUserService`, `MongoUserService`, `UserDto`, `PagingFilterDto`, `ForgotPasswordMailSendEto`, `ApplicationAutoMapperProfile`) referencing `Authorization`, `Ddd.Application`; `EfCore` (`AppEfCoreDbContext`, `UserConfiguration`, `ContentServiceDbContextFactory`, a migration) referencing `EntityFrameworkCore.PostgreSql`; `MongoDb` (`AppMongoDbContext`, `EntityClassMap`, `UserClassMap`, `MongoSettings`) referencing `MongoDB`. All net10.0, `IsPackable=false`.

**Where it is now.** (b) same layering exists in samples (`abc`, `edx`, `hsx` have Domain/Application/Infrastructure style slices; not re-verified file by file). Note that `Domain -> AutoMapper` in the old demo was a consequence of the old `Ddd.Domain -> AutoMapper` package edge.

**Why.** Rewrite; ADR-0003 (abstractions separate from providers) and the sample structure.

**Verdict.** Over-structured for what it demonstrated (a User CRUD), and it showed the old coupling (Domain tied to AutoMapper).

**Gap.** None. **Keep and avoid.** Keep the idea of one entity served by both EF and Mongo repositories in a sample to show provider swappability. Avoid: AutoMapper in the domain layer.

### 1.4 `Test.Api.Helper` and `Test.Api.Hosting`

**What they were.** `Helper`: `Class1.cs` (empty), references `Core`. `Hosting`: `Class1.cs` (`public class Class1;`), references `AspNetCore`, `Serilog`, `Helper` and `Swashbuckle.AspNetCore 10.2.3`. Scaffolding with no behaviour.

**Where now.** (c) dropped. **Why.** Inferred: never filled in. **Verdict.** Flawed (dead code in the solution). **Gap/Keep.** None. Avoid committing template stubs.

### 1.5 What the new test strategy covers, and what it no longer covers

Covers (better): one `test/<Package>.Tests` per package (ADR-0009, enforced by `ci-check-test-project-exist.sh`), the `HsnSoft.Base.IntegrationTests` project against real services (Postgres, MySql, MsSql, Mongo, Redis, RabbitMQ, Kafka, MinIO via Chainguard image, Azurite), 99 % merged line-coverage gate on `src`, test isolation per ADR-0110, tests discovered by project name, `InternalsVisibleTo` for `<Project>.Tests`, test time/count summary.

No longer covers or does differently:
- Mongo2Go (embedded mongod) is gone; Mongo is tested against the real container. Gain in fidelity, cost in needing Docker (fine, the detector has `--skip-integration`).
- No in-repo manual playground API; samples are the end-to-end proof, but samples have their own tests/checks (`samples/check.sh`), outside the framework coverage gate (memory: sample tests live in samples).
- No load/performance tooling (old workers were load generators). Not replaced.
- No benchmark tests anywhere (verify; not found in `test/` listing). Motto says "performant"; there is no measured evidence. Medium-low priority gap (see open questions).

---

## 2. Repository-level files: old versus new

| Old file | What it did | New counterpart | Lost / improved |
|---|---|---|---|
| `common.props` | LangVersion latest, `Version 2026.10.40` (hand-edited), MS package versions inline (10.0.5), `NoWarn CS1591;CS1998;CS0436`, doc file, pdb in package, LGPL, icon, `Copyright 2023` | `common.props` + `common.versions.props` | Improved: `Nullable enable`, `Deterministic`, `ContinuousIntegrationBuild` on CI, `TreatWarningsAsErrors` on CI (with a narrow `WarningsNotAsErrors` for obsolete and NuGet advisories), `EnforceCodeStyleInBuild`, `AnalysisLevel latest-recommended`, `PublishRepositoryUrl`, `EmbedUntrackedSources`, `InternalsVisibleTo $(MSBuildProjectName).Tests`, version base `2026.10` + release-script number (ADR-0115) instead of a hand-edited full version. Lost: nothing needed. CS1998 (async without await) and CS0436 are no longer silenced, which is stricter. |
| `common.test.props` | LangVersion, NoWarn, a few Generate*=false | `common.test.props` | Improved: owns the whole test stack (xunit, runner, coverlet, Test.Sdk), `IsTestProject`, nullable, analyzers, shared host packages. Old test csprojs each pinned their own package versions. |
| `configureawait.props` | `ConfigureAwait.Fody 3.4.1` + `Fody 6.9.3` imported by every one of the 46 `src` projects (`FodyWeavers.xml` per project) | none | IL weaving at build time removed; replaced by explicit `ConfigureAwait(false)` (307 occurrences in new `src`; old src had only 38 explicit calls because Fody did it). See section 3 for the risk. |
| `Dockerfile` | one image that restores, builds, tests (with the broken filter), packs with `-dev.<run>` suffix and pushes to GitHub Packages; secrets via `--mount=type=secret` (good) but then written to files `./nuget_secret`, `./version_number` in a layer; copies only `common.props`, `configureawait.props` | CI workflow + `deploy-global-nuget.yml` + `scripts/pack-local.sh` | Improved: build once, artifacts shared across jobs, no secret in image layers. Lost: automatic dev-feed publish to GitHub Packages (`publish-github-nuget.yml`, triggered on branch `DEVELOPMENT`). Low priority; `pack-local.sh` covers local consumption. |
| `.github/workflows/net-core-build-and-test.yml` | PR to `dev`: restore, build, test with the broken filter | `ci-control-workflow.yml` | Improved: rules, format, Release build with warnings as errors, unit then integration tests, coverage gate 99 %, summary job, badges branch, Node 24 actions (ADR-0111), weekly schedule, release branches on push (ADR-0113, 0116). |
| `analyze-codecov.yml` | codecov upload; used `dotnet-version: 9.0.x` while projects target net10.0 (it could not even build); same broken filter | coverage gate (`ci-check-coverage.py`) + badges (ADR-0114) | Improved. Lost: Codecov.io hosted trend graph (README badge was codecov). Replaced by own badge data on `badges` branch. |
| `analyze-codeql.yml` | CodeQL on branch `RELEASE` (legacy name) and weekly cron | none found in `.github/workflows/` | **Lost.** Static security analysis (SAST) is not in the new CI. Motto says "secure". Medium: re-add CodeQL as a scheduled/release job (needs an ADR). |
| `analyze-sonarqube.yml` | SonarCloud on `RELEASE`; `dotnet-version: 9.0.x` (wrong SDK) | `scripts/sonar/analysis_{mac.sh,win.ps1}` + `docker/sonarqube.compose.yml` (local only; README says CI does not use them) | Cloud SonarQube analysis dropped on purpose (inferred: replaced by the coverage gate and analyzers). |
| `deploy-global-nuget.yml` | push to `rel/nuget`: build, test (broken filter), pack, push to nuget.org | `deploy-global-nuget.yml` (rewritten; `rel/nuget-candidate`, `rel/nuget-regular`) + `scripts/versioning/next-version.py` + `Releasing.md` | Improved: candidate/stable, computed version, tests. Maintainer-only (ADR-0132). |
| `publish-github-nuget.yml` | `docker build` for branch `DEVELOPMENT` using `kzrnm/get-net-sdk-project-versions-action` to read the version | none | Lost on purpose probably; Low. |
| `analysis_sonar.ps1` | local Sonar run for Windows only; **contains a hard-coded Sonar token `sqp_…(redacted)` (twice) and project key `data-project`, in git history on `dev-backup`** | `scripts/sonar/*` (mac + win, token from environment per its README) | Improved. **Security pitfall: that token should be considered leaked and revoked** (it is in history; only matters if the SonarQube instance was ever reachable). |
| `clear_bin_obj.ps1` | Windows-only bin/obj delete | `scripts/cleaner/cleaner_{mac.sh,win.ps1}` (never deletes tracked/source files, logs list) | Improved. |
| `NuGet.Config` | nuget.org only | same file present | Same. |
| `CONTRIBUTING.md` / `README.md` | CONTRIBUTING contains one word; README has 6 badges and one marketing paragraph (8 lines) | CONTRIBUTING, README, `docs/Overview.md`, 18 reference docs, 130+ ADRs, `.claude/rules` and skills | Improved by orders of magnitude. |
| *(did not exist)* | no `docs/`, no ADRs, no hooks | `.husky` (format on commit, build + unit tests on push, ADR-0013), `scripts/ci-detector/` (local CI mirror, ADR-0014), `ci-force-csproj-order.py` (ADR-0043), `ci-force-framework-references-rules.py` (ADR-0075, mechanically forbids a repeated sibling-transitive reference), `ci-check-test-project-exist.sh`, `format-json.cs`, `.config/dotnet-tools.json`, `test/coverlet.runsettings`, `samples/` | New. The dependency-graph rule is now machine-checked; in the old repo it was not (hence section 4). |

Also lost: old `.dockerignore` (no Docker build now, fine). Old `.editorconfig` present in both (192 lines old; the new one is the authority, ADR-0008).

---

## 3. Cross-cutting rules: enforced or ignored

| Topic | Old | New | Comment |
|---|---|---|---|
| ConfigureAwait | `ConfigureAwait.Fody` weaver in all 46 projects; only 38 explicit calls in code | explicit `ConfigureAwait(false)` (307 calls); rule in ADR-0008 and `.claude/rules/dotnet.md` | Fody is a build-time IL weaver (extra build dependency, `PrivateAssets=All`, slows builds, not compatible with every SDK change) but it was impossible to forget. New: **no analyzer enforcement found** (no `CA2007` entry in `.editorconfig`, `common.props` or csproj files) so it is by convention and review. Recommendation: enable `dotnet_diagnostic.CA2007.severity = warning` for `src` only (not tests, not samples). Low-Medium. |
| Nullable | not enabled anywhere (no `Nullable` in any old csproj or props) | `Nullable enable` in `common.props` and `common.test.props` | Big improvement for a security/robustness-minded framework. |
| Analyzers | `.editorconfig` only (IDE-time); no `EnforceCodeStyleInBuild`, no `AnalysisLevel`; `NoWarn CS1591;CS1998;CS0436` hid async-without-await and type conflicts | `EnforceCodeStyleInBuild`, `AnalysisLevel latest-recommended`, warnings as errors on CI, format check in CI, pre-commit formatter | Improved. |
| Package versioning | `Version` hand-edited in `common.props` (`2026.10.40`); CI added `-dev.<run_number>`; MS package version props `10.0.5` in the same file; many other packages pinned in each csproj (e.g. `System.Runtime.Loader 4.3.0`, `System.Linq.Queryable 4.3.0`, `System.Linq.Expressions 4.3.0` from the .NET Core 1 era referenced in a net10 project) | `BaseVersion 2026.10` + number made by `scripts/versioning/next-version.py` (ADR-0115) with tests; shared versions in `common.versions.props`; `Directory.Packages.props` in samples | Improved. Check: third-party package versions still live in each csproj in the framework (not central package management); only the two Microsoft versions are centralised. Low-Medium: consider `Directory.Packages.props` for `src` as well (open question). |
| Target frameworks | all `net10.0`, single target; `AssetTargetFallback portable-net45+win8+wp8+wpa81` in Core | `net10.0` only, set once in `common.props` | Same single-target policy; the legacy fallback is gone. CI used SDK 9.0.x in two old workflows (codecov, sonarqube) which cannot build net10.0. |
| SourceLink / symbols | `RepositoryUrl`, pdb copied into the nupkg (`AllowedOutputExtensionsInPackageBuildOutputFolder`), but no `PublishRepositoryUrl`, no `EmbedUntrackedSources`, no `Deterministic`/`ContinuousIntegrationBuild`, no `.snupkg`; `GenerateEmbeddedFilesManifest` set for every project (pointless without embedded files) | `PublishRepositoryUrl`, `EmbedUntrackedSources`, `Deterministic`, `ContinuousIntegrationBuild` on CI, pdb in nupkg (Microsoft.SourceLink.GitHub is part of the .NET SDK since 8, so source linking works) | Improved. Not found: `IncludeSymbols`/`SymbolPackageFormat snupkg`; pdb is inside the nupkg instead, which is acceptable but larger. Verify once with a pack. |
| NuGet metadata | Authors, Product, Copyright `2023`, LGPL-3.0-only, icon, project URL; `PackageReadmeFile` set only in `Core` and the `HsnSoft.Base` name package (2 of 46) | same metadata (`Copyright (c) HsnSoft`, `Company`), icon, license; `PackageReadmeFile` per project not checked here (belongs to other clusters) | Mostly same. Old: 44 of 46 packages had no readme on nuget.org. |
| Doc XML | `GenerateDocumentationFile` true, CS1591 silenced | same | Same. |
| Static state / hidden I/O | not measured here (other clusters) | n/a | n/a |
| Test hygiene | traits filter disabled the suite; tests need local Postgres | project-name selection, Testcontainers | Improved. |
| Secrets | Sonar token in script (history), Dockerfile writes secrets into files inside build stage | environment/secrets only; CLAUDE.md forbids credentials | Improved. |

---

## 4. Dependency-graph critique of the OLD framework

Method: parsed all 46 `src/*/*.csproj` on `dev-backup` (`ProjectReference` and `PackageReference`) and computed transitive closures (script `g.py`).

### 4.1 Facts
- **No cycles** (the old graph was a DAG). The one near-cycle shape is `Ddd.Domain -> AutoMapper -> Auditing/ObjectMapping`, and `AspNetCore.Mvc -> AspNetCore`, but none closed.
- Redundant (transitively implied) direct project references, i.e. ADR-0075 violations by today's rule: `AspNetCore` (VirtualFileSystem, Security), `AspNetCore.Mvc` (Security, Localization), `AspNetCore.Mvc.UI` (UI.Navigation, AutoMapper), `Auditing` (Timing, Security, MultiTenancy, Data), `Authorization` (Localization, Security), `Ddd.Application` (Security, Localization, ObjectMapping, Auditing), `Ddd.Domain` (Timing, MultiTenancy, Data), `EventBus` (MultiTenancy), `Http.Client` (MultiTenancy, ExceptionHandling, Validation), `Swashbuckle` (VirtualFileSystem), `Timing` (Security), plus `Json` redundancy in `EntityFrameworkCore`, `MongoDB`, `Caching.StackExchangeRedis`.
- No project referenced a provider from an abstraction, and the provider pattern (`EventBus` + `EventBus.RabbitMQ/Kafka/Azure`, `BlobStoring` + 4 providers, `EntityFrameworkCore` + 4 providers, `Json.Abstractions` + two implementations) was right.

### 4.2 Fat dependencies and forced packages (what a consumer paid)

**`HsnSoft.Base.Core` was fat**: 21 NuGet packages for the "core": `Microsoft.Extensions.Localization`, `Logging.Console`, `Configuration.EnvironmentVariables/UserSecrets/CommandLine`, `Options.ConfigurationExtensions`, `Hosting.Abstractions`, `System.Text.Encodings.Web`, `System.Linq.Dynamic.Core`, `JetBrains.Annotations`, `Nito.AsyncEx.Coordination` + `Context`, `System.ComponentModel.Annotations 5.0.0`, and legacy `System.Runtime.Loader/Linq.Queryable/Linq.Expressions 4.3.0`. Every package in the framework inherited all 21 (min 21 packages for the simplest leaf: `Security`, `Data`, `ObjectMapping`, `BlobStoring`). New Core: 2 packages (`DependencyInjection.Abstractions`, `Logging.Abstractions`).

Forced packages (consumer uses package X, gets Y):

| Consumer takes | Is forced to take | Via | Motto violation |
|---|---|---|---|
| `EventBus` (abstraction) | `Serilog` + 8 Serilog sinks/helpers (Console, File, Async, Graylog, Compact, Destructurama.Attributed, Destructurama.JsonNet), `Newtonsoft.Json` + `Newtonsoft.Json.Schema`, `TimeZoneConverter`, `Microsoft.Extensions.Caching.Memory`, FileProviders.*, Localization. 39 packages in closure | `EventBus -> Serilog`, `-> Json -> Json.Newtonsoft`, `-> MultiTenancy -> Security`, `Json -> Timing -> Localization -> VirtualFileSystem` | Yes. Code evidence: `EventBus/Logging/EventBusLogger.cs` uses Serilog. A bus abstraction must not pick a logger. |
| `Ddd.Domain` | `AutoMapper 14` (a licensed library since v15, a commercial-license risk), Serilog, Newtonsoft.Json, EventBus, Auditing, ExceptionHandling, Localization, VirtualFileSystem. 17 projects, 40 packages | `Ddd.Domain -> AutoMapper`; code: `Domain/Repositories/IReadOnlyGenericRepository.cs`, `GenericRepositoryBase.cs` use AutoMapper | Yes, the worst one. Entities/repositories forced a mapper. |
| `EntityFrameworkCore.<Provider>`, `MongoDB`, `Caching.StackExchangeRedis` | all of `Ddd.Domain`: AutoMapper, Serilog, Newtonsoft, EventBus, plus `EFCore.BulkExtensions` (EF) or `LinqKit` (Mongo). Redis cache referenced `Ddd.Domain` (!) for nothing but entity caching | closure of 18-19 projects, 42-44 packages | Yes. A Redis cache package pulled the domain, the event bus and AutoMapper. |
| `EventBus.RabbitMQ` | Newtonsoft.Json(+Schema), Serilog stack, TimeZoneConverter, Polly, HealthChecks | via `EventBus` | Yes |
| `Json` (meta) | **both** `System.Text.Json` and `Newtonsoft.Json` + `Newtonsoft.Json.Schema` + DeepCloner | `Json -> Json.SystemTextJson` and `-> Json.Newtonsoft` | Yes: the meta package defeated the point of the `Json.Abstractions` split. `Auditing`, `EventBus`, `EntityFrameworkCore`, `MongoDB`, `Caching` all took `Json` (not `Json.Abstractions`), so Newtonsoft was everywhere. |
| `Auditing` | Newtonsoft via `Json`, `Timing` -> `TimeZoneConverter`, `MultiTenancy` | | Yes: auditing forced multi-tenancy (also opt-in per ADR-0074 today) |
| `Timing` | `MultiTenancy`, `Localization`, `Security`, `TimeZoneConverter` | a clock/timezone package forced tenancy and localization | Yes |
| `Authorization.Abstractions` | `Microsoft.AspNetCore.Authorization` (ASP.NET Core), `Localization`, `MultiTenancy` | an "abstractions" package depending on the web framework | Yes |
| `AspNetCore` | `Serilog.AspNetCore`, `JwtBearer`, `System.IdentityModel.Tokens.Jwt`, Auditing, Authorization, ExceptionHandling, Validation, VirtualFileSystem (17 projects, 43 packages) | one web package bundled logging, JWT, audit, authorization | Yes (the "kitchen-sink" web package). |
| `AspNetCore.Mvc` | `Microsoft.AspNetCore.Mvc.Versioning 5.1.0` (a **deprecated** package, last release 2019-2020 line), `Razor.RuntimeCompilation`, `Ddd.Application`, `UI.Navigation` | | Yes, and a dead dependency. |
| `Swashbuckle` | whole Mvc + AspNetCore stack (26 projects, 48 packages) | | Yes |
| `Http.Client` | `AspNetCore.Mvc` (!), `IdentityModel` | a client package depended on the server-side MVC package | Yes (26 projects) |
| `AutoMapper` | `Auditing` (and with it Json -> Newtonsoft...) | | Yes; `Ddd.Domain` forced it |
| `BlobStoring.FileSystem` | `Polly` | | Minor |
| `BlobStoring.Azure` | explicit `System.Text.Json` package with MS version for no clear reason | | Minor |
| `Localization` | `VirtualFileSystem` -> three FileProviders packages; `Caching.Memory` | | Moderate |
| `Serilog` | 10 packages incl. Graylog sink and Destructurama.JsonNet (which in turn needs Newtonsoft) | a logger pulled a second JSON stack | Yes |
| `PuppeTeer` (sic) | `PuppeteerSharp 25.3.3` | typo in the project name; only Core | Fine on references |

Per-package "transitive packages" (old): leaf `Security` 21; `Timing` 26; `Ddd.Domain` 40; `AspNetCore` 43; `EventBus` 39; `EntityFrameworkCore.PostgreSql` 44; `Swashbuckle` 48. New (same metric): `Core` 2; `EventBus` 3; `AspNetCore` 2; `Ddd.Domain` 4; `EntityFrameworkCore.PostgreSql` 9 (of which EF Core + Npgsql are the point); `MongoDB` 7; `EventBus.RabbitMQ` 7; max 10.

### 4.3 Hidden coupling and other graph flaws
- Sibling references that were not needed (list in 4.1): 21 redundant direct edges. Today `ci-force-framework-references-rules.py` rejects them.
- "Abstraction" packages referencing implementations: `Ddd.Domain -> AutoMapper`, `EventBus -> Serilog`, `Json -> Json.Newtonsoft`, `Authorization.Abstractions -> Microsoft.AspNetCore.Authorization`.
- Wrong direction: `Http.Client -> AspNetCore.Mvc`; `Caching.StackExchangeRedis -> Ddd.Domain`; `Timing -> MultiTenancy`; `AspNetCore.Mvc -> Ddd.Application`; `UI -> ExceptionHandling`.
- Name hygiene: `PuppeTeer` typo; `HsnSoft.Base` package contains only a Core reference (name-holder; the new repo keeps this deliberately).
- Single-purpose micro projects (`Data`, `Security`, `ObjectMapping`, `Json.Abstractions`...) were merged in the new graph (`513d8411 refactor: fold single-purpose abstraction projects into their host projects`, ADR-0002 / ADR-0003 / ADR-0004).
- Provider mixing is not present in the old graph (good).

### 4.4 The new graph (working tree)
- 37 projects, no cycles, **no redundant direct edge** (computed: `redundant=[]` for every project).
- `Core` has 0 project and 2 package references; every package reaches Core exactly once (ADR-0004, 0075).
- Abstractions-only closures: `EventBus` -> Core; `AspNetCore` -> Core (no JWT, no Serilog: split to `Authentication`, `AspNetCore.WebApi`, `AspNetCore.Swashbuckle`, `AspNetCore.SwaggerGateway`); `Logging` (abstraction) separate from `Logging.Serilog` and `Logging.NLog` (user choice); `Json` dropped entirely (System.Text.Json is part of the platform; Newtonsoft remains only in tests, `NewtonsoftParentCopyTests`); AutoMapper dropped from the framework; tenancy is a leaf (`MultiTenancy`) with glue packages `MultiTenancy.EntityFrameworkCore/MongoDB/EventBus/Authentication` so that EF or Mongo does not force tenancy.
- Remaining critique of the new graph: (1) `EntityFrameworkCore` and `MongoDB` reference `Ddd.Domain`, which brings `Auditing` + `Data` + `Core` (acceptable, they are repository implementations of the domain abstractions; check that `Auditing` need not force `Microsoft.Extensions.DependencyInjection.Abstractions` duplicates). (2) `EventBus.EntityFrameworkCore` references `Hosting` as a sibling (outbox needs the hosted-service base; fine). (3) `Logging.Serilog` depends on `Hosting` and `Logging` and takes 4 Serilog sinks (Console, File, Graylog) always, so a Serilog user who only wants Console gets the Graylog sink (Medium-low; a sink-per-package split or optional would honour the motto more; not decided here). (4) `Caching.StackExchangeRedis` brings `Microsoft.Extensions.Diagnostics.HealthChecks` (needed for its health check). (5) Third-party versions are still per-csproj (no central package management in `src`).
- Conclusion: the old graph violated the motto in at least these places: Core 21 packages; EventBus->Serilog; Json meta pulling Newtonsoft; Ddd.Domain->AutoMapper; Timing/Auditing->MultiTenancy; Authorization.Abstractions->ASP.NET Core; AspNetCore bundling JWT and Serilog.AspNetCore; Http.Client->Mvc; Redis cache->Ddd.Domain. All are removed in the new graph.

---

## Open questions for the maintainer

1. **CodeQL / SAST.** The old repo had CodeQL; the new CI has none. Should a scheduled CodeQL (or equivalent) job be added (needs a new ADR)? Motto includes "secure".
2. **Revoke the old Sonar token.** `sqp_…(redacted)` is in `dev-backup` history (`analysis_sonar.ps1`). Confirm it is revoked and decide whether `dev-backup` should be rewritten or left as an archive.
3. **ConfigureAwait enforcement.** Replace Fody with an analyzer rule (`CA2007` as a warning for `src` only), or keep it purely as a convention?
4. **Central package management.** Move third-party versions of `src` into `Directory.Packages.props` (samples already use one), or keep per-csproj versions plus `common.versions.props`?
5. **Performance evidence.** Add a BenchmarkDotNet project (repository CRUD, event envelope serialization, tenant filter) so "performant" is measured? The old writer/cleaner workers were the only load tool and were disabled.
6. **Serilog sinks.** Keep Console/File/Graylog bundled in `Logging.Serilog`, or split sinks so a consumer does not take Graylog it does not use?
7. **Dev feed.** Is the old automatic dev-package publish (`-dev.<run>` to GitHub Packages) still wanted for the samples/hhs/Hsx consumption, or is `pack-local.sh` enough?
8. **Parity matrix.** Want a one-time check that each of the 69 old repository cases (list in 1.1) has a twin in `IntegrationTests/Data/*RepositoryTests` for Postgres, MySql, Mongo (I did not diff them case by case)?
