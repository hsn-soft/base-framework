---
name: release
description: "Use when preparing a HsnSoft.Base candidate or stable NuGet release, changing release versions, or investigating the release workflow."
---

# Prepare a release

## Procedure

1. Read `.claude/rules/git-release.md`, `docs/Releasing.md`, `.github/workflows/deploy-global-nuget.yml`, the branch workflow, and ADR-0115/0116/0130. Confirm the current branch and the branch rule in `CLAUDE.md` (ADR-0132).
2. Claude prepares a release on its own branch (for example the `<BaseVersion>` change) and does not open or merge the pull requests into `dev`, `rel/nuget-candidate` or `rel/nuget-regular`: the maintainer does (ADR-0132). If the branch state or the request conflicts with repository policy, stop and ask for direction.
3. The normal flow is `dev` → `rel/nuget-candidate` → `rel/nuget-regular`. Candidate and stable releases are produced by merges into their respective branches; do not publish from an arbitrary local build.
4. For a new monthly base, update `<BaseVersion>` in `common.props` through the normal PR. Release scripts derive the package number from NuGet; do not manually set each package version. Use `scripts/versioning/` tests and `scripts/pack-local.sh` only for local validation.
5. Run the full CI detector before proposing a release PR. Confirm the CI/branch-rule check is green and verify the intended NuGet version/channel before any merge.
6. Never merge into `dev` or `rel/*`, tag, publish, or expose credentials, and push only to Claude's own branch. NuGet publishing uses trusted OIDC; never add a long-lived API key.
7. Update `docs/Releasing.md` and add/supersede an ADR if release behavior changes.
