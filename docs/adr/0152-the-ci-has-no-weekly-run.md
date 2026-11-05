# ADR-0152: The CI has no weekly run

- **Status:** Accepted
- **Date:** 2026-11-05 (supersedes the weekly part of [ADR-0113](0113-the-ci-runs-on-pull-requests-weekly-on-dev-and-by-hand.md))

## Context

ADR-0113 added a run every Monday at 03:00 UTC on `dev`, for the badge and to catch what changes without a push (a test image that disappears, a package with a new advisory). It costs about 12 billed minutes a week with no change to check, and a public repository with no activity has its scheduled workflows switched off after 60 days anyway.
The runs that matter are those of a pull request into `dev` and of a push to a release branch; the maintainer can start one by hand when an outside change is feared.

## Decision

- **The triggers are `pull_request` into `dev`, `push` to the two release branches ([ADR-0116](0116-the-release-branches-run-the-ci-on-a-push-and-have-their-own-badges.md)) and `workflow_dispatch`.** The `schedule` trigger is gone.
- **The badges of `dev` are made by a run by hand** (*Run workflow* on `dev`), as before for a run wanted now; the badges of a release are made by its push.

## Consequences

- No scheduled minutes are paid. A change outside the repository (a vanished image, an advisory) is found by the next pull request or by a run by hand, not on Monday.
- The badges of `dev` can be older than a week; the *checked* badge carries the date of the run ([ADR-0114](0114-the-readme-badges-are-made-by-the-ci-and-published-to-their-own-branch.md)).
- The "keep the repository active" step of ADR-0113 is no longer needed.

## Related

[ADR-0113](0113-the-ci-runs-on-pull-requests-weekly-on-dev-and-by-hand.md), [ADR-0114](0114-the-readme-badges-are-made-by-the-ci-and-published-to-their-own-branch.md), [ADR-0116](0116-the-release-branches-run-the-ci-on-a-push-and-have-their-own-badges.md).
