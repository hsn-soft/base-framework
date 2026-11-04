# ADR-0144: Which branch may be merged into which: the rules of the source branch of a pull request

- **Status:** Accepted
- **Date:** 2026-11-04 (supersedes the source rules of the branches in [ADR-0115](0115-versions-and-releases-two-branches-and-a-version-made-from-nuget.md): `dev` from `feat/*`, `hotfix/*`, `bugfix/*` only; `rel/nuget-candidate` from `dev` only; `rel/nuget-regular` from `rel/nuget-candidate` only)

## Context

The branch rule of ADR-0115 limited the source of a pull request only for the branches it named, and it ran only on pull requests into them. A pull request from a branch with any name, or into any other branch, was not checked, a new release branch had to be added to the workflow before it could be used, and `dev` could not take a `rel/*` branch back (a fix made on a release branch). The maintainer set the rules in general terms first, then the specific ones of each release branch.

## Decision

The `CI Branch Rule` workflow runs on **every pull request, into any branch**, and checks, in this order:

1. **The name of the source** is `dev`, `master`, `feat/*`, `bugfix/*`, `hotfix/*` or `rel/*`; any other name cannot be merged.
2. **Into `dev` or `master`**: from `feat/*`, `bugfix/*`, `hotfix/*` or `rel/*`.
3. **Into a release branch (`rel/*`)**: from `bugfix/*`, `hotfix/*` or `rel/*`; and, as the **specific rules** of a release branch (the flow of the repository), also: `rel/nuget-candidate` from `dev`; `rel/nuget-regular` from `dev` (and from `rel/nuget-candidate`, which `rel/*` already allows). A release branch with no specific rule takes only what the general rule says, so a new `rel/*` needs no change to the workflow to take a fix.
4. Another target (not `dev`, `master` or `rel/*`) takes any source of a known name.

A pull request whose check fails cannot be merged once the protection of the branch requires `CI Branch Rule JOB` (a GitHub setting of the maintainer). The same rules are written in `hhs-shared` and the HHS services, each with the specific rules of its own release branches.

## Consequences

A branch name always says what the branch is for; a new release branch works at once. `rel/nuget-regular` may now take `dev` directly, so ADR-0115's sentence that stable is always a candidate published first no longer holds as a rule: the maintainer chooses the flow, and the check only allows both. A branch that does not start with one of the prefixes has to be renamed before its pull request can be merged.

## Related

[ADR-0115](0115-versions-and-releases-two-branches-and-a-version-made-from-nuget.md), [ADR-0132](0132-claude-works-on-its-own-branch-from-dev-and-never-merges-into-dev.md), `.github/workflows/ci-branch-rule.yml`.
