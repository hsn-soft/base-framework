# ADR-0106: The CI reports the time of every step, the test counts and the coverage, and shows every test

- **Status:** Accepted
- **Date:** 2026-10-29 (adds to [ADR-0012](0012-ci-workflow-and-what-breaks-a-pull-request.md))

## Context

A CI run said pass or fail and little else: how long the restore, the format check, the build and each kind of test took, how many tests ran and what the coverage was could only be found by opening every step of two jobs. The tests wrote one line for the whole project, so a slow or stuck test was
not visible, and the trx logger wrote every test project of a kind to the same file name, so all but the last project's results were overwritten in the artifact.

## Decision

- **`scripts/run-tests.sh` prints, for every test project, `n/total`, the time it started, the time it took and when it ended, and the total of the kind** (this also shows in the detector, which runs the same script).
- **The workflow asks for the console logger at `normal` verbosity** (`--logger "console;verbosity=normal"`), so every test prints its own result as it ends. The detector keeps its quiet output: the logger is an argument of the caller, not of the script.
- **`scripts/ci-detector/ci-timed.sh` times a step**: restore, the format checks, build, unit tests (job `unit-tests`) and integration tests (job `integration-tests`) write `label=seconds` into a file that the job uploads as an artifact (`unit-timings`, `integration-timings`), whether the step passes or fails.
- **A last job, `summary`, runs always** (`if: always()`): `scripts/ci-detector/ci-summary.py` reads the timings, the `.trx` files and the coverage reports and prints one page, also to the page of the run (`GITHUB_STEP_SUMMARY`): the time of every step by job, `unit tests x/y`, `integration tests x/y` and the merged line coverage of `src`. It only reports; the gate and the tests still decide.
- **The trx logger is given `LogFilePrefix` instead of `LogFileName`**, so every project keeps its own results file and the counts are the sum over all of them.

## Consequences

- The slow step, a test count that dropped (a Docker that did not start, a project that was not found) and a coverage that moved are on the first page of the run.
- A run that fails in a step still reports the time of the steps before it, and of the one that failed.
- `ci-check-coverage.py` has its merge of the reports as a function (`merge_lines`) that the summary uses, so the coverage of the summary is the number of the gate.
- Not done: the detector does not print the summary page (it has its own table of the stages and their times).

## Related

[ADR-0011](0011-coverage-gate.md), [ADR-0012](0012-ci-workflow-and-what-breaks-a-pull-request.md), [ADR-0014](0014-detector-before-a-pull-request.md).
