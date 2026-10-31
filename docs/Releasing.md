# Releasing

How a version of the packages gets to NuGet, and what to do. Why it is this way: [ADR-0115](adr/0115-versions-and-releases-two-branches-and-a-version-made-from-nuget.md).

## The versions

All packages have one version, `<base>.<number>`, for example `2026.10.41`.

- The **base** (`2026.10`) is `<BaseVersion>` in `common.props`, written by hand.
- The **number** is made by the release: the highest stable number of the base on NuGet, plus one (0 for a base that has none yet).
- A **candidate** is the pre-release of the next stable version: `2026.10.41-rc.261031.57` (date `yyMMdd`, then the number of the run of the workflow).
- A build that is not a release is `2026.10.0-dev`; `scripts/pack-local.sh` makes `2026.10.41-local.<time>` to try a change in a service.

## Branches

```
feat/*  hotfix/*  bugfix/*  ──PR──►  dev  ──PR──►  rel/nuget-candidate  ──PR──►  rel/nuget-regular
                                      (the pipeline of the CI on every pull request into dev; on the push to the two release branches, ADR-0116)
                                                         │ merge                         │ merge
                                                         ▼                               ▼
                                          2026.10.41-rc.<date>.<run>              2026.10.41
                                          (nuget.org, pre-release)           (nuget.org, stable)
                                          tag v…-rc… + GitHub pre-release    tag v2026.10.41 + GitHub Release
```

Each arrow is a pull request; nothing else may be merged into a branch (`CI Branch Rule JOB` refuses it). Nothing is published when a pull request is opened, only when it is merged; the merge (the push) also runs the pipeline of the CI, and its numbers are the badges of that release in the README (a red run means what was published failed a check).

## To release

1. **A candidate**: open a pull request from `dev` into `rel/nuget-candidate` (merge method: merge commit). When `CI Branch Rule JOB` is green (the pipeline runs after the merge, on the push), merge it: the candidate is published. Try it in a service (`2026.10.41-rc.…`, with pre-releases on).
2. **The stable version**: open a pull request from `rel/nuget-candidate` into `rel/nuget-regular` and merge it: `2026.10.41` is published, with its tag and release.

## To start a new base (a new month)

Change `<BaseVersion>` in `common.props` (`2026.11`) in a pull request into `dev`. The next release of the base starts at `2026.11.0`.

## If a release failed

The run says in which step. Nothing is pushed before the packages are checked; if the push itself stopped half way, run the release again: it makes the next number (the half set stays at the old one, the new set is whole).

## An urgent fix

`hotfix/*` → `dev` → `rel/nuget-candidate` → `rel/nuget-regular`, the same way (the pipeline runs on the pull request into dev and on each push). There is no way from `dev` to regular: stable is always a candidate that was published first.
