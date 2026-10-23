# Contributing

Thank you for helping with the HsnSoft Base Framework. This page is the short version of how we work; the root
[README](README.md) explains what the packages are.

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

## Branches and pull requests

1. Branch from `dev` (`feat/...`, `fix/...`, `refactor/...`).
2. Open the pull request against `dev`. The `.NETCore Build & Test` workflow must be green: build, unit tests, integration tests and
   the coverage gate.
3. `rel/nuget` holds what is published to NuGet; pushing to it runs the deploy workflow. Do not open pull requests against it.

Commit messages follow the Conventional Commits style used in the history: `feat:`, `fix:`, `refactor:`, `test:`, `ci:`, `style:`,
`docs:`, with a short subject in the imperative.

## Building and testing

You need the .NET 10 SDK, and Docker for the integration tests.

```bash
dotnet build
./scripts/run-tests.sh unit          # fast, no Docker
./scripts/run-tests.sh integration   # starts RabbitMQ, Kafka, Redis, MongoDB, PostgreSQL, SQL Server, MinIO (Testcontainers)
```

- **Every package has a unit test project**, `test/<Package>.Tests`. `scripts/check-test-projects.sh` fails the build when one is
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

To format everything yourself:

```bash
dotnet format whitespace HsnSoft.Base.sln
dotnet format style HsnSoft.Base.sln --severity info
dotnet run scripts/format-json.cs -- --all
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
- Do not commit secrets, tokens or real connection strings. Sample values live only under `samples/`.

## Adding a package

1. Create `src/HsnSoft.Base.<Name>/` with a project that imports `common.props` and follows the layout of an existing package
   (package id, description, tags, README).
2. Add `test/HsnSoft.Base.<Name>.Tests/` (imports `common.test.props`; it can see the package's internals).
3. For a provider (a broker, a database, a store), add integration tests to `test/HsnSoft.Base.IntegrationTests`.
4. Add both to the solution, add a row to the package table in the root README, and, if it shows how products compose the
   framework, use it in a sample under `samples/`.

## Trying a change in another service

`./scripts/pack-local.sh` packs the framework into `~/nuget-local-feed` with a `-local.<timestamp>` version, so a microservice can
reference the local build and be debugged against it. Details are at the top of the script.

## Other tools

| Folder | What it is |
| --- | --- |
| `scripts/sonar/` | Local SonarQube analysis for Linux/macOS and Windows |
| `scripts/clean/` | Deletes build output, test results and editor leftovers |
| `samples/` | Example systems built only from the framework packages; see `samples/README.md` |
