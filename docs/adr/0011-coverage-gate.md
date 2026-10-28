# ADR-0011: The coverage gate merges unit and integration coverage and fails below 99 % of `src`

- **Status:** Accepted
- **Date:** 2026-10-22

## Context

Coverage that counts only unit tests punishes provider code that is reachable only against a real service; coverage that counts nothing lets untested code in. We wanted one number that
says "this line ran in a test" regardless of which kind.

## Decision

- Both test kinds collect coverage with coverlet (`test/coverlet.runsettings`: scope `[HsnSoft.Base*]`, tests, generated code, migrations and `[ExcludeFromCodeCoverage]` excluded; formats cobertura and opencover).
- **`scripts/ci-check-coverage.py <threshold> <reports...>` merges the reports line by line: a line is covered when any report covered it**, and fails when the merged line coverage of `src` is below the threshold.
- **The threshold is 99 %** and lives in one place, the `COVERAGE_THRESHOLD` environment variable of the workflow; the detector reads it from there so it cannot drift. Raise it as coverage grows; **do not lower it**.
- New code comes with tests; code that cannot be tested is a design question first, a coverage exclusion last (and an exclusion needs a reason in the code).
- Local SonarQube analysis (`scripts/sonar/`) is for looking at the quality dashboard; it excludes `test/` and `scripts/` and is not part of CI. Its token is a secret kept in an ignored `.env`, never committed.

## Consequences

- A change that removes tests along with code can drop the percentage; the pull request shows it.
- A line only the integration tests cover counts, so the gate cannot be satisfied without Docker; that is why the detector has `--skip-integration` but then reports the gate as skipped, not passed.
- Merging the reports needs the same source root; the script resolves each report's relative file names to absolute ones.

## Related

[ADR-0009](0009-unit-tests-every-package.md), [ADR-0010](0010-integration-tests-with-testcontainers.md), [ADR-0012](0012-ci-workflow-and-what-breaks-a-pull-request.md), `scripts/ci-check-coverage.py`, `test/coverlet.runsettings`.
