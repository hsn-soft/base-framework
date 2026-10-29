# ADR-0007: Shared build settings, one version, and how a package is published

- **Status:** Accepted
- **Date:** 2026-10-22

## Context

Dozens of projects must build the same way, with the same strictness, and be released together. Settings copied into each project drift; a version per package makes a consistent set impossible to
pick; a warning that is only a warning on a developer's machine becomes a failure on the pull request.

## Decision

- **`common.props`** (libraries under `src/`) and **`common.test.props`** (tests under `test/`) hold the shared settings: `net10.0`, `LangVersion latest`, `Nullable enable`, deterministic builds, generated XML
  documentation, `EnforceCodeStyleInBuild`, `AnalysisLevel latest-recommended`, NuGet metadata (authors, license, icon, symbols in the package). A project imports one of them and adds only its id, description and tags.
  Tests also get `InternalsVisibleTo` for `<Project>.Tests` through `common.props`, so a unit test project can see the internals of its package.
- **Warnings are errors in CI** (`TreatWarningsAsErrors` when `GITHUB_ACTIONS` is true) and in the pre-push hook and the detector (`-p:TreatWarningsAsErrors=true`), so a warning that would break the pull
  request breaks the push. Kept as warnings even then, so a build does not break without a code change: `CS0618`/`CS0612` (use of an obsolete API after an upgrade) and `NU1901`-`NU1904` (a new vulnerability advisory).
- **One version for every package**, in `common.props` (`<Version>2026.10.40</Version>`: year.month.patch). All packages are released together; there is no per-package version.
- **Versions of shared dependencies** are properties in `common.props` (`MicrosoftAspNetCorePackageVersion`, `MicrosoftEntityFrameworkCorePackageVersion`) so a framework upgrade is one edit. The framework projects manage their own versions (no central package management).
- **Publishing:** pushing to `rel/nuget` runs `deploy-global-nuget.yml`: restore, a clean Release build, the unit tests, `dotnet pack`, `dotnet nuget push --skip-duplicate` with the secret `GLOBAL_NUGET_TOKEN`.
  Nobody opens a pull request against `rel/nuget`; `dev` is merged into it to release.
- **Trying a change in another service before release:** `scripts/pack-local.sh` packs into `~/nuget-local-feed` as the next patch with a `-local.<timestamp>` suffix; the `.pdb` files are inside the packages,
  so the consuming service can be debugged step by step.

## Consequences

- Raising the version is a deliberate edit of `common.props` in the pull request that releases.
- A new analyzer rule that is on by default can break the build after an SDK upgrade; the fix is in the code, not a suppression (see [ADR-0008](0008-code-style-and-analyzers.md)).
- The deploy runs only the unit tests; the integration tests and the coverage gate run on the pull request into `dev`, so only code that passed them reaches `rel/nuget`.

## Related

[ADR-0012](0012-ci-workflow-and-what-breaks-a-pull-request.md), `common.props`, `common.test.props`, `.github/workflows/deploy-global-nuget.yml`, `scripts/pack-local.sh`.
