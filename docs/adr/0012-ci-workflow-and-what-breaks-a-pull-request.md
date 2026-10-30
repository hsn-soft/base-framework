# ADR-0012: The CI workflow, and what breaks a pull request

- **Status:** Accepted
- **Date:** 2026-10-22

## Context

A failing pull request costs a round trip (push, wait, read the log, fix, push). We wanted CI to check everything that matters, in an order that fails fast, with the same steps a developer can run locally,
and we wanted to know what exactly can break a pull request.

## Decision

`.github/workflows/ci-control-workflow.yml` (`.NETCore Build & Test`) runs on pull requests into `dev` (opened, synchronized, reopened), on pushes to `dev` (the state of `dev` is what the README badge shows) and by hand.
A new push to a pull request cancels its previous run; pushes to `dev` always finish. Three jobs:

1. **Build And Unit Test** (Ubuntu): every package has a unit test project (`ci-check-test-project-exist.sh`) -> restore -> **format check C#** (`dotnet format whitespace` and `style --verify-no-changes --severity info`) ->
   **format check JSON** (`format-json.cs --all --check`) -> **build Release, warnings are errors** -> unit tests with coverage. The test results are uploaded.
2. **Integration Test** (Ubuntu, Docker): `run-tests.sh integration` with coverage.
3. **Coverage Gate** (needs both): merges the coverage and fails below `COVERAGE_THRESHOLD` (see [ADR-0011](0011-coverage-gate.md)).

**What breaks a pull request**, so none of them is a surprise:

| Break | Cause | Where it is also caught earlier |
|---|---|---|
| A package without `test/<Package>.Tests` | a new package without tests | detector |
| Format (C#) or Format (JSON) | a file the pre-commit hook would have fixed (hook skipped, not installed, `--no-verify`) | pre-commit, detector |
| Build | any compiler or analyzer **warning** (warnings are errors), an unused using, a missing XML comment on a public member that registers services | pre-push, detector |
| Unit tests | a failing or flaky test | pre-push, detector |
| Integration tests | a provider that no longer works against the real service | detector |
| Coverage gate | merged line coverage of `src` under 99 % | detector |

- **Linux is the runner.** Differences from macOS (path case, line endings, Docker) show up first on the pull request; line endings are LF everywhere by `.editorconfig` and `.gitattributes`.
- The workflow and the detector are kept **in step**: a change to a step in one is made in the other in the same pull request (see [ADR-0014](0014-detector-before-a-pull-request.md)).
- Pull requests go against `dev`, branched from `dev` (`feat/...`, `fix/...`, `refactor/...`), with Conventional Commit messages (`feat:`, `fix:`, `refactor:`, `test:`, `ci:`, `style:`, `docs:`).
  `rel/nuget` holds what is published (see [ADR-0007](0007-build-settings-and-versioning.md)).

## Consequences

- A warning left in the code fails the pull request even if the code works; that is intended.
- Adding a CI step means adding it to the detector and to this table.

## Related

[ADR-0011](0011-coverage-gate.md), [ADR-0013](0013-git-hooks.md), [ADR-0014](0014-detector-before-a-pull-request.md), `.github/workflows/`, `CONTRIBUTING.md` (Branches and pull requests).
