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
- Every provider must have integration tests against its real external service in the shared integration-test project. Add or update integration tests when behavior crosses a broker/database/cache/blob-service boundary; pure logic that does not cross such a boundary does not need a duplicate integration test.
- CI merges unit and integration coverage and requires at least 99% line coverage for `src`; do not lower or bypass the gate.
- Preserve CI's build-once pipeline: repository rules, formatting and Release build precede tests; downstream jobs run tests against the built artifacts, then coverage gating and summary.
- Keep `scripts/ci-detector/` aligned with `.github/workflows/ci-control-workflow.yml`; it is the local counterpart to CI. Run the focused tests while iterating and the full detector before finishing a change.
- Workflow actions must use versions compatible with Node.js 24. Integration tests use the repository's Chainguard-built MinIO image; do not substitute a different image without a new decision.
- CI generates and publishes README badge data to the `badges` branch; do not hand-edit generated badge files.
- Current triggers are PRs into `dev`, a weekly scheduled run and manual dispatch; release branches run CI on push. Do not reintroduce a push trigger on `dev` or PR triggers on release branches without a new decision.

## Run focused tests

```bash
./scripts/run-tests.sh unit
./scripts/run-tests.sh integration
./scripts/run-tests.sh unit --filter "FullyQualifiedName~TestName"
dotnet test test/HsnSoft.Base.Core.Tests/HsnSoft.Base.Core.Tests.csproj
dotnet test test/HsnSoft.Base.IntegrationTests/HsnSoft.Base.IntegrationTests.csproj --filter "FullyQualifiedName~TestName"
```

Use the shared runner for a test kind across the repository. Run `dotnet test <project.csproj>` when targeting one project; append `--filter` to select tests. Unit tests are fast and need no external services; integration tests use Docker/Testcontainers. Run focused tests while iterating, then the full detector before the PR. `--skip-integration` is an interim local check only, not final PR validation.

Use [the detector guide](../../scripts/ci-detector/README.md) for stages and diagnostics. Relevant decisions: ADR-0009–0014, ADR-0106–0111, ADR-0113–0116, ADR-0123.
