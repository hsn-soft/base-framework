# ADR-0116: The release branches run the CI on a push, and have badges of their own

- **Status:** Accepted
- **Date:** 2026-10-31 (supersedes the run on pull requests into the release branches in [ADR-0115](0115-versions-and-releases-two-branches-and-a-version-made-from-nuget.md); adds to [ADR-0114](0114-the-readme-badges-are-made-by-the-ci-and-published-to-their-own-branch.md))

## Context

The badges of the README said how `dev` was, last checked on a Monday. A reader who takes a package from nuget.org takes what `rel/nuget-candidate` or `rel/nuget-regular` published, and wants the numbers of **that**: coverage, the number of tests, the state of the build and how long the CI took. The pipeline ran on the pull requests into those branches, but a pull request run is not
the run of the published commit (the release branches are not required to be up to date, so the merge can differ from what the pull request tested), and a pull request must not write the badges.

## Decision

- **On the two release branches the pipeline runs on every push, and not on a pull request.** `ci-control-workflow.yml` has `push` for `rel/nuget-candidate` and `rel/nuget-regular`; its `pull_request` is for `dev` only. The run is of the commit that is published; its result is the badge, and **a red run is the signal** that what was published failed a check (the package is
  published by `Deploy Global NuGet`, which runs in parallel and does not wait for it). `CI Branch Rule JOB` still runs on the pull requests into the release branches: where a merge comes from is checked before it, not after.
- **Each release branch has badges of its own** on the branch `badges`, the files with a prefix (`candidate-coverage.json`, `regular-unit-tests.json`, ...); `dev` keeps the names with no prefix. `ci-summary.py --badge-prefix` writes them, `ci-publish-badges.sh` replaces only the files it is given (and takes the commit of another branch that finished first, then pushes again).
- **A new badge, `ci-time`**: the minutes the steps of the run took, the same number as the summary.
- **The README has three groups**: development (unchanged), candidate (the version with pre-releases, the status of the pipeline on push, coverage, unit and integration tests, CI time, checked) and regular (the same, with the stable version). **The downloads badge is gone**: NuGet counts them per package and the badge added up every version since the package began (483 thousand, almost all of them of versions of before the rewrite), so it said nothing of what is published now.
- The publish step runs for a scheduled or a manual run on `dev` and for a push to a release branch, **also when the pipeline failed** (the badge is red); a pull request never runs it.

## Consequences

- A release runs the pipeline once (on the push), not twice: about 12 billed minutes less than ADR-0115 said. The price: **nothing stops a bad merge into a release branch before it is published**; the code came from `dev`, whose pull request run passed, and from the candidate, for regular, so the risk is a merge that differs from what was tested.
- The protection of the release branches **must not require `CI Summary JOB`** (it does not run on their pull requests, the check would wait for ever); it requires `CI Branch Rule JOB` only. `dev` keeps both.
- The unit tests that `Deploy Global NuGet` runs stay as its own last check before a version that cannot be taken back.
- A scheduled run is for `dev` only; the badges of a release are as old as its last push, and say so (*checked*).

## What to do on GitHub (once)

1. In the protection of `rel/nuget-candidate` and `rel/nuget-regular`, remove `CI Summary JOB` from the required checks (keep `CI Branch Rule JOB`).
2. The first push to each branch makes its badges (shields.io caches them for a few minutes).

## Related

[ADR-0109](0109-the-ci-jobs-are-a-pipeline-built-once.md), [ADR-0113](0113-the-ci-runs-on-pull-requests-weekly-on-dev-and-by-hand.md), [ADR-0114](0114-the-readme-badges-are-made-by-the-ci-and-published-to-their-own-branch.md), [ADR-0115](0115-versions-and-releases-two-branches-and-a-version-made-from-nuget.md).
