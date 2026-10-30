# ADR-0109: The CI jobs are a pipeline: the rules and the build first, the tests on what was built, the gate and a summary that decides last

- **Status:** Accepted
- **Date:** 2026-10-30 (changes the layout of the jobs of [ADR-0012](0012-ci-workflow-and-what-breaks-a-pull-request.md))

## Context

The workflow had a unit job (rules, format, restore, build, unit tests) and an integration job (which restored and built again) that ran at the same time, then the coverage gate and the summary. A pull request that broke a rule, the format or the build still started the Docker tests and paid for them, the integration job built what the first
job had already built, and the first job mixed checking the repository with testing it.

## Decision

The jobs form a pipeline, each starting only when the ones it needs passed (`needs`), with these names:

1. **`CI Rules, Format And Build JOB`** (`rules-format-build`): the rules of the repository (every package has unit tests, the order of the references, the framework reference rules), the format check (C# and JSON), restore, and **one build in Release with warnings as errors**; it ends the pipeline there when any of them fails. It then packs the build output of the **test projects** and the restore state of every project (`test/*/bin/Release`, `src/*/obj`, `test/*/obj`) into **one `.tar.zst` artifact** (one file, not thousands: uploading small files one by one is slow; zstd, because it packs in a third of the time of gzip and smaller; kept one day).
   The `bin` of the libraries is left out on purpose: a test project holds copies of what it references, and every unit test project ran with all of them removed (checked).
2. **`CI Check Unit Test JOB`** (`unit-tests`, needs 1): downloads that artifact, unpacks it where it was, runs `dotnet restore` (which only checks the packages from the cache) and runs the unit tests with `--no-restore --no-build`.
3. **`CI Check Integration Test JOB`** (`integration-tests`, needs 2): the same hand-over, then the integration tests with `--no-restore --no-build`.
4. **`CI Coverage Gate JOB`** (`coverage-gate`, needs 3): merges the coverage of both kinds of tests and fails the pipeline below the threshold.
5. **`CI Summary JOB`** (`summary`, needs all, runs unless the run was cancelled): prints the times of every step, the test counts and the coverage whatever happened before, and **then fails when any job did not succeed** (failed, or skipped because one before it failed).

The steps still write their times with `ci-timed.sh`, now into three files (`rules-build.txt`, `unit.txt`, `integration.txt`).

## Consequences

- A broken rule, format or build costs about a minute and a half and starts nothing else; a broken unit test costs no Docker; the integration tests build nothing.
- **The jobs are in sequence, not side by side, because the minutes of the runners are limited.** The minutes are the sum of the durations of the jobs (each rounded up), so running the unit tests and the integration tests at the same time saves waiting, not minutes, and costs minutes whenever the unit tests fail: the Docker tests would run to the end for nothing. In sequence a failed unit test stops before the most expensive job.
  The price is the waiting: the pipeline is slower end to end than the old two jobs side by side (the rules, the build, the unit tests before the integration tests, and each job has its own start and its own download of the build). The gain is the early stop, the single build and the clear order.
- The hand-over relies on every runner having the same path for the checkout (`/home/runner/work/<repo>/<repo>`), which the restore state records; a self-hosted runner with another path would need a restore and a build in each job.
- **Only `CI Summary JOB` has to be a required check.** A job that is skipped because a job it needs failed is reported as skipped, and the protection of a branch counts a skipped check as passed: requiring only the coverage gate would let a pull request with a failed unit test be merged. The summary always runs and fails when any of the jobs did not succeed, so it alone stands for all of them.
  The names of the jobs changed, so the old required checks (`Build And Unit Test JOB` and the like) must be removed from the protection of `dev`.
- The detector runs the same stages in the same order on a developer's machine, in one process.

## Related

[ADR-0011](0011-coverage-gate.md), [ADR-0012](0012-ci-workflow-and-what-breaks-a-pull-request.md), [ADR-0106](0106-the-ci-reports-the-time-of-every-step-the-test-counts-and-the-coverage.md), [ADR-0107](0107-the-scripts-of-the-ci-and-the-detector-live-in-one-folder.md).
