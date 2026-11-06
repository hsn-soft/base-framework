---
paths:
  - "src/**"
  - "test/**"
  - "scripts/run-tests.sh"
  - "scripts/ci-detector/**"
  - ".github/workflows/**"
---

# Tests and CI

- Every production package has a corresponding `test/<Package>.Tests` unit-test project, except the `HsnSoft.Base` name-holder package. Test each provider against its real service in `test/HsnSoft.Base.IntegrationTests` with Testcontainers.
- Preserve test isolation and ownership: integration tests share a container per service, own their test data, and a test that breaks a container owns its lifecycle. Follow existing fixtures and serialization conventions rather than introducing competing infrastructure.
- Tests are required for behavior, not just for a coverage number. Add focused unit tests for every new or changed behavior, including boundary/error cases; coverage is a gate and never substitutes for meaningful assertions.
- Add or update integration tests when behavior crosses a broker/database/cache/blob-service boundary; pure logic that does not cross such a boundary does not need a duplicate integration test.
- CI merges unit and integration coverage and requires at least 99% line coverage for `src`; do not lower or bypass the gate.
- Preserve CI's build-once pipeline: repository rules, formatting and Release build precede tests; downstream jobs run tests against the built artifacts, then coverage gating and summary.
- Keep `scripts/ci-detector/` aligned with `.github/workflows/ci-control-workflow.yml`; it is the local counterpart to CI.
- Workflow actions must use versions compatible with Node.js 24. Integration tests use the repository's Chainguard-built MinIO image; do not substitute a different image without a new decision.
- CI generates and publishes README badge data to the `badges` branch; do not hand-edit generated badge files.
- Current triggers are PRs into `dev` and manual dispatch (no scheduled run, ADR-0152); the release branches do not run the CI pipeline (ADR-0153). Do not reintroduce a push trigger on `dev` or `rel/*`, or PR triggers on release branches, without a new decision.
- **A test must pass on a smaller, slower and different machine than the one it was written on.** The CI runner is a shared Linux machine with two cores; a developer machine is not. So a test never depends on how long something takes or on how the scheduler interleaves callers: never `await Task.Delay(n)` and then assert that something *happened* (wait for the condition with a bounded poll, `Until` or `Support.EventuallyAsync`, where the bound, ten seconds or more, is a limit and not the wait, or with a `TaskCompletionSource` or a semaphore); a delay is allowed before a *negative* assertion (nothing more happened) and never as the proof of a positive one. **Contention is made, not hoped for**: a test that needs callers to collide holds the places itself (a held lease, a gate) instead of asserting that a counter of refusals is above zero after a hammer (that is what failed in CI on 2026-11-05). An elapsed time is asserted only as a generous upper bound (a multiple of the timeout under test). Do not rely on the number of cores, the order of parallel results, the culture or the time zone of the machine, a case-insensitive file system (macOS) or a fixed port. A test that fails in CI and passes locally is a bug of the test's assumption: fix the assumption (the log of the run is in the workflow run), never re-run until it is green.
- **A script a person runs by `source` must work in bash and in zsh** (the terminal of macOS is zsh): no `BASH_SOURCE`, no `${!name}` indirection; find the root with `git rev-parse --show-toplevel` and test a variable with `printenv`. A script run as a program (`./scripts/x.sh`, shebang `bash`) may use bash. Test a sourced script in both shells (`zsh -c '. ./scripts/x.sh'`, `bash -c ...`) with fake values before it is committed (`load-env.sh` was bash-only once and loaded nothing in zsh).

## Run focused tests

```bash
./scripts/run-tests.sh unit
./scripts/run-tests.sh integration
./scripts/run-tests.sh unit --filter "FullyQualifiedName~TestName"
dotnet test test/HsnSoft.Base.Core.Tests/HsnSoft.Base.Core.Tests.csproj
dotnet test test/HsnSoft.Base.IntegrationTests/HsnSoft.Base.IntegrationTests.csproj --filter "FullyQualifiedName~TestName"
```

Use the shared runner for a test kind across the repository. Run `dotnet test <project.csproj>` when targeting one project; append `--filter` to select tests. Unit tests are fast and need no external services; integration tests use Docker/Testcontainers. Run focused tests while iterating, then the full detector (see the git-release rule for what counts as the final check).

Use [the detector guide](../../scripts/ci-detector/README.md) for stages and diagnostics. Relevant decisions: ADR-0009–0014, ADR-0106–0111, ADR-0113–0116, ADR-0123, ADR-0152, ADR-0153.
