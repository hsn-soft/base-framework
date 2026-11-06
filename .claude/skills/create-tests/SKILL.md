---
name: create-tests
description: "Use when adding or improving unit tests or integration tests for HsnSoft.Base behavior, providers, regressions, boundaries, or failure paths."
---

# Create tests

## Choose the test kind

- Write unit tests for deterministic behavior, public contracts, validation, boundary conditions, errors, and regressions. Unit tests must not require Docker or live infrastructure; use the repository's existing fakes and fixtures.
- Write integration tests when behavior crosses a real broker, database, cache, or blob-storage boundary. Provider integration coverage is required and must exercise the real service through `test/HsnSoft.Base.IntegrationTests` and Testcontainers.
- A coverage percentage is only a gate. Assert meaningful behavior and expected outcomes; do not add tests solely to execute lines.
- **The CI runner has two cores and is slower than your machine: a test must not depend on timing or on how callers interleave** (no `Task.Delay` followed by an assertion that something happened: wait for the condition with a bounded poll or a `TaskCompletionSource`; make a collision by holding the places in the test, never by hoping a hammer collides; assert an elapsed time only as a generous upper bound). The rule is in [testing-ci.md](../../rules/testing-ci.md).

## Procedure

1. Read `.claude/rules/testing-ci.md` and the relevant feature rule/reference/ADR. Inspect neighboring tests and shared fixtures before creating new test infrastructure.
2. Identify the observable behavior and expected result, including failure/boundary cases. Use descriptive names in the repository's `Behavior_ExpectedResult` style; keep one behavior per test.
3. Put unit tests in `test/<Package>.Tests/`. Put real-service tests in the relevant folder of `test/HsnSoft.Base.IntegrationTests/`; reuse its container per service, data ownership, non-parallelization and cleanup patterns.
4. Keep tests deterministic. Fix a flaky test at its source; do not add retries, ignore it, or weaken assertions. Remove obsolete tests when the behavior is removed.
5. Run one project with `dotnet test <path-to-project.csproj>`. Use `--filter "FullyQualifiedName~TestName"` to select cases. Use `./scripts/run-tests.sh unit|integration` for all projects of that kind.
6. Run the full CI detector before the PR so both test kinds and the merged 99% source line-coverage gate are checked.

Integration tests are not required for pure logic that does not cross an external-service boundary; do not create a redundant integration test merely to increase coverage.
