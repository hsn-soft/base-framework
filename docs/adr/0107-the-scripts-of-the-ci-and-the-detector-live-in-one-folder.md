# ADR-0107: What the CI and the detector run lives in one folder; the cleaning script is `cleaner`

- **Status:** Accepted
- **Date:** 2026-10-29 (renames the folders of [ADR-0014](0014-detector-before-a-pull-request.md); the layout of `scripts/` is not part of [ADR-0006](0006-repository-folders-and-the-solution-file.md))

## Context

The scripts of the checks were spread over `scripts/`: the detector in its own folder, the Python checks of the CI (`ci-check-coverage.py`, `ci-force-*.py`) and the formatters (`format-json.cs`, `format-staged.py`) beside unrelated scripts. A change to a CI step meant looking for the detector stage and the script in two
places, and the rule of the workflow ("keep it in step with the detector") had no folder to match it.

## Decision

- **`scripts/ci-detector/`** holds the detector (`ci_detector_mac.sh`, `ci_detector_win.ps1`), every Python script that starts with `ci-` (`ci-check-coverage.py`, `ci-force-csproj-order.py`, `ci-force-framework-references-rules.py`, `ci-summary.py`), the two shell scripts above and the two formatters
  (`format-json.cs`: CI, detector and pre-commit hook; `format-staged.py`: the pre-commit hook). Its README describes them all, with a *Format* section.
- **`scripts/cleaner/`** (`cleaner_mac.sh`, `cleaner_win.ps1`) is the former `scripts/clean/`; the detector calls it first, so it starts from a clean tree as the CI does.
- **Also in the folder:** `ci-check-test-project-exist.sh` and `ci-timed.sh`: only the CI and the detector call them (a first version of this layout left them in `scripts/`, on the wrong ground that many places call them).
- **Stays in `scripts/`:** the scripts that are called from places other than the checks: `run-tests.sh` (the CI, the detector **and the pre-push hook**), `pack-local.sh`, `sonar/`, `graylog-fields/`.
- The output folder of a detector run keeps its name, `TestResults/detector/`.

## Consequences

- One folder to open when a check changes; the workflow, the hooks, the docs and the ADRs name the new paths (the paths in older ADRs were updated, their decisions were not).
- Any script of the folder reaches the root of the repository with two `..` (`scripts/ci-detector/`), one more than before for the framework-references check.

## Related

[ADR-0008](0008-code-style-and-analyzers.md), [ADR-0011](0011-coverage-gate.md), [ADR-0013](0013-git-hooks.md), [ADR-0014](0014-detector-before-a-pull-request.md), [ADR-0106](0106-the-ci-reports-the-time-of-every-step-the-test-counts-and-the-coverage.md).
