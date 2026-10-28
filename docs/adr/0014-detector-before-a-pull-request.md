# ADR-0014: The detector runs the CI's checks on the developer's machine before a pull request

- **Status:** Accepted
- **Date:** 2026-10-23

## Context

The pre-push hook stops at the unit tests. The pull request's CI also checks formatting, the integration tests and the coverage gate. Finding those on the pull request costs a round trip, and a developer
(or an AI session) should be able to know in one command whether the pull request will be green.

## Decision

- **`scripts/detector/detector_mac.sh`** (macOS/Linux) and **`detector_win.ps1`** (Windows, PowerShell 7) run **the stages of the CI workflow, in the same order**, and print one table: restore, every package has a
  test project, format (C#), format (JSON), build (Release, warnings are errors), unit tests, integration tests, coverage gate.
- **A stage that could not run because an earlier one failed is SKIPPED, not shown as passed** (a failed restore skips format and build; a failed build skips the tests; the gate needs both kinds of tests).
- It reads **test counts from the `.trx` files** and forces English `dotnet` output, so the numbers are right on any machine, and it reads the **threshold from the workflow** (`COVERAGE_THRESHOLD`) so it cannot drift.
- On success it prints the counts, the unit-only, integration-only and **merged** coverage (the number CI gates) and "Every stage passed. The pull request is safe to open."; on failure, the lines of the log that say
  what broke and the path of the full log under `TestResults/detector/logs/`; exit code 1.
- **`--skip-integration`** (`-SkipIntegration`) is faster and not the full check: the integration stage and the gate are SKIPPED and the closing line says so. The full check runs once before the pull request.
- **Run it before opening a pull request, and after every change that touches code, tests, settings or documents the build reads.** A change to the workflow is made in the detector too.

## Consequences

- A full run takes about two minutes (most of it the integration tests) and needs the .NET SDK, git, python3 and Docker.
- The detector is a second place that must be kept in step with the workflow; it is the price of finding problems locally.

## Related

[ADR-0012](0012-ci-workflow-and-what-breaks-a-pull-request.md), [ADR-0013](0013-git-hooks.md), `scripts/detector/README.md`.
