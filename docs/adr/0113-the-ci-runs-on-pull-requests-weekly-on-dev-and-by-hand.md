# ADR-0113: The CI runs on pull requests, weekly on `dev` and by hand, not on every push to `dev`

- **Status:** Accepted
- **Date:** 2026-10-31 (changes when the workflow of [ADR-0012](0012-ci-workflow-and-what-breaks-a-pull-request.md) runs)

## Context

The workflow ran on pull requests into `dev`, on every push to `dev` and by hand. `dev` takes code only through a pull request, so every merge ran the whole pipeline a second time on what the pull request had just run: about 12 billed minutes for nothing, and the minutes of the runners are limited. The push run was kept for the
README badge, which shows the last run **on `dev`** (a run of a pull request counts for the branch of the pull request, not for `dev`).

## Decision

- **The triggers are `pull_request` into `dev`, `schedule` (`0 3 * * 1`: Mondays at 03:00 UTC, on the default branch, which is `dev`) and `workflow_dispatch`.** The `push` trigger is gone.
- **The weekly run is what the badge shows** (the label says *weekly*), and it checks what changes without anybody pushing: a test image that disappears from a registry (the MinIO image did, [ADR-0108](0108-the-minio-image-of-the-tests-is-built-from-source-by-chainguard.md)), a package with a new advisory, a runner image that changes.
  **By hand** (*Run workflow* on `dev`) does the same when a run is wanted now, for instance after a merge of something risky.
- **The protection of `dev` makes the run of the pull request the one that counts**: *Require branches to be up to date before merging* is on, so what the pull request tested is exactly what `dev` becomes (a second pull request has to be brought up to date, and its run repeated, after the first is merged), and the only required status check is `CI Summary JOB`
  ([ADR-0109](0109-the-ci-jobs-are-a-pipeline-built-once.md)).

## What to set on GitHub (once)

1. **Settings → Branches** (or **Rules → Rulesets**) → the rule of `dev`:
   - *Require a pull request before merging*: on (a direct push is already refused).
   - *Require status checks to pass before merging*: on. **Remove** every old name (`Build And Test JOB`, `Build And Unit Test JOB`, `Integration Test JOB`, `Coverage Gate JOB`, `Summary JOB`) and **add only `CI Summary JOB`** (it appears in the search box once it has run in the last week). It fails when any job before it did not succeed, so it stands for all of them.
   - *Require branches to be up to date before merging*: **on**. This is what makes dropping the push run safe.
2. **After this change is merged**, run the workflow once by hand so that the badge shows a run on `dev` at once: *Actions → CI Control Workflow → Run workflow → branch `dev`*.
3. **Keep the repository active**: GitHub switches scheduled workflows off in a public repository after 60 days without activity (a push or a pull request, not a run); a notice comes by e-mail, and *Actions → Enable workflow* turns it on.

## Consequences

- A merge no longer starts a run (about 12 billed minutes saved per merge); a week costs one run (about 12 minutes) on `dev` plus the runs of the pull requests.
- A change that breaks `dev` after the merge, only if it was merged without the run of its pull request being current, is found by the next weekly run, or by hand, not at once: that is what the protection setting above prevents.
- The badge can be up to a week old, and it shows the result of the last weekly or manual run, not of the last merge.

## Related

[ADR-0012](0012-ci-workflow-and-what-breaks-a-pull-request.md), [ADR-0109](0109-the-ci-jobs-are-a-pipeline-built-once.md), [ADR-0108](0108-the-minio-image-of-the-tests-is-built-from-source-by-chainguard.md).
