# ADR-0153: The CI does not run on the release branches

- **Status:** Accepted
- **Date:** 2026-11-05 (supersedes the push run and the release badges of [ADR-0116](0116-the-release-branches-run-the-ci-on-a-push-and-have-their-own-badges.md))

## Context

ADR-0116 ran the pipeline on every push to `rel/nuget-candidate` and `rel/nuget-regular`, for the badges of a release. The publishing (`deploy-global-nuget.yml`) runs in parallel and never waits for it, so the run could only report after the package was out. The two branches take code only through a pull request from `dev` (or from the candidate), and the run of the pull request into `dev` already passed with
"up to date" required ([ADR-0113](0113-the-ci-runs-on-pull-requests-weekly-on-dev-and-by-hand.md)). The push run was about 12 billed minutes per release for a signal that came too late to stop anything.

## Decision

- **The triggers of the pipeline are `pull_request` into `dev` and `workflow_dispatch`.** The `push` trigger for the release branches is gone.
- **The release branches have no badges of their own**: the badge groups *candidate* and *regular* lose their CI badges (the NuGet version badges stay); the badge files with a prefix are no longer made. `dev` keeps its badges (a run by hand).
- **The protection of a release branch requires `CI Branch Rule JOB` only**, and not "up to date" (as in ADR-0116). `dev` requires `CI Branch Rule JOB` and `CI Summary JOB`, with "up to date".
- `deploy-global-nuget.yml` is unchanged: it builds, runs the unit tests, packs and pushes.

## Consequences

- A release costs no pipeline minutes. A merge into a release branch that differs from what was tested is not seen before it is published; "up to date" on `dev` and the source rule ([ADR-0144](0144-the-source-of-every-pull-request-has-a-known-branch-name.md)) are what prevent it.
- No red badge for a release; the version badges and the release workflow are the signal.
- `ci-summary.py --badge-prefix` stays as an option of the script but no workflow uses it.

## Related

[ADR-0113](0113-the-ci-runs-on-pull-requests-weekly-on-dev-and-by-hand.md), [ADR-0114](0114-the-readme-badges-are-made-by-the-ci-and-published-to-their-own-branch.md), [ADR-0116](0116-the-release-branches-run-the-ci-on-a-push-and-have-their-own-badges.md), [ADR-0152](0152-the-ci-has-no-weekly-run.md).
