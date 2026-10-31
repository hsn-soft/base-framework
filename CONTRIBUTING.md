# Contributing

Thank you for helping with the HsnSoft Base Framework. This page is the short version of how we work; the root
[README](README.md) says what the framework is, and [docs/Overview.md](docs/Overview.md) explains what the packages are.

## Ground rules

- **One package, one capability.** An application takes only the packages it needs. Do not add a dependency "because it is
  convenient": a package must not pull in anything its users do not need.
- **Abstractions are separate from providers.** `EventBus` is the contract, `EventBus.RabbitMQ` and `EventBus.Kafka` are providers;
  `BlobStoring` and `BlobStoring.Minio` likewise. A provider references its abstraction, never the other way round.
- **Do not split without a reason.** Add a package only when a real application would use it without the rest. Code that only one
  product needs belongs in that product's own shared project, not here.
- **The framework knows no product.** No product names, no product settings; defaults must be neutral and overridable by
  configuration.
- **Reference `HsnSoft.Base.Core` once.** Every package depends on it, directly or through another framework package. If the
  package already references a sibling that references Core, do not add Core again.

- **Write down architecture decisions.** A rule other code must follow, or a choice between real alternatives, gets a short record in [`docs/adr`](docs/adr/README.md)
  (see [ADR-0001](docs/adr/0001-record-architecture-decisions.md)), in the same pull request as the change.

## Branches and pull requests

1. Branch from `dev` (`feat/...`, `fix/...`, `refactor/...`).
2. **Run the detector before you open the pull request**: `./scripts/ci-detector/ci_detector_mac.sh` (Windows:
   `./scripts/ci-detector/ci_detector_win.ps1`). It runs the CI's checks on your machine and shows in one table what would fail, so you
   do not discover it on the pull request. See [scripts/ci-detector](scripts/ci-detector/README.md).
3. Open the pull request against `dev`. The `.NETCore Build & Test` workflow must be green: build, unit tests, integration tests and
   the coverage gate.
4. `rel/nuget-candidate` (from `dev` only) and `rel/nuget-regular` (from `rel/nuget-candidate` only) are what is published to NuGet, a pre-release and a stable version; a merge into them runs the deploy workflow. Into `dev` only from `feat/*`, `hotfix/*` and `bugfix/*`. See [Releasing](docs/Releasing.md).

Commit messages follow the Conventional Commits style used in the history: `feat:`, `fix:`, `refactor:`, `test:`, `ci:`, `style:`,
`docs:`, with a short subject in the imperative.

## Building and testing

You need the .NET 10 SDK, and Docker for the integration tests.

```bash
dotnet build
./scripts/run-tests.sh unit          # fast, no Docker
./scripts/run-tests.sh integration   # starts RabbitMQ, Kafka, Redis, MongoDB, PostgreSQL, SQL Server, MinIO (Testcontainers)
```

- **Every package has a unit test project**, `test/<Package>.Tests`. `scripts/ci-detector/ci-check-test-project-exist.sh` fails the build when one is
  missing.
- **Every provider has integration tests** in `test/HsnSoft.Base.IntegrationTests`, against the real service.
- **Coverage:** CI merges the coverage of both kinds of tests and fails below 99 % of the lines of `src`. New code comes with tests;
  do not lower the threshold.
- Test names say what is expected (`MissingConnectionString_IsAnError`); one behaviour per test.

## Code style and formatting

The `.editorconfig` is the single source of truth for Visual Studio, Rider and VS Code alike: 4 spaces (2 for JSON), LF line
endings, UTF-8, a final newline, no trailing whitespace. Nobody configures a formatter by hand.

**Before every commit** a Git hook ([Husky.Net](https://alirezanet.github.io/Husky.Net/)) formats the staged `.cs` and `.json`
files, stages the result and refuses the commit if a rule it cannot fix automatically is still broken (for example a naming rule).
It is installed for you the first time you build or restore the solution (`dotnet tool restore` + `dotnet husky install`); to
install it by hand run those two commands in the repository root. A commit takes a few seconds longer because of it. Set `HUSKY=0`
to skip it in an emergency; CI runs the same checks and fails the pull request anyway.

**Before every push** a second hook builds the solution in `Release` with warnings as errors (as CI does) and runs the unit tests;
the push is refused if either fails, so a broken branch never reaches the remote. It takes about 30 seconds. The integration tests
are not part of it (they need Docker and take longer); CI runs them on the pull request. `HUSKY=0 git push` skips the hook when you
really must, CI will still fail the pull request.

To format everything yourself:

```bash
dotnet format whitespace HsnSoft.Base.sln
dotnet format style HsnSoft.Base.sln --severity info
dotnet run scripts/ci-detector/format-json.cs -- --all
```

Format on save is worth switching on, so the hook has nothing left to do:

| Editor | Setting |
| --- | --- |
| VS Code | Already set in `.vscode/settings.json`; install the recommended extensions (C# Dev Kit) when asked. |
| Visual Studio | *Tools > Options > Text Editor > Code Cleanup > Run Code Cleanup profile on Save*. It reads the `.editorconfig` by itself. |
| Rider | *Settings > Tools > Actions on Save > Reformat code* and *Optimize imports*. It reads the `.editorconfig` by itself. |

Rules to know:

- CI builds with warnings as errors: fix a warning instead of suppressing it. Unused usings, members and parameters are warnings too.
- Use explicit types instead of `var`, except where the type is obvious from the right-hand side (`new Foo()`).
- Private static fields start with `s_` (`s_defaultOptions`); constants are PascalCase.
- Library code: explicit `using` directives (no implicit usings), `ConfigureAwait(false)` on awaits, `ArgumentNullException.ThrowIfNull`
  on public entry points, logging through `[LoggerMessage]` source generators.
- Every public extension method that registers services has an XML doc comment saying what it registers and which configuration
  section it reads.
- The references of a project file are in groups, one `ItemGroup` each, from the top down: external (NuGet, `FrameworkReference`), framework (`HsnSoft.Base.*`), shared (`<Product>.Shared.*`), own (the other projects of the service or product),
  each in alphabetical order ([ADR-0043](docs/adr/0043-project-file-reference-order.md)). `python3 scripts/ci-detector/ci-force-csproj-order.py --fix` puts them in order; the CI and the detector check it. In `src/` every package reaches `HsnSoft.Base.Core` and does not list a reference a sibling already brings, `Core` included ([ADR-0075](docs/adr/0075-rules-of-the-framework-project-files.md), `scripts/ci-detector/ci-force-framework-references-rules.py`).
- Do not commit secrets, tokens or real connection strings.

## Adding a package

1. Create `src/HsnSoft.Base.<Name>/` with a project that imports `common.props` and follows the layout of an existing package
   (package id, description, tags, README).
2. Add `test/HsnSoft.Base.<Name>.Tests/` (imports `common.test.props`; it can see the package's internals). Package versions are never written in a project file: use `$(MicrosoftAspNetCorePackageVersion)` / `$(MicrosoftEntityFrameworkCorePackageVersion)` (`common.versions.props`, [ADR-0105](docs/adr/0105-shared-package-versions-in-one-file-and-a-common-test-host.md)).
3. For a provider (a broker, a database, a store), add integration tests to `test/HsnSoft.Base.IntegrationTests`.
4. Add both to the solution and add a row to the package table in [docs/Overview.md](docs/Overview.md).

## Trying a change in another service

`./scripts/pack-local.sh` packs the framework into `~/nuget-local-feed` with a `-local.<timestamp>` version, so a microservice can
reference the local build and be debugged against it. Details are at the top of the script.

## Other tools

| Folder | What it is |
| --- | --- |
| `scripts/sonar/` | Local SonarQube analysis for Linux/macOS and Windows |
| `scripts/ci-detector/` | Runs the CI's checks on your machine before you open a pull request |
| `scripts/cleaner/` | Deletes build output, test results and editor leftovers |
