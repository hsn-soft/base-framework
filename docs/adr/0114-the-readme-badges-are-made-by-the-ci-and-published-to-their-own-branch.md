# ADR-0114: The badges of the README are made by the CI and published to a branch of their own

- **Status:** Accepted
- **Date:** 2026-10-31 (adds to [ADR-0113](0113-the-ci-runs-on-pull-requests-weekly-on-dev-and-by-hand.md))

## Context

The README had one badge that only said *passing* (the status of the last run on `dev`) and three of NuGet. What a reader wants to know of `dev` is more: how much of the code the tests cover, how many tests there are, and **when** that was last checked. Those numbers are known only inside a run, and the badges of the packages
(the version on NuGet) are a different kind of thing from the badges of the development.

## Decision

- **Two groups in the README.** *Development*: the status of the pipeline, **coverage** (the merged line coverage of `src`, green at or above the threshold of the CI, red below), **unit tests** and **integration tests** (how many passed, red when any failed) and **checked** (when, `yyyyMMddHHmm` in UTC; red when the pipeline of that run failed).
  *NuGet*: the version, the version with pre-releases and the downloads of `HsnSoft.Base.Core`.
- **`ci-summary.py --badges` writes the endpoint file of each badge** (the JSON that shields.io reads: `coverage.json`, `unit-tests.json`, `integration-tests.json`, `checked.json`) from the same numbers as the summary of the run, so a badge and the summary of the run cannot disagree.
- **`CI Summary JOB` publishes them to the branch `badges`** (`ci-publish-badges.sh`): an orphan branch of those four files and nothing else, made the first time. The README shows them through `img.shields.io/endpoint?url=` the raw address of the files. The job has `contents: write` for this, and the step runs **only for a scheduled or a manual run on `dev`**: a
  pull request does not change what the README says of `dev`, and the weekly run (ADR-0113) is what keeps the numbers and the date.
- The branch `badges` is not protected: no rule of the repository may require a pull request or reviews for it (the rule of `dev` names `dev`).

## Consequences

- The badges are as old as the last weekly or manual run, and say so: the *checked* badge carries the minute of the run. A merge changes none of them until that run.
- A failed publish (a protection that refuses the push) fails the weekly run, which is the signal; a pull request never runs the step.
- The branch `badges` grows by one tiny commit per run; it holds no code.
- Not here: the version numbers of the packages and of the pre-releases (their own decision).

## What to do on GitHub (once)

1. Merge the pull request that brings this, then run the workflow by hand (*Actions → CI Control Workflow → Run workflow → `dev`*): it makes the branch `badges` and the badges appear (shields.io caches them for a few minutes).
2. If a ruleset covers all branches (`~ALL`), exclude `badges` from it, or the push of the first run is refused.

## Related

[ADR-0011](0011-coverage-gate.md), [ADR-0106](0106-the-ci-reports-the-time-of-every-step-the-test-counts-and-the-coverage.md), [ADR-0109](0109-the-ci-jobs-are-a-pipeline-built-once.md), [ADR-0113](0113-the-ci-runs-on-pull-requests-weekly-on-dev-and-by-hand.md).
