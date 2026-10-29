# ADR-0009: Every package has a unit test project, found by name

- **Status:** Accepted
- **Date:** 2026-10-22

## Context

A new package can slip in untested, and test kinds selected by traits or categories are forgotten (a test without the trait never runs). We wanted a rule that cannot be bypassed by
forgetting something, and a fast test loop that needs no external service.

## Decision

- **Every package under `src/` has `test/<Package>.Tests/`** (it imports `common.test.props` and can see the internals of its package). `scripts/ci-detector/ci-check-test-project-exist.sh` fails CI (and the
  detector) when one is missing. The only exceptions are listed in the script with the reason: `HsnSoft.Base` (a name holder that only references Core) and `HsnSoft.Base.BlobStoring` (a single
  interface; its implementations are tested in the provider projects).
- **Test kinds are told by project name, not by traits:** `*.Tests.csproj` are unit tests, `*.IntegrationTests.csproj` are integration tests (see [ADR-0010](0010-integration-tests-with-testcontainers.md)).
  `scripts/run-tests.sh unit|integration` finds them by that pattern. Having no unit test project is an error; having no integration project is fine.
- **Unit tests need no external service** and are fast; they run in the pre-push hook (see [ADR-0013](0013-git-hooks.md)) and in the first CI job.
- **How they are written:** against the public API plus the internals the project exposes; fakes for the logger (a `ListLogger`/`ListLoggerFactory` that keeps entries, event names and scope properties) and for
  the network; a web api is tested through `TestServer` with the real pipeline. A flaky test is fixed so it is deterministic (it was
  reproduced under CPU load first), never retried or ignored. Names say what is expected.
- **A change comes with its tests**, including the removal of a feature (the tests of what was removed go too) and the boundary cases (an empty list, the default, the limit).

## Consequences

- A new package means a new test project in the same pull request and a row in the solution (see [ADR-0006](0006-repository-folders-and-the-solution-file.md)).
- Behaviour that only exists against a real broker or database is not unit-tested; it is covered by the integration tests, and the coverage gate counts both (see [ADR-0011](0011-coverage-gate.md)).

## Related

[ADR-0010](0010-integration-tests-with-testcontainers.md), [ADR-0011](0011-coverage-gate.md), `scripts/ci-detector/ci-check-test-project-exist.sh`, `scripts/run-tests.sh`.
