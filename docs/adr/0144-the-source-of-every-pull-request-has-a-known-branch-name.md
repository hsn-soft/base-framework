# ADR-0144: The source branch of every pull request has a known name

- **Status:** Accepted
- **Date:** 2026-11-04 (adds to the branch rule of [ADR-0115](0115-versions-and-releases-two-branches-and-a-version-made-from-nuget.md); supersedes nothing)

## Context

The branch rule of ADR-0115 limited the source of a pull request only for the branches it named (`dev` and the release branches), and it ran only on pull requests into them. A pull request from a branch with any name, or into any other branch, was not checked, and a misnamed working branch (`wip`, `test2`, a person's name) could be merged without anyone noticing that the name carried no meaning.

## Decision

The `CI Branch Rule` workflow runs on **every pull request, into any branch**, and its first check is the name of the source branch: it must be `dev`, `master`, `rel/*`, `feat/*`, `hotfix/*` or `bugfix/*`; any other name fails the check, and a pull request whose check fails cannot be merged once the protection of the branch requires the check. The rules of ADR-0115 stay and run after it (into `dev` only from `feat/*`, `hotfix/*` or `bugfix/*`; the release branches from the branch the release flow names). A new release branch (`rel/*`) fails the check until a rule for it is added to the workflow, so its source is decided before it takes code. The same rule is written in `hhs-shared` and the HHS services.

## Consequences

A branch name always says what the branch is for. The cost: a branch that does not start with one of the prefixes has to be renamed before its pull request is merged. Merging is blocked only where the maintainer set the protection of the branch to require `CI Branch Rule JOB` (GitHub setting).

## Related

[ADR-0115](0115-versions-and-releases-two-branches-and-a-version-made-from-nuget.md), [ADR-0132](0132-claude-works-on-its-own-branch-from-dev-and-never-merges-into-dev.md), `.github/workflows/ci-branch-rule.yml`.
