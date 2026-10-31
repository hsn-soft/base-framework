# ADR-0115: Versions and releases: a candidate branch and a regular branch, and a version made from what NuGet has

- **Status:** Accepted (the pipeline on the pull requests into the release branches is superseded by [ADR-0116](0116-the-release-branches-run-the-ci-on-a-push-and-have-their-own-badges.md): it runs on the push)
- **Date:** 2026-10-31 (replaces how the version is kept and how a release is made in [ADR-0007](0007-build-settings-and-versioning.md))

## Context

The version was one line of `common.props` (`<Version>2026.10.40</Version>`) that a person raised by hand (40 to 41) in a pull request; pushing `dev` into `rel/nuget` then built, tested and pushed the packages with `--skip-duplicate`. Three weak points: a forgotten bump made the push skip all 37 packages **and the workflow ended green**; no tag or release
said which commit a version was made from; and there was no pre-release to try a version in a service before it was final. NuGet versions cannot be deleted (only unlisted), and only nuget.org is used (no GitHub Packages).

## Decision

### The version

- **`common.props` has the base by hand: `<BaseVersion>2026.10</BaseVersion>`** (year.month, written by the owner). A build that is not a release is `2026.10.0-dev`.
- **The number after the base is made by the release** (`scripts/versioning/next-version.py`): the highest **stable** number of the base that NuGet has for **any** package of the repository, plus one (zero when the base has none). A number that exists anywhere is never made again.
  When a month changes, the owner changes the base (`2026.11`) and the number starts at 0.
- **Regular: `2026.10.41`. Candidate: `2026.10.41-rc.<yyMMdd>.<run number>`**: the pre-release of the **next** stable number (a pre-release of 40 would sort below the published 40 and nobody would be offered it). The number of the run of the workflow always grows, so the candidates sort in the order they were made
  (a pull request number does not: #380 can be merged before #379); the pull requests are in the notes of the release.

### The branches

- **`rel/nuget-candidate` takes code from `dev` only; `rel/nuget-regular` takes code from `rel/nuget-candidate` only** (no other branch, `dev` included). What is published as stable is therefore a candidate that was published first. There is **no way from `dev` straight to regular**: an urgent fix goes `hotfix/*` → `dev` → candidate → regular, each a pull
  request with the pipeline of the CI, which is minutes; a standing exception would break the rule that stable is a tested candidate, and an emergency that cannot wait is a decision of the owner to bypass the protection for that one merge.
- **`dev` takes code from `feat/*`, `hotfix/*` and `bugfix/*` only.**
- GitHub cannot limit the source of a merge, so **every pull request into these three branches runs `CI Branch Rule JOB`** (`ci-branch-rule.yml`), and the protection of each branch requires it. The names of the branches reach the check as environment variables, not inside its script, and a pull request from another repository is refused.
- The pipeline of the CI (`CI Summary JOB`) runs on the pull requests into the two release branches as well, so what is published was tested.

### The release

- **Publishing happens when a pull request is merged (the push to the branch), never when it is opened**: a version cannot be taken back, a pull request can get a new commit ten times, and a pull request run is not given the NuGet key.
- **`Deploy Global NuGet` runs on a push to either branch**: the version, restore, build with it (`-p:Version`), **the unit tests** (not the integration tests, the coverage, the format and the rules: the pull request ran them), pack, a check that every package is of that version (nothing packed or another version stops before the push; no
  `--skip-duplicate`), push to nuget.org, and **a tag `v<version>` with a GitHub Release** (a pre-release for a candidate) whose notes list the pull requests since the last stable version. The tag and the release store no package.
- `rel/nuget` is retired (its push no longer runs anything).

## What to set on GitHub (once)

1. **Branches**: create `rel/nuget-candidate` from `dev` and `rel/nuget-regular` from the commit of the last release (`4141bc54`, the head of `rel/nuget`, version 2026.10.40). The first release goes through the pull requests.
2. **Protection** of the three branches (*Settings → Branches* or *Rules*): require a pull request, no direct push, no bypass for administrators; required checks **`CI Branch Rule JOB`** and **`CI Summary JOB`**; the merge method of the two release branches is **a merge commit** (a squash or a rebase makes commits that the next pull request
   would show again). **Require branches to be up to date**: **on for `dev`, off for the two release branches** (a pull request from `dev` cannot be brought up to date without writing to `dev`, and their pipeline run is on the result of the merge anyway).
3. **A baseline tag** so that the first release notes start at the last release: `git tag v2026.10.40 4141bc54 && git push origin v2026.10.40`. Rules that restrict the creation of tags `v*` must let the workflow (the token of the run) create them.
4. **The secret `GLOBAL_NUGET_TOKEN`** stays; nothing else is new.

## Consequences

- No bump to remember and no silent no-op; a version is always new and says which commit made it.
- A release is two pull requests and two runs of the pipeline (a few minutes of waiting and about 24 billed minutes more than one), and the candidate is a free trial in a service before the stable version.
- A push that fails after some packages were pushed leaves those at a number that exists; the next run makes the next number (a gap, never a mix of two sets under one number).
- The pre-releases stay on nuget.org for good (listed or unlisted); `-rc.` versions are shown only to those who ask for pre-releases.
- The base is raised by hand: forgetting it makes the numbers go on in the old base (not wrong, only a month later than meant).

## Related

[ADR-0007](0007-build-settings-and-versioning.md), [ADR-0012](0012-ci-workflow-and-what-breaks-a-pull-request.md), [ADR-0109](0109-the-ci-jobs-are-a-pipeline-built-once.md), [ADR-0113](0113-the-ci-runs-on-pull-requests-weekly-on-dev-and-by-hand.md), [docs/Releasing.md](../Releasing.md).
